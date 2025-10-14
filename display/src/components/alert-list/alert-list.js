class AlertList extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this._alerts = [];
        this._intervalId = null;
        this._refreshMs = null; // milliseconds
        this._timeFormat = this.getAttribute('time-format') || 'h:mma';
    }

    static get observedAttributes() {
        return ['url', 'interval', 'time-format', 'time-zone', 'timezone'];
    }

    attributeChangedCallback(name, oldVal, newVal) {
        if (name === 'url' && newVal) {
            this.loadAlerts();
            this._setupRefresh();
        }
        if (name === 'interval') {
            this._setupRefresh();
        }
        if (name === 'time-format') {
            this._timeFormat = newVal || 'h:mma';
            this._render();
        }
        if (name === 'time-zone' || name === 'timezone') {
            this._timeZone = normalizeTimeZone(newVal);
            this._render();
        }
    }

    connectedCallback() {
        this._timeZone = normalizeTimeZone(this.getAttribute('time-zone') || this.getAttribute('timezone'));
        this._timeFormat = this.getAttribute('time-format') || this._timeFormat;
        this._setupRefresh();
        if (this.getAttribute('url')) this.loadAlerts();
    }

    disconnectedCallback() {
        if (this._intervalId) {
            clearInterval(this._intervalId);
            this._intervalId = null;
        }
    }

    _setupRefresh() {
        if (this._intervalId) {
            clearInterval(this._intervalId);
            this._intervalId = null;
        }

        const val = this.getAttribute('interval');
        // support milliseconds if a numeric > 1000, otherwise treat as minutes
        let ms = null;
        if (val) {
            const n = Number(val);
            if (!Number.isNaN(n)) {
                ms = n > 1000 ? n : n * 60 * 1000;
            }
        }

        // default to 30 minutes if not provided
        if (!ms) ms = 30 * 60 * 1000;
        this._refreshMs = ms;

        const url = this.getAttribute('url');
        if (url && ms > 0) {
            this._intervalId = setInterval(() => {
                this.loadAlerts();
            }, ms);
        }
    }

    async loadAlerts() {
        const url = this.getAttribute('url');
        if (!url) return;

        try {
            const res = await fetch(url);
            if (!res.ok) throw new Error('Failed to fetch alerts');
            const json = await res.json();
            this._alerts = Array.isArray(json.alerts) ? json.alerts : (Array.isArray(json) ? json : []);

            // Log alerts that are expired or not yet active to debug console
            try {
                const now = this._getNow();
                for (const a of this._alerts) {
                    const eff = parseEventDateValue(a.effective, true);
                    const exp = parseEventDateValue(a.expires, true);
                    if (exp && now >= exp) {
                        console.debug('alert-list: expired alert', { title: a.title, effective: eff ? eff.toISOString() : null, expires: exp ? exp.toISOString() : null, severity: a.severity });
                    } else if (eff && now < eff) {
                        console.debug('alert-list: not-yet-active alert', { title: a.title, effective: eff ? eff.toISOString() : null, expires: exp ? exp.toISOString() : null, severity: a.severity });
                    }
                }
            } catch (logErr) {
                // swallow logging errors to avoid breaking UI
                console.debug('alert-list: logging error', logErr);
            }

            this._render();
        } catch (err) {
            // on error, hide output
            this._alerts = [];
            this._render();
        }
    }

    _getNow() {
        return new Date();
    }

    _isActive(alert) {
        // parse effective/expires similar to calendar-agenda-view
        const now = this._getNow();
        const eff = parseEventDateValue(alert.effective, true);
        const exp = parseEventDateValue(alert.expires, true);
        if (!eff && !exp) return false;
        if (eff && now < eff) return false;
        if (exp && now >= exp) return false;
        return true;
    }

    _formatTime(dateObj) {
        // reuse renderTimeSegments from calendar-agenda-view if available
        if (typeof renderTimeSegments === 'function') {
            return renderTimeSegments(dateObj, this._timeFormat);
        }

        // fallback simple formatting
        const hours = dateObj.getHours();
        const minutes = dateObj.getMinutes();
        const ampm = hours >= 12 ? 'PM' : 'AM';
        const h12 = hours % 12 || 12;
        return `${h12}:${minutes.toString().padStart(2, '0')}${ampm.toLowerCase()}`;
    }

    _render() {
        // filter active alerts
        const active = (this._alerts || []).filter(a => this._isActive(a));

        if (!active || active.length === 0) {
            // render nothing at all
            this.shadowRoot.innerHTML = '';
            // also remove any host-level content so it's not visible
            this.style.display = 'none';
            return;
        }

        this.style.display = '';
        // render list of active alerts
        const parts = active.map(a => {
            const eff = parseEventDateValue(a.effective, true);
            const exp = parseEventDateValue(a.expires, true);
            const timeRange = eff && exp ? `${this._formatTime(eff)} - ${this._formatTime(exp)}` : (eff ? this._formatTime(eff) : (exp ? this._formatTime(exp) : ''));

            return `
                <div class="alert-item ${escapeHtml(a.severity || '')}">
                    <div class="alert-header">
                        <span class="alert-title">${escapeHtml(a.title || '')}</span>
                        ${timeRange ? `<span class="alert-time">${timeRange}</span>` : ''}
                    </div>
                    ${a.description ? `<div class="alert-desc">${escapeHtml(a.description)}</div>` : ''}
                </div>
            `;
        }).join('');

        this.shadowRoot.innerHTML = `
            <style>
                :host { display: block; }
                .alert-item { padding: 0.25rem 0.5rem; border-radius: 4px; margin-bottom: 0.25rem; background: rgba(255,255,255,0.02); color: var(--text, #fff); }
                .alert-item .alert-header { display:flex; justify-content: space-between; gap: 0.5rem; align-items: baseline; }
                .alert-title { font-weight: 600; }
                .alert-time { font-size: 0.85em; opacity: 0.9; }
                .alert-desc { margin-top: 0.25rem; font-size: 0.95em; color: var(--muted, #bfc9d6); }
                .Severe, .severe { border-left: 4px solid #e74c3c; }
                .Moderate, .moderate { border-left: 4px solid #f39c12; }
                .Minor, .minor { border-left: 4px solid #2ecc71; }
            </style>
            <div class="alert-list">${parts}</div>
        `;
    }
}

customElements.define('alert-list', AlertList);

// copy helper functions from calendar-agenda-view that this component depends on
// We assume calendar-agenda-view is loaded before this file in index.html; if not, these functions will already exist.
// Provide safe fallbacks only if they are not defined.
if (typeof escapeHtml !== 'function') {
    window.escapeHtml = function (str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    };
}

if (typeof parseEventDateValue !== 'function') {
    window.parseEventDateValue = function (value, assumeUtc) {
        if (!value) return null;
        if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : new Date(value.getTime());
        if (typeof value === 'string') {
            let text = value.trim();
            if (text.length === 0) return null;
            if (assumeUtc && !/([zZ]|[+-]\d{2}:?\d{2})$/.test(text)) text += 'Z';
            const parsed = new Date(text);
            return Number.isNaN(parsed.getTime()) ? null : parsed;
        }
        return null;
    };
}

if (typeof normalizeTimeZone !== 'function') {
    window.normalizeTimeZone = function (value) {
        const trimmed = typeof value === 'string' ? value.trim() : '';
        return trimmed.length > 0 ? trimmed : null;
    };
}
