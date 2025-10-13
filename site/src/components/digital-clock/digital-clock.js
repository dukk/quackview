class DigitalClock extends HTMLElement {
    static get observedAttributes() { return ['format', 'show-date', 'date-format']; }
    constructor() {
        super();
        this.attachShadow({mode: 'open'});
        this.format = this.getAttribute('format') || 'hh:mm:ss a';
        // date display enabled by default; set attribute show-date="false" to disable
        this.showDate = this.hasAttribute('show-date') ? this.getAttribute('show-date') !== 'false' : true;
        this.dateFormat = this.getAttribute('date-format') || 'ddd, MMM D';
        this.timer = null;
    }
    connectedCallback() {
        this.render();
        this.timer = setInterval(() => this.render(), 1000);
    }
    disconnectedCallback() {
        clearInterval(this.timer);
    }
    attributeChangedCallback(name, oldVal, newVal) {
        if (name === 'format') {
            this.format = newVal;
        } else if (name === 'show-date') {
            this.showDate = newVal === null ? true : newVal !== 'false';
        } else if (name === 'date-format') {
            this.dateFormat = newVal || 'ddd, MMM D';
        }
        this.render();
    }
    pad(n) { return n < 10 ? '0' + n : n; }
    render() {
        const now = new Date();
        let h = now.getHours();
        let m = now.getMinutes();
        let s = now.getSeconds();
        let ampm = h >= 12 ? 'PM' : 'AM';
        let hour12 = h % 12 || 12;
    // Build HTML by parsing the time format string into tokens and separators so each segment can be styled
        const tokenRegex = /(hh|h|mm|m|ss|s|a)/g;

        const pad = (n) => n < 10 ? '0' + n : '' + n;
        const getValueForToken = (token) => {
            switch (token) {
                case 'hh': return pad(hour12);
                case 'h': return '' + hour12;
                case 'mm': return pad(m);
                case 'm': return '' + m;
                case 'ss': return pad(s);
                case 's': return '' + s;
                case 'a': return ampm;
                default: return token;
            }
        };

        const escapeHtml = (str) => str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

        let htmlParts = [];
        let lastIndex = 0;
        let match;
        while ((match = tokenRegex.exec(this.format)) !== null) {
            if (match.index > lastIndex) {
                const sep = this.format.slice(lastIndex, match.index);
                htmlParts.push(`<span class="sep">${escapeHtml(sep)}</span>`);
            }
            const token = match[0];
            const value = escapeHtml(getValueForToken(token));
            let cls = 'segment';
            if (token === 'hh' || token === 'h') cls = 'hours';
            else if (token === 'mm' || token === 'm') cls = 'minutes';
            else if (token === 'ss' || token === 's') cls = 'seconds';
            else if (token === 'a') cls = 'ampm';

            htmlParts.push(`<span class="${cls}">${value}</span>`);
            lastIndex = tokenRegex.lastIndex;
        }
        if (lastIndex < this.format.length) {
            const trailing = this.format.slice(lastIndex);
            htmlParts.push(`<span class="sep">${escapeHtml(trailing)}</span>`);
        }

        const html = htmlParts.join('');
        // Prepare date if enabled
        let dateHtml = '';
        if (this.showDate) {
            const dateStr = this.formatDate(now, this.dateFormat);
            dateHtml = `<div class="date">${dateStr}</div>`;
        }

        this.shadowRoot.innerHTML = `
            <style>
                :host {
                    display: inline-block;
                    font-family: inherit;
                    font-size: 2.2rem;
                    letter-spacing: 1px;
                    color: var(--accent, #00d1ff);
                    vertical-align: middle;
                }
                .digital-clock { display:block; }
                .hours { font-weight: 700; }
                .minutes { font-weight: 600; margin: 0 6px; }
                .seconds { font-size: 1.2rem; opacity: 0.8; margin-left: 4px; }
                .ampm { font-size: 1.1rem; opacity: 0.9; margin-left: 8px; }
                .sep { opacity: 0.85; margin: 0 4px; }
                .date { font-size: 1.05rem; color: var(--muted, #bfc9d6); margin-top: 6px; }
            </style>
            <span class="digital-clock">${html}</span>
            ${dateHtml}
        `;
    }

    // Format date according to a simple token set similar in spirit to the time format
    formatDate(dateObj, format) {
        const weekdays = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
        const weekdaysShort = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
        const months = ['January','February','March','April','May','June','July','August','September','October','November','December'];
        const monthsShort = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

        const day = dateObj.getDate();
        const month = dateObj.getMonth() + 1;
        const year = dateObj.getFullYear();
        const weekday = dateObj.getDay();

        const tokenRegex = /(dddd|ddd|MMMM|MMM|YYYY|YY|DD|D|MM|M)/g;

        const pad = (n) => n < 10 ? '0' + n : '' + n;

        return format.replace(tokenRegex, (token) => {
            switch (token) {
                case 'dddd': return weekdays[weekday];
                case 'ddd': return weekdaysShort[weekday];
                case 'MMMM': return months[month-1];
                case 'MMM': return monthsShort[month-1];
                case 'YYYY': return '' + year;
                case 'YY': return String(year).slice(-2);
                case 'DD': return pad(day);
                case 'D': return '' + day;
                case 'MM': return pad(month);
                case 'M': return '' + month;
                default: return token;
            }
        });
    }
}
customElements.define('digital-clock', DigitalClock);