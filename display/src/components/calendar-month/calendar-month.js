class CalendarMonth extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this._today = new Date();
        this._selected = null; // Date object
        this._year = null;
        this._month = null; // 0-based
        this.events = [];
        this.refreshIntervalId = null;
        this.timeZone = normalizeTimeZone(this.getAttribute('time-zone') || this.getAttribute('timezone'));
        this._daysWithEvents = new Set();
    }

    static get observedAttributes() {
        return ['year', 'month', 'select-today', 'selected-date', 'url', 'refresh', 'time-zone', 'timezone'];
    }

    attributeChangedCallback(name, oldVal, newVal) {
        if (name === 'year' || name === 'month') {
            this._year = this.hasAttribute('year') ? parseInt(this.getAttribute('year'), 10) : null;
            this._month = this.hasAttribute('month') ? parseInt(this.getAttribute('month'), 10) : null;
            this._render();
        }
        if (name === 'select-today') {
            if (this.hasAttribute('select-today')) {
                this._selectToday();
            }
            this._render();
        }
        if (name === 'selected-date') {
            const val = this.getAttribute('selected-date');
            if (val) {
                const d = new Date(val);
                if (!Number.isNaN(d.getTime())) {
                    this._selected = d;
                }
            } else {
                this._selected = null;
            }
            this._render();
        }
        if (name === 'url' && newVal) {
            this.fetchEvents(newVal);
            this.setupRefresh();
        }
        if (name === 'refresh') {
            this.setupRefresh();
        }
        if (name === 'time-zone' || name === 'timezone') {
            this.timeZone = normalizeTimeZone(newVal);
            this._render();
        }
    }

    connectedCallback() {
        // initialize month/year to today if not provided
        const now = new Date();
        if (this._year === null) this._year = now.getFullYear();
        if (this._month === null) this._month = now.getMonth();

        if (this.hasAttribute('select-today')) {
            this._selectToday();
        }

        if (this.hasAttribute('selected-date')) {
            const val = this.getAttribute('selected-date');
            const d = new Date(val);
            if (!Number.isNaN(d.getTime())) this._selected = d;
        }

        this._render();
        // start event refresh if url provided
        const url = this.getAttribute('url');
        if (url) {
            this.fetchEvents(url);
            this.setupRefresh();
        }
    }

    _selectToday() {
        const t = new Date();
        this._selected = new Date(t.getFullYear(), t.getMonth(), t.getDate());
        // keep calendar focused on today's month
        this._year = this._selected.getFullYear();
        this._month = this._selected.getMonth();
        this.setAttribute('selected-date', this._selected.toISOString().slice(0,10));
    }

    _onDayClick(evt) {
        const dataset = evt.target.dataset;
        if (!dataset || !dataset.day) return;
        const y = Number(dataset.year);
        const m = Number(dataset.month);
        const d = Number(dataset.day);
        const selected = new Date(y, m, d);
        this._selected = selected;
        this.setAttribute('selected-date', selected.toISOString().slice(0,10));
        this.dispatchEvent(new CustomEvent('date-selected', { detail: { year: y, month: m, day: d, date: selected }, bubbles: true }));
        this._render();
    }

    async fetchEvents(url) {
        try {
            const res = await fetch(url);
            if (!res.ok) throw new Error('Failed to fetch events');
            const json = await res.json();
            this.events = Array.isArray(json) ? json : (Array.isArray(json.events) ? json.events : []);
            this._buildDaysWithEvents();
            this._render();
        } catch (err) {
            // on error, clear events
            this.events = [];
            this._daysWithEvents.clear();
            this._render();
        }
    }

    setupRefresh() {
        if (this.refreshIntervalId) {
            clearInterval(this.refreshIntervalId);
            this.refreshIntervalId = null;
        }
        const refreshAttr = this.getAttribute('refresh');
        const url = this.getAttribute('url');
        const minutes = parseFloat(refreshAttr);
        if (url && minutes > 0) {
            this.refreshIntervalId = setInterval(() => {
                this.fetchEvents(url);
            }, minutes * 60 * 1000);
        }
    }

    disconnectedCallback() {
        if (this.refreshIntervalId) {
            clearInterval(this.refreshIntervalId);
            this.refreshIntervalId = null;
        }
    }

    _buildDaysWithEvents() {
        this._daysWithEvents.clear();
        const targetTimeZone = this.timeZone;
        for (const event of this.events) {
            if (!event) continue;
            const isAllDay = Boolean(event.isAllDay);
            const parsedStart = parseEventDateValue(event.start, !isAllDay);
            if (!parsedStart) continue;
            const displayStart = isAllDay ? parsedStart : convertToTimeZone(parsedStart, targetTimeZone);
            const dayKey = buildDayKey(displayStart);
            this._daysWithEvents.add(dayKey);
        }
    }

    _render() {
        const year = Number.isFinite(this._year) ? this._year : new Date().getFullYear();
        const month = Number.isFinite(this._month) ? this._month : new Date().getMonth();

        const firstOfMonth = new Date(year, month, 1);
        const startWeekday = firstOfMonth.getDay(); // 0=Sun
        const daysInMonth = new Date(year, month+1, 0).getDate();

        // compute previous month's tail days
        const prevDays = startWeekday; // number of blank cells before day 1
        const totalCells = Math.ceil((prevDays + daysInMonth) / 7) * 7;

        const cells = [];
        // date cursor starting at the (1 - prevDays)
        let dayCursor = 1 - prevDays;
        for (let i = 0; i < totalCells; i++, dayCursor++) {
            let cell = null;
            if (dayCursor < 1) {
                // previous month
                const prevDate = new Date(year, month, dayCursor);
                cell = { date: prevDate, inMonth: false };
            } else if (dayCursor > daysInMonth) {
                // next month
                const nextDate = new Date(year, month, dayCursor);
                cell = { date: nextDate, inMonth: false };
            } else {
                const cur = new Date(year, month, dayCursor);
                cell = { date: cur, inMonth: true };
            }
            cells.push(cell);
        }

        const today = new Date();
        const sel = this._selected ? new Date(this._selected.getFullYear(), this._selected.getMonth(), this._selected.getDate()) : null;

        const weekDays = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];

        // build HTML
        const html = [];
        html.push(`<style>
            :host{ display: flex; flex-direction: column; flex: 1 1 auto; align-self: stretch; min-width: 0; min-height: 0; font-family: inherit; color: var(--text, #e6eef6); }
            .calendar { display:flex; width: 100%; flex: 1 1 auto; min-height: 0; box-sizing: border-box; padding: .5rem; background: var(--panel-bg, rgba(255,255,255,0.02)); border-radius:6px; flex-direction:column; }
            .header { display:flex; justify-content: space-between; align-items:center; margin-bottom: .25rem }
            .month-title { font-weight:600 }
            .grid { display:grid; grid-template-columns: repeat(7, 1fr); gap:2px; flex:1 1 auto; align-content:start; min-height: 0 }
            .wd { text-align:center; font-size: .85em; opacity: .8 }
            .cell { min-height: 3rem; display:flex; align-items:center; justify-content:center; cursor: pointer; border-radius:4px; box-sizing:border-box }
            .cell.inMonth{ background: transparent }
            .cell.notInMonth { opacity: .25 }
            .cell.today { box-shadow: inset 0 0 0 2px rgba(255,255,255,0.06); }
            .cell.selected { background: rgba(255,255,255,0.08); font-weight:700 }
            .cell.has-event { border: 2px solid rgba(46, 204, 113, 0.6); }
        </style>`);

        html.push(`<div class="calendar">
            <div class="header">
                <div class="month-title">${firstOfMonth.toLocaleString(undefined, { month: 'long' })} ${year}</div>
                <div class="controls"></div>
            </div>
            <div class="grid">`);

        // weekdays header
        for (const wd of weekDays) {
            html.push(`<div class="wd">${wd}</div>`);
        }

        // day cells
        for (const c of cells) {
            const d = c.date;
            const y = d.getFullYear();
            const m = d.getMonth();
            const day = d.getDate();
            const classes = ['cell'];
            if (c.inMonth) classes.push('inMonth'); else classes.push('notInMonth');
            const dayKey = buildDayKey(d);
            if (today.getFullYear() === y && today.getMonth() === m && today.getDate() === day) classes.push('today');
            if (sel && sel.getFullYear() === y && sel.getMonth() === m && sel.getDate() === day) classes.push('selected');
            if (this._daysWithEvents.has(dayKey)) classes.push('has-event');

            html.push(`<div class="${classes.join(' ')}" data-year="${y}" data-month="${m}" data-day="${day}">${day}</div>`);
        }

        html.push(`</div></div>`);

        this.shadowRoot.innerHTML = html.join('');

        // attach click handler
        const grid = this.shadowRoot.querySelector('.grid');
        grid.removeEventListener('click', this._boundClick);
        this._boundClick = this._onDayClick.bind(this);
        grid.addEventListener('click', this._boundClick);
    }
}

customElements.define('calendar-month', CalendarMonth);
