/*
 QR code web component that renders a standards-compliant matrix without external deps.
 Supports byte mode, versions 1-4, error level L (sufficient for dashboard URLs).
*/

const DEFAULT_PADDING = 10;

const QR_VERSION_INFO = {
    1: { size: 21, alignmentCenters: [], ecc: { L: { totalDataCodewords: 19, ecCodewordsPerBlock: 7, blocks: [{ dataCodewords: 19, eccCodewords: 7 }] } } },
    2: { size: 25, alignmentCenters: [6, 18], ecc: { L: { totalDataCodewords: 34, ecCodewordsPerBlock: 10, blocks: [{ dataCodewords: 34, eccCodewords: 10 }] } } },
    3: { size: 29, alignmentCenters: [6, 22], ecc: { L: { totalDataCodewords: 55, ecCodewordsPerBlock: 15, blocks: [{ dataCodewords: 55, eccCodewords: 15 }] } } },
    4: { size: 33, alignmentCenters: [6, 26], ecc: { L: { totalDataCodewords: 80, ecCodewordsPerBlock: 20, blocks: [{ dataCodewords: 80, eccCodewords: 20 }] } } }
};

const GF_EXP = new Array(512);
const GF_LOG = new Array(256);
generateGF256Tables();

const GENERATOR_POLY_CACHE = new Map();

class QRCodeElement extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
    }

    static get observedAttributes() {
    return ['data', 'size', 'level', 'padding'];
    }

    attributeChangedCallback() {
        this.render();
    }

    connectedCallback() {
        this.render();
    }

    render() {
        const data = this.getAttribute('data') || '';
        const requestedSize = parseInt(this.getAttribute('size') || '200', 10);
        const requestedLevel = (this.getAttribute('level') || 'L').toUpperCase();
        const paddingAttr = parseFloat(this.getAttribute('padding'));
        const paddingPxRaw = Number.isFinite(paddingAttr) && paddingAttr >= 0 ? paddingAttr : DEFAULT_PADDING;

        if (!data) {
            this.shadowRoot.innerHTML = `<div style="color:#f88">No data</div>`;
            return;
        }

        try {
            const matrix = generateQRCodeMatrix(data, requestedLevel);
            if (!matrix) {
                this.shadowRoot.innerHTML = `<div style="color:#f88">Data too long</div>`;
                return;
            }

            const moduleCount = matrix.length;
            const totalSize = Number.isFinite(requestedSize) && requestedSize > 0 ? requestedSize : moduleCount * 10;
            const paddingPx = Math.min(paddingPxRaw, totalSize / 2);
            const innerSize = totalSize - paddingPx * 2;

            if (innerSize <= 0) {
                this.shadowRoot.innerHTML = `<div style="color:#f88">QR size too small for padding.</div>`;
                return;
            }

            const viewBoxMargin = (moduleCount * paddingPx) / innerSize;
            const viewBoxMin = -viewBoxMargin;
            const viewBoxSize = moduleCount + viewBoxMargin * 2;

            let svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${totalSize}" height="${totalSize}" viewBox="${formatFloat(viewBoxMin)} ${formatFloat(viewBoxMin)} ${formatFloat(viewBoxSize)} ${formatFloat(viewBoxSize)}" shape-rendering="crispEdges" preserveAspectRatio="xMidYMid meet">`;
            svg += `<rect x="${formatFloat(viewBoxMin)}" y="${formatFloat(viewBoxMin)}" width="${formatFloat(viewBoxSize)}" height="${formatFloat(viewBoxSize)}" fill="#ffffff"/>`;

            for (let y = 0; y < moduleCount; y++) {
                for (let x = 0; x < moduleCount; x++) {
                    if (matrix[y][x] === 1) {
                        svg += `<rect x="${x}" y="${y}" width="1" height="1" fill="#000"/>`;
                    }
                }
            }

            svg += `</svg>`;
            this.shadowRoot.innerHTML = `<div style="width:${totalSize}px;height:${totalSize}px;display:inline-block;">${svg}</div>`;
        } catch (err) {
            console.error('QR render failed', err);
            this.shadowRoot.innerHTML = `<div style="color:#f88">QR error</div>`;
        }
    }
}

function formatFloat(value) {
    return Number.isFinite(value) ? Number(value.toFixed(6)).toString() : '0';
}

function generateQRCodeMatrix(text, level) {
    const normalizedLevel = level === 'L' ? 'L' : 'L';
    if (level !== normalizedLevel) {
        console.warn(`Unsupported QR level "${level}", falling back to level L.`);
    }

    const encoder = new TextEncoder();
    const dataBytes = Array.from(encoder.encode(text));
    const version = chooseVersion(dataBytes.length);
    if (!version) return null;

    const versionInfo = QR_VERSION_INFO[version];
    const eccInfo = versionInfo.ecc[normalizedLevel];
    const maxDataBytes = eccInfo.totalDataCodewords;

    const bitStream = buildBitStream(dataBytes, maxDataBytes);
    const dataCodewords = bitsToCodewords(bitStream, eccInfo.totalDataCodewords);
    const eccCodewords = computeECCBlocks(dataCodewords, eccInfo);
    const finalCodewords = interleaveBlocks(dataCodewords, eccCodewords, eccInfo);
    const finalBits = codewordsToBits(finalCodewords);

    const { matrix, reserved } = createEmptyMatrix(versionInfo.size);
    placeFinderPatterns(matrix, reserved);
    placeTimingPatterns(matrix, reserved);
    placeAlignmentPatterns(matrix, reserved, versionInfo);
    placeDarkModule(matrix, reserved, version);
    reserveFormatInfo(matrix, reserved);

    const dataModules = placeDataBits(matrix, reserved, finalBits);

    const baseMatrix = cloneMatrix(matrix);
    const maskChoice = chooseBestMask(baseMatrix, reserved, dataModules);
    applyMask(matrix, dataModules, maskChoice.pattern);
    placeFormatInfo(matrix, reserved, normalizedLevel, maskChoice.pattern);

    return matrix;
}

function chooseVersion(byteLength) {
    const capacities = { 1: 17, 2: 32, 3: 53, 4: 78 };
    for (let version = 1; version <= 4; version++) {
        if (byteLength <= capacities[version]) {
            return version;
        }
    }
    return null;
}

function buildBitStream(bytes, maxDataBytes) {
    const bits = [];
    bits.push(0, 1, 0, 0); // byte mode
    bits.push(...toBits(bytes.length, 8));
    for (const value of bytes) {
        bits.push(...toBits(value, 8));
    }

    const totalDataBits = maxDataBytes * 8;
    const terminatorLength = Math.min(4, totalDataBits - bits.length);
    for (let i = 0; i < terminatorLength; i++) bits.push(0);

    while (bits.length % 8 !== 0) bits.push(0);

    const padBytes = [0xEC, 0x11];
    let padIndex = 0;
    while (bits.length / 8 < maxDataBytes) {
        bits.push(...toBits(padBytes[padIndex % 2], 8));
        padIndex += 1;
    }
    return bits.slice(0, totalDataBits);
}

function bitsToCodewords(bits, dataCodewordTarget) {
    const codewords = [];
    for (let i = 0; i < bits.length; i += 8) {
        const slice = bits.slice(i, i + 8);
        let value = 0;
        for (const bit of slice) {
            value = (value << 1) | bit;
        }
        codewords.push(value & 0xFF);
    }
    while (codewords.length < dataCodewordTarget) {
        codewords.push(0);
    }
    return codewords;
}

function computeECCBlocks(dataCodewords, eccInfo) {
    const { blocks, ecCodewordsPerBlock } = eccInfo;
    const eccBlocks = [];
    let offset = 0;
    for (const block of blocks) {
        const blockData = dataCodewords.slice(offset, offset + block.dataCodewords);
        offset += block.dataCodewords;
        const ecc = reedSolomon(blockData, ecCodewordsPerBlock);
        eccBlocks.push({ data: blockData, ecc });
    }
    return eccBlocks;
}

function interleaveBlocks(dataCodewords, eccBlocks, eccInfo) {
    if (eccInfo.blocks.length === 1) {
        return [...dataCodewords, ...eccBlocks[0].ecc];
    }

    const dataColumns = [];
    const eccColumns = [];
    const maxDataLength = Math.max(...eccBlocks.map(block => block.data.length));
    for (let i = 0; i < maxDataLength; i++) {
        for (const block of eccBlocks) {
            if (i < block.data.length) dataColumns.push(block.data[i]);
        }
    }
    const maxEccLength = Math.max(...eccBlocks.map(block => block.ecc.length));
    for (let i = 0; i < maxEccLength; i++) {
        for (const block of eccBlocks) {
            if (i < block.ecc.length) eccColumns.push(block.ecc[i]);
        }
    }
    return [...dataColumns, ...eccColumns];
}

function codewordsToBits(codewords) {
    const bits = [];
    for (const value of codewords) {
        bits.push(...toBits(value, 8));
    }
    return bits;
}

function createEmptyMatrix(size) {
    const matrix = Array.from({ length: size }, () => Array(size).fill(null));
    const reserved = Array.from({ length: size }, () => Array(size).fill(false));
    return { matrix, reserved };
}

function placeFinderPatterns(matrix, reserved) {
    placeFinder(matrix, reserved, 0, 0);
    placeFinder(matrix, reserved, matrix.length - 7, 0);
    placeFinder(matrix, reserved, 0, matrix.length - 7);
}

function placeFinder(matrix, reserved, x, y) {
    const pattern = [
        [1, 1, 1, 1, 1, 1, 1],
        [1, 0, 0, 0, 0, 0, 1],
        [1, 0, 1, 1, 1, 0, 1],
        [1, 0, 1, 1, 1, 0, 1],
        [1, 0, 1, 1, 1, 0, 1],
        [1, 0, 0, 0, 0, 0, 1],
        [1, 1, 1, 1, 1, 1, 1]
    ];
    for (let dy = 0; dy < 7; dy++) {
        for (let dx = 0; dx < 7; dx++) {
            const value = pattern[dy][dx];
            setModule(matrix, reserved, x + dx, y + dy, value, true);
        }
    }

    const size = matrix.length;
    for (let i = -1; i <= 7; i++) {
        markWhite(matrix, reserved, x + i, y - 1, size);
        markWhite(matrix, reserved, x + i, y + 7, size);
        markWhite(matrix, reserved, x - 1, y + i, size);
        markWhite(matrix, reserved, x + 7, y + i, size);
    }
}

function markWhite(matrix, reserved, x, y, size) {
    if (x >= 0 && y >= 0 && x < size && y < size && matrix[y][x] === null) {
        setModule(matrix, reserved, x, y, 0, true);
    } else if (x >= 0 && y >= 0 && x < size && y < size) {
        reserved[y][x] = true;
    }
}

function placeTimingPatterns(matrix, reserved) {
    const size = matrix.length;
    for (let i = 8; i < size - 8; i++) {
        const value = i % 2 === 0 ? 1 : 0;
        setModule(matrix, reserved, 6, i, value, true);
        setModule(matrix, reserved, i, 6, value, true);
    }
}

function placeAlignmentPatterns(matrix, reserved, versionInfo) {
    const centers = versionInfo.alignmentCenters;
    if (!centers || centers.length === 0) return;
    for (let i = 0; i < centers.length; i++) {
        for (let j = 0; j < centers.length; j++) {
            const cx = centers[i];
            const cy = centers[j];
            if (reserved[cy][cx]) continue;
            placeAlignmentPattern(matrix, reserved, cx, cy);
        }
    }
}

function placeAlignmentPattern(matrix, reserved, cx, cy) {
    const pattern = [
        [1, 1, 1, 1, 1],
        [1, 0, 0, 0, 1],
        [1, 0, 1, 0, 1],
        [1, 0, 0, 0, 1],
        [1, 1, 1, 1, 1]
    ];
    for (let dy = -2; dy <= 2; dy++) {
        for (let dx = -2; dx <= 2; dx++) {
            setModule(matrix, reserved, cx + dx, cy + dy, pattern[dy + 2][dx + 2], true);
        }
    }
}

function placeDarkModule(matrix, reserved, version) {
    const size = matrix.length;
    const row = 4 * version + 9;
    if (row < size) {
        setModule(matrix, reserved, 8, row, 1, true);
    }
}

function placeDataBits(matrix, reserved, bits) {
    const size = matrix.length;
    const dataModules = [];
    let bitIndex = 0;
    let directionUp = true;
    for (let col = size - 1; col > 0; col -= 2) {
        if (col === 6) col -= 1;
        for (let i = 0; i < size; i++) {
            const row = directionUp ? size - 1 - i : i;
            for (let c = 0; c < 2; c++) {
                const x = col - c;
                const y = row;
                if (reserved[y][x]) continue;
                const bit = bitIndex < bits.length ? bits[bitIndex] : 0;
                bitIndex += 1;
                matrix[y][x] = bit;
                dataModules.push({ x, y, value: bit });
                reserved[y][x] = false;
            }
        }
        directionUp = !directionUp;
    }
    return dataModules;
}

function chooseBestMask(baseMatrix, reserved, dataModules) {
    let bestPattern = 0;
    let bestPenalty = Infinity;
    let bestMatrix = baseMatrix;

    for (let pattern = 0; pattern < 8; pattern++) {
        const candidate = cloneMatrix(baseMatrix);
        applyMask(candidate, dataModules, pattern);
        const penalty = calculatePenalty(candidate);
        if (penalty < bestPenalty) {
            bestPenalty = penalty;
            bestPattern = pattern;
            bestMatrix = candidate;
        }
    }
    // Update base matrix to best candidate to avoid re-applying mask twice
    for (let y = 0; y < baseMatrix.length; y++) {
        for (let x = 0; x < baseMatrix.length; x++) {
            baseMatrix[y][x] = bestMatrix[y][x];
        }
    }
    return { pattern: bestPattern, matrix: bestMatrix };
}

function applyMask(matrix, dataModules, pattern) {
    for (const module of dataModules) {
        const mask = maskFunction(pattern, module.x, module.y) ? 1 : 0;
        matrix[module.y][module.x] = module.value ^ mask;
    }
}

function placeFormatInfo(matrix, reserved, level, maskPattern) {
    const formatBits = computeFormatBits(level, maskPattern);
    const size = matrix.length;

    for (let i = 0; i <= 5; i++) setModule(matrix, reserved, i, 8, getBit(formatBits, i), true);
    setModule(matrix, reserved, 7, 8, getBit(formatBits, 6), true);
    setModule(matrix, reserved, 8, 8, getBit(formatBits, 7), true);
    setModule(matrix, reserved, 8, 7, getBit(formatBits, 8), true);
    for (let i = 9; i < 15; i++) setModule(matrix, reserved, 14 - i, 8, getBit(formatBits, i), true);

    for (let i = 0; i < 8; i++) setModule(matrix, reserved, size - 1 - i, 8, getBit(formatBits, i), true);
    for (let i = 8; i < 15; i++) setModule(matrix, reserved, 8, size - 15 + i, getBit(formatBits, i), true);
}

function reserveFormatInfo(matrix, reserved) {
    const size = matrix.length;
    for (let i = 0; i <= 5; i++) setModule(matrix, reserved, i, 8, 0, true);
    setModule(matrix, reserved, 7, 8, 0, true);
    setModule(matrix, reserved, 8, 8, 0, true);
    setModule(matrix, reserved, 8, 7, 0, true);
    for (let i = 9; i < 15; i++) setModule(matrix, reserved, 14 - i, 8, 0, true);
    for (let i = 0; i < 8; i++) setModule(matrix, reserved, size - 1 - i, 8, 0, true);
    for (let i = 8; i < 15; i++) setModule(matrix, reserved, 8, size - 15 + i, 0, true);
}

function computeFormatBits(level, maskPattern) {
    const levelBits = { L: 0b01, M: 0b00, Q: 0b11, H: 0b10 }[level] ?? 0b01;
    const value = (levelBits << 3) | maskPattern;
    let bits = value << 10;
    const generator = 0b10100110111;
    for (let i = 14; i >= 10; i--) {
        if (((bits >> i) & 1) === 1) {
            bits ^= generator << (i - 10);
        }
    }
    const mask = 0b101010000010010;
    return ((value << 10) | bits) ^ mask;
}

function calculatePenalty(matrix) {
    return penaltyRule1(matrix) + penaltyRule2(matrix) + penaltyRule3(matrix) + penaltyRule4(matrix);
}

function penaltyRule1(matrix) {
    let total = 0;
    for (const row of matrix) {
        total += linePenalty(row);
    }
    const size = matrix.length;
    for (let x = 0; x < size; x++) {
        const column = [];
        for (let y = 0; y < size; y++) column.push(matrix[y][x]);
        total += linePenalty(column);
    }
    return total;
}

function linePenalty(values) {
    let penalty = 0;
    let runColor = values[0];
    let runLength = 1;
    for (let i = 1; i < values.length; i++) {
        if (values[i] === runColor) {
            runLength += 1;
        } else {
            if (runLength >= 5) penalty += 3 + (runLength - 5);
            runColor = values[i];
            runLength = 1;
        }
    }
    if (runLength >= 5) penalty += 3 + (runLength - 5);
    return penalty;
}

function penaltyRule2(matrix) {
    let penalty = 0;
    const size = matrix.length;
    for (let y = 0; y < size - 1; y++) {
        for (let x = 0; x < size - 1; x++) {
            const value = matrix[y][x];
            if (value === matrix[y][x + 1] && value === matrix[y + 1][x] && value === matrix[y + 1][x + 1]) {
                penalty += 3;
            }
        }
    }
    return penalty;
}

function penaltyRule3(matrix) {
    let penalty = 0;
    const size = matrix.length;
    const pattern = [1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0];
    const reversePattern = [0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1];

    for (const row of matrix) {
        penalty += patternPenalty(row, pattern);
        penalty += patternPenalty(row, reversePattern);
    }

    for (let x = 0; x < size; x++) {
        const column = [];
        for (let y = 0; y < size; y++) column.push(matrix[y][x]);
        penalty += patternPenalty(column, pattern);
        penalty += patternPenalty(column, reversePattern);
    }
    return penalty;
}

function patternPenalty(values, pattern) {
    let penalty = 0;
    for (let i = 0; i <= values.length - pattern.length; i++) {
        let matches = true;
        for (let j = 0; j < pattern.length; j++) {
            if (values[i + j] !== pattern[j]) {
                matches = false;
                break;
            }
        }
        if (matches) penalty += 40;
    }
    return penalty;
}

function penaltyRule4(matrix) {
    let darkCount = 0;
    let totalCount = 0;
    for (const row of matrix) {
        for (const value of row) {
            totalCount += 1;
            if (value === 1) darkCount += 1;
        }
    }
    const percentage = (darkCount * 100) / totalCount;
    const deviation = Math.abs(percentage - 50);
    return Math.floor(deviation / 5) * 10;
}

function maskFunction(pattern, x, y) {
    switch (pattern) {
        case 0: return (x + y) % 2 === 0;
        case 1: return y % 2 === 0;
        case 2: return x % 3 === 0;
        case 3: return (x + y) % 3 === 0;
        case 4: return (Math.floor(y / 2) + Math.floor(x / 3)) % 2 === 0;
        case 5: return ((x * y) % 2) + ((x * y) % 3) === 0;
        case 6: return ((((x * y) % 2) + ((x * y) % 3)) % 2) === 0;
        case 7: return ((((x + y) % 2) + ((x * y) % 3)) % 2) === 0;
        default: return (x + y) % 2 === 0;
    }
}

function setModule(matrix, reserved, x, y, value, markReserved) {
    if (x < 0 || y < 0 || y >= matrix.length || x >= matrix.length) return;
    matrix[y][x] = value;
    if (markReserved) reserved[y][x] = true;
}

function toBits(value, length) {
    const result = [];
    for (let i = length - 1; i >= 0; i--) {
        result.push((value >> i) & 1);
    }
    return result;
}

function cloneMatrix(matrix) {
    return matrix.map(row => row.slice());
}

function getBit(value, position) {
    return (value >> position) & 1;
}

function reedSolomon(data, eccLength) {
    const generator = getGeneratorPolynomial(eccLength);
    const message = data.slice();
    message.push(...Array(eccLength).fill(0));
    for (let i = 0; i < data.length; i++) {
        const coefficient = message[i];
        if (coefficient === 0) continue;
        const logCoefficient = GF_LOG[coefficient];
        for (let j = 0; j < generator.length; j++) {
            const idx = i + j;
            const value = message[idx] ^ gfMul(generator[j], logCoefficient);
            message[idx] = value;
        }
    }
    return message.slice(-eccLength);
}

function getGeneratorPolynomial(eccLength) {
    if (GENERATOR_POLY_CACHE.has(eccLength)) {
        return GENERATOR_POLY_CACHE.get(eccLength);
    }
    let poly = [1];
    for (let i = 0; i < eccLength; i++) {
        poly = multiplyPolynomials(poly, [1, gfPow(2, i)]);
    }
    GENERATOR_POLY_CACHE.set(eccLength, poly);
    return poly;
}

function multiplyPolynomials(a, b) {
    const result = Array(a.length + b.length - 1).fill(0);
    for (let i = 0; i < a.length; i++) {
        for (let j = 0; j < b.length; j++) {
            if (a[i] === 0 || b[j] === 0) continue;
            result[i + j] ^= gfMulRaw(a[i], b[j]);
        }
    }
    return result;
}

function gfMulRaw(a, b) {
    if (a === 0 || b === 0) return 0;
    return GF_EXP[(GF_LOG[a] + GF_LOG[b]) % 255];
}

function gfMul(value, logCoefficient) {
    if (value === 0) return 0;
    const logValue = GF_LOG[value];
    return GF_EXP[(logValue + logCoefficient) % 255];
}

function gfPow(base, exponent) {
    return GF_EXP[(GF_LOG[base] * exponent) % 255];
}

function generateGF256Tables() {
    let value = 1;
    for (let i = 0; i < 255; i++) {
        GF_EXP[i] = value;
        GF_LOG[value] = i;
        value = value << 1;
        if (value & 0x100) value ^= 0x11D;
    }
    for (let i = 255; i < 512; i++) {
        GF_EXP[i] = GF_EXP[i - 255];
    }
    GF_LOG[0] = 0;
}

customElements.define('qr-code', QRCodeElement);
