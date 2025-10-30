class DigitalClock extends HTMLElement {
    constructor() {
        super();
        this._mounted = false;
        this._debug = false;
        this._io = null;
        this._timer = null; // per-second tick
        this._templateEl = null; // optional user-provided <template>
        this._contentEl = null; // rendered content wrapper
    }

    connectedCallback() {
        if (this._mounted) return;
        this._mounted = true;
        this._debug = this.hasAttribute('debug');

        // Minimal layout styles
        this.style.display = this.style.display || 'block';
        this.style.position = this.style.position || 'relative';

        this._log('connected');

        // Discover or create template
        this._discoverTemplate();
        if (!this._templateEl) {
            this._templateEl = this._createDefaultTemplate();
            this._log('created default template');
        }

        // Content container
        this._contentEl = document.createElement('div');
        this.appendChild(this._contentEl);

        // Initial render
        this._renderFromTemplate();
        this._updateNow();

        // Setup ticking and visibility handling
        this._setupIntersectionObserver();
        this._startTicking();
    }

    disconnectedCallback() {
        this._stopTicking();
        if (this._io) {
            try { this._io.disconnect(); } catch {}
            this._io = null;
        }
    }

    _discoverTemplate() {
        const tpl = this.querySelector(':scope > template');
        if (!tpl) return;
        this._templateEl = tpl;
        this._log('discovered user template');
    }

    _createDefaultTemplate() {
        const tpl = document.createElement('template');
        const wrapper = document.createElement('div');
        const timeEl = document.createElement('div');
        timeEl.setAttribute('data-clock', 'time');
        timeEl.setAttribute('data-format', 'hh:mm:ss A');
        timeEl.style.fontSize = '4rem';
        timeEl.style.fontWeight = 'bold';
        const dateEl = document.createElement('div');
        dateEl.setAttribute('data-clock', 'date');
        dateEl.setAttribute('data-format', 'dddd, MMMM D, YYYY');
        dateEl.style.fontSize = '1.5rem';
        wrapper.appendChild(timeEl);
        wrapper.appendChild(dateEl);
        tpl.content.appendChild(wrapper);
        return tpl;
    }

    _renderFromTemplate() {
        this._contentEl.innerHTML = '';
        const clone = this._templateEl.content.cloneNode(true);
        this._contentEl.appendChild(clone);
        this._log('rendered from template');
    }

    _setupIntersectionObserver() {
        if (!this.hasAttribute('pause-off-screen')) return;
        this._io = new IntersectionObserver(entries => {
            for (const entry of entries) {
                if (entry.target !== this) continue;
                if (entry.isIntersecting) {
                    this._log('observer: intersecting, resume ticking');
                    this._startTicking();
                } else {
                    this._log('observer: not intersecting, pause ticking');
                    this._stopTicking();
                }
            }
        }, { threshold: 0.01 });
        this._io.observe(this);
    }

    _startTicking() {
        this._stopTicking();
        const tick = () => {
            this._updateNow();
            this._timer = setTimeout(tick, 1000 - (Date.now() % 1000)); // align to the next second
        };
        tick();
        this._log('ticking started');
    }

    _stopTicking() {
        if (this._timer) {
            try { clearTimeout(this._timer); } catch {}
            this._timer = null;
        }
        this._log('ticking stopped');
    }

    _updateNow() {
        const now = new Date();
        // Update all nodes with data-clock
        const nodes = this._contentEl.querySelectorAll('[data-clock]');
        nodes.forEach(node => {
            const type = (node.getAttribute('data-clock') || '').toLowerCase();
            const fmt = node.getAttribute('data-format') || (type === 'time' ? 'hh:mm:ss A' : 'dddd, MMMM D, YYYY');
            node.textContent = this._format(now, fmt);
        });
    }

    // Simple formatter supporting tokens used in tests
    _format(date, format) {
        const pad2 = (n) => String(n).padStart(2, '0');
        const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

        const h24 = date.getHours();
        const h12raw = h24 % 12;
        const h12 = h12raw === 0 ? 12 : h12raw;
        const A = h24 >= 12 ? 'PM' : 'AM';
        const HH = pad2(h24);
        const hh = pad2(h12);
        const mm = pad2(date.getMinutes());
        const ss = pad2(date.getSeconds());
        const dddd = dayNames[date.getDay()];
        const D = String(date.getDate());
        const MMMM = monthNames[date.getMonth()];
        const YYYY = String(date.getFullYear());

        // Replace tokens; order matters to avoid partial replacements
        let out = format;
        out = out.replace(/dddd/g, dddd);
        out = out.replace(/MMMM/g, MMMM);
        out = out.replace(/YYYY/g, YYYY);
        out = out.replace(/hh/g, hh);
        out = out.replace(/HH/g, HH);
        out = out.replace(/mm/g, mm);
        out = out.replace(/ss/g, ss);
        out = out.replace(/\bD\b/g, D);
        out = out.replace(/\bA\b/g, A);
        return out;
    }

    _log(msg, data) {
        if (!this._debug) return;
        try { console.info('[digital-clock]', msg, data ?? ''); } catch {}
    }
}

customElements.define('digital-clock', DigitalClock);
