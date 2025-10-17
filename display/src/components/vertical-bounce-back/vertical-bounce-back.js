const DEFAULT_SPEED = 20; // pixels per second
const DEFAULT_DELAY = 1500; // milliseconds

class VerticalBounceBack extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.innerHTML = `
            <style>
                :host {
                    display: block;
                    overflow: hidden;
                    position: relative;
                }

                .viewport {
                    width: 100%;
                    height: 100%;
                    overflow: hidden;
                    position: relative;
                    align-content: top;
                }

                .inner {
                    will-change: transform;
                    transform: translateY(0);
                }
            </style>
            <div class="viewport">
                <div class="inner"><slot></slot></div>
            </div>
        `;

        this.viewport = this.shadowRoot.querySelector('.viewport');
        this.inner = this.shadowRoot.querySelector('.inner');
        this.slotElement = this.shadowRoot.querySelector('slot');

        this.speed = DEFAULT_SPEED;
        this.delay = DEFAULT_DELAY;
        this.offset = 0;
        this.maxScroll = 0;
        this.state = 'idle';
        this.direction = 1; // 1 = down, -1 = up
        this.pauseRemaining = 0;
        this.lastTimestamp = null;
        this.rafId = null;

        this.step = this.step.bind(this);
        this.handleSlotChange = this.handleSlotChange.bind(this);
        this.resizeObserver = new ResizeObserver(() => this.queueMetricsUpdate());
        this.pendingMetricsUpdate = false;
    }

    static get observedAttributes() {
        return ['speed', 'delay'];
    }

    attributeChangedCallback(name, _oldValue, _newValue) {
        if (name === 'speed' || name === 'delay') {
            this.readAttributes();
            this.restartAnimation();
        }
    }

    connectedCallback() {
        this.readAttributes();
        this.slotElement.addEventListener('slotchange', this.handleSlotChange);
        this.resizeObserver.observe(this);
        this.resizeObserver.observe(this.viewport);
        this.resizeObserver.observe(this.inner);
        this.queueMetricsUpdate();
    }

    disconnectedCallback() {
        this.stopAnimation();
        this.slotElement.removeEventListener('slotchange', this.handleSlotChange);
        this.resizeObserver.disconnect();
    }

    handleSlotChange() {
        this.queueMetricsUpdate();
    }

    readAttributes() {
        const speedAttr = parseFloat(this.getAttribute('speed'));
        this.speed = Number.isFinite(speedAttr) && speedAttr > 0 ? speedAttr : DEFAULT_SPEED;

        const delayAttr = parseFloat(this.getAttribute('delay'));
        this.delay = Number.isFinite(delayAttr) && delayAttr >= 0 ? delayAttr : DEFAULT_DELAY;
    }

    queueMetricsUpdate() {
        if (this.pendingMetricsUpdate) return;
        this.pendingMetricsUpdate = true;
        requestAnimationFrame(() => {
            this.pendingMetricsUpdate = false;
            this.updateMetrics();
        });
    }

    updateMetrics() {
        const viewportHeight = this.viewport.clientHeight;
        const contentHeight = this.inner.scrollHeight;
        this.maxScroll = Math.max(0, contentHeight - viewportHeight);

        console.debug('[vertical-bounceback] metrics', {
            viewportHeight,
            contentHeight,
            maxScroll: this.maxScroll,
            offset: this.offset,
            direction: this.direction,
            state: this.state
        });

        if (this.maxScroll <= 0) {
            this.offset = 0;
            this.applyTransform();
            this.stopAnimation();
            this.state = 'idle';
            this.direction = 1;
            return;
        }

        if (this.offset > this.maxScroll) {
            this.offset = this.maxScroll;
            this.applyTransform();
        }

        if (this.rafId === null) {
            //console.debug('[vertical-bounceback] starting animation loop');
            this.resetState();
            this.rafId = requestAnimationFrame(this.step);
        }
    }

    resetState() {
        this.offset = 0;
        this.applyTransform();
        this.state = 'moving';
        this.direction = 1;
        this.pauseRemaining = 0;
        this.lastTimestamp = null;
    }

    restartAnimation() {
        if (!this.isConnected) return;
        if (this.rafId !== null) {
            this.stopAnimation();
        }
        this.queueMetricsUpdate();
    }

    stopAnimation() {
        if (this.rafId !== null) {
            cancelAnimationFrame(this.rafId);
            this.rafId = null;
        }
    }

    step(timestamp) {
        if (this.lastTimestamp === null) {
            this.lastTimestamp = timestamp;
        }
        const deltaMs = timestamp - this.lastTimestamp;
        this.lastTimestamp = timestamp;

        if (this.state === 'pausing') {
            this.pauseRemaining -= deltaMs;
            if (this.pauseRemaining <= 0) {
                this.pauseRemaining = 0;
                this.state = 'moving';
            }
        } else {
            const deltaPx = (this.speed * deltaMs) / 1000;
            this.offset += this.direction * deltaPx;

            if (this.offset >= this.maxScroll) {
                this.offset = this.maxScroll;
                this.direction = -1;
                this.startPause();
            } else if (this.offset <= 0) {
                this.offset = 0;
                this.direction = 1;
                this.startPause();
            }

            this.applyTransform();
        }

        if (this.maxScroll > 0) {
            this.rafId = requestAnimationFrame(this.step);
        } else {
            this.stopAnimation();
        }
    }

    startPause() {
        if (this.delay > 0) {
            this.state = 'pausing';
            this.pauseRemaining = this.delay;
        } else {
            this.state = 'moving';
        }
        this.applyTransform();
    }

    applyTransform() {
        this.inner.style.transform = `translateY(${-this.offset}px)`;
    }
}

customElements.define('vertical-bounce-back', VerticalBounceBack);
