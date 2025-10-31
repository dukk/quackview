class CalendarMonth extends HTMLElement {
    constructor() {
        super();
        this._mounted = false;
        this._debug = false;
        this._lists = [];
        this._events = [];
        this._container = null;
        this._weekdayHeader = null; // separate weekday header container
        this._titleEl = null; // separate month title container
    this._dayTemplate = null; // <template data-part="day">
        this._eventTemplate = null; // <template data-part="event">
    }

    connectedCallback() {
        if (this._mounted) return;
        this._mounted = true;
        this._debug = this.hasAttribute('debug');

        // Minimal host styles; keep styling primarily external
        this.style.display = this.style.display || 'block';

        this._log('connected');

        // Discover templates or create defaults
        this._discoverTemplates();
        if (!this._dayTemplate) this._dayTemplate = this._createDefaultDayTemplate();
        if (!this._eventTemplate) this._eventTemplate = this._createDefaultEventTemplate();

        // Build DOM containers
        this._buildContainers();

        // Collect event lists and render
        this._collectLists();
        this._loadAllLists().then(() => {
            this._log('lists loaded', { lists: this._lists.length, events: this._events.length });
            this._renderMonth();
        });
    }

    disconnectedCallback() {
        // no timers to clear for now
    }

    _discoverTemplates() {
        // Day and event templates are optional direct children of <calendar-month>
        const templates = Array.from(this.querySelectorAll(':scope > template'));
        for (const tpl of templates) {
            const part = (tpl.getAttribute('data-part') || '').toLowerCase();
            if (part === 'day') this._dayTemplate = tpl;
            if (part === 'event') this._eventTemplate = tpl;
        }
        this._log('templates', { hasDay: !!this._dayTemplate, hasEvent: !!this._eventTemplate });
    }

    _createDefaultDayTemplate() {
        const tpl = document.createElement('template');
        const root = document.createElement('div');
        root.className = 'calendar-day';
        // minimal structure; page CSS can style .calendar-day, .day-number, .events
        const num = document.createElement('div');
        num.className = 'day-number';
        num.setAttribute('data-day', 'number');
        const events = document.createElement('div');
        events.className = 'events';
        events.setAttribute('data-day', 'events');
        root.appendChild(num);
        root.appendChild(events);
        tpl.content.appendChild(root);
        return tpl;
    }

    _createDefaultEventTemplate() {
        const tpl = document.createElement('template');
        const root = document.createElement('div');
        root.className = 'event';
        const time = document.createElement('span');
        time.className = 'event-time';
        time.setAttribute('data-event', 'time');
        const title = document.createElement('span');
        title.className = 'event-title';
        title.setAttribute('data-event', 'title');
        root.appendChild(time);
        root.appendChild(document.createTextNode(' '));
        root.appendChild(title);
        tpl.content.appendChild(root);
        return tpl;
    }

    _buildContainers() {
        this.innerHTML = '';

        // Month title header (normal div)
        this._titleEl = document.createElement('div');
        this._titleEl.className = 'calendar-month-title';
        this.appendChild(this._titleEl);

        // Weekday header wrapper (grid with 7 columns)
        this._weekdayHeader = document.createElement('div');
        this._weekdayHeader.className = 'calendar-weekdays';
        this._weekdayHeader.style.display = 'grid';
        this._weekdayHeader.style.gridTemplateColumns = 'repeat(7, 1fr)';
        this._weekdayHeader.style.gap = '2px';
        this.appendChild(this._weekdayHeader);

        // Day grid container (6x7)
        this._container = document.createElement('div');
        this._container.className = 'calendar-grid';
        // Provide a minimal grid so it works out of the box; users can override with page CSS
        this._container.style.display = 'grid';
        this._container.style.gridTemplateColumns = 'repeat(7, 1fr)';
        this._container.style.gap = '2px';
        this.appendChild(this._container);
    }

    _collectLists() {
        const children = Array.from(this.querySelectorAll(':scope > calendar-month-events'));
        this._lists = children.map((el, idx) => ({
            el,
            index: idx,
            src: el.getAttribute('src'),
            condition: el.getAttribute('condition') || '',
            loaded: false,
            error: null,
            events: [],
        }));
        this._log('collected lists', this._lists.map(l => ({ index: l.index, src: l.src })));
    }

    async _loadAllLists() {
        await Promise.all(this._lists.map(l => this._loadList(l)));
        // Combine all
        this._events = this._lists.filter(l => l.loaded && !l.error).flatMap(l => l.events);
    }

    async _loadList(list) {
        if (!list.src) {
            list.loaded = true;
            return;
        }
        try {
            const resp = await fetch(list.src, { cache: 'no-cache' });
            if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
            const data = await resp.json();
            list.events = this._normalizeEvents(data);
            list.loaded = true;
            this._log('loaded list', { src: list.src, count: list.events.length });
        } catch (err) {
            list.error = String(err);
            list.loaded = true;
            // eslint-disable-next-line no-console
            console.warn('calendar-month: failed to load list', list.src, err);
        }
    }

    _normalizeEvents(data) {
        // Accept: array of objects or { events: [...] }
        let arr = [];
        if (Array.isArray(data)) arr = data;
        else if (data && Array.isArray(data.events)) arr = data.events;
        else return [];
        return arr
            .map(item => {
                if (!item || typeof item !== 'object') return null;
                const title = item.title || item.subject || item.name || '';
                const startStr = item.start || item.startTime || item.startDate;
                const endStr = item.end || item.endTime || item.endDate || startStr;
                let start = startStr ? new Date(startStr) : null;
                let end = endStr ? new Date(endStr) : null;
                if (start && isNaN(start.getTime())) start = null;
                if (end && isNaN(end.getTime())) end = null;
                if (!start) return null;
                // If end missing, assume 1 hour for non-all-day, or same day for all-day
                const isAllDay = !!(item.isAllDay || item.allDay);
                if (!end) {
                    if (isAllDay) {
                        end = new Date(start);
                        end.setDate(end.getDate() + 1);
                    } else {
                        end = new Date(start.getTime() + 60 * 60 * 1000);
                    }
                }
                // Ensure end after start
                if (end <= start) {
                    end = new Date(start.getTime() + (isAllDay ? 24 * 60 * 60 * 1000 : 60 * 60 * 1000));
                }
                return {
                    title,
                    location: item.location || '',
                    start,
                    end,
                    isAllDay,
                    calendar: item.calendar || '',
                    raw: item,
                };
            })
            .filter(Boolean);
    }

    _parseMonthSpec() {
        const spec = (this.getAttribute('month') || 'current').toLowerCase();
        const now = new Date();
        let year = now.getFullYear();
        let month = now.getMonth(); // 0-11
        if (/^\d{4}-\d{1,2}$/.test(spec)) {
            const [y, m] = spec.split('-').map(n => parseInt(n, 10));
            if (!isNaN(y) && !isNaN(m)) {
                year = y;
                month = Math.max(1, Math.min(12, m)) - 1;
            }
        } else if (spec === 'next') {
            month += 1;
            if (month > 11) { month = 0; year += 1; }
        } else if (spec === 'prev' || spec === 'previous') {
            month -= 1;
            if (month < 0) { month = 11; year -= 1; }
        }
        return { year, month };
    }

    _buildMonthDays(year, month) {
        // Start from Sunday of the week containing the 1st
        const first = new Date(year, month, 1);
        const start = new Date(first);
        const firstDOW = this._getFirstDayOfWeekIndex(); // 0..6
        const firstDayOfMonthDOW = first.getDay(); // 0..6
        const offset = (firstDayOfMonthDOW - firstDOW + 7) % 7;
        start.setDate(1 - offset);
        const days = [];
        for (let i = 0; i < 42; i++) {
            const d = new Date(start);
            d.setDate(start.getDate() + i);
            days.push({
                date: d,
                inMonth: d.getMonth() === month,
                events: [],
            });
        }
        return days;
    }

    _eventOccursOnDay(event, dayDate) {
        // Check if any overlap between [start, end) and the day span [day 00:00, next 00:00)
        const startOfDay = new Date(dayDate.getFullYear(), dayDate.getMonth(), dayDate.getDate());
        const endOfDay = new Date(startOfDay);
        endOfDay.setDate(endOfDay.getDate() + 1);
        return event.start < endOfDay && event.end > startOfDay;
    }

    _renderMonth() {
        const { year, month } = this._parseMonthSpec();
        const days = this._buildMonthDays(year, month);

        // Assign events to days
        for (const ev of this._events) {
            for (const day of days) {
                if (this._eventOccursOnDay(ev, day.date)) {
                    day.events.push(ev);
                }
            }
        }

        // Update title and weekday header; then render days
        this._titleEl.textContent = this._formatMonthTitle(year, month);

        // Weekday header row
        this._weekdayHeader.innerHTML = '';
        const dayNames = this._getOrderedDayNames();
        for (const name of dayNames) {
            const el = document.createElement('div');
            el.className = 'calendar-weekday';
            el.textContent = name;
            this._weekdayHeader.appendChild(el);
        }

        // Day cells
        this._container.innerHTML = '';
        for (const day of days) {
            const dayEl = this._renderDay(day);
            this._container.appendChild(dayEl);
        }
        this._log('rendered month', { year, month: month + 1 });
    }

    _formatMonthTitle(year, monthIndex) {
        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        return `${monthNames[monthIndex]} ${year}`;
    }

    _renderDay(day) {
        // Clone day template
        const frag = this._dayTemplate.content.cloneNode(true);
        const root = frag.firstElementChild || frag.firstChild;
        if (root && root.setAttribute) {
            // Add class for in-month days and dataset date
            if (day.inMonth) root.classList.add('calendar-in-month-day');
            // Add event presence and current day classes here for styling
            if (day.events && day.events.length > 0) root.classList.add('calendar-event-day');
            const today = new Date();
            if (
                day.date.getFullYear() === today.getFullYear() &&
                day.date.getMonth() === today.getMonth() &&
                day.date.getDate() === today.getDate()
            ) {
                root.classList.add('calendar-current-day');
            }
            root.setAttribute('data-date', day.date.toISOString().slice(0, 10));
        }

        // Fill day number
        const numNode = this._queryWithin(frag, '[data-day="number"]');
        if (numNode) numNode.textContent = String(day.date.getDate());

        // Events container
        const eventsContainer = this._queryWithin(frag, '[data-day="events"]') || root;
        // Sort events: all-day first, then by start time
        const events = day.events.slice().sort((a, b) => {
            if (a.isAllDay && !b.isAllDay) return -1;
            if (!a.isAllDay && b.isAllDay) return 1;
            return a.start.getTime() - b.start.getTime();
        });
        const showEvents = this.hasAttribute('show-events');
        if (showEvents) {
            for (const ev of events) {
                const evEl = this._renderEvent(ev, day.date);
                eventsContainer.appendChild(evEl);
            }
        }

        const container = document.createElement('div');
        container.appendChild(frag);
        return container.firstElementChild || container.firstChild;
    }

    _renderEvent(ev, dayDate) {
        const frag = this._eventTemplate.content.cloneNode(true);
        const timeNode = this._queryWithin(frag, '[data-event="time"]');
        const titleNode = this._queryWithin(frag, '[data-event="title"]');
        if (timeNode) timeNode.textContent = this._formatEventTime(ev, dayDate);
        if (titleNode) titleNode.textContent = ev.title || '';

        const container = document.createElement('div');
        container.appendChild(frag);
        return container.firstElementChild || container.firstChild;
    }

    _formatEventTime(ev, dayDate) {
        if (ev.isAllDay) return 'All day';
        // If the event spans multiple days, show appropriate indicator
        const startOfDay = new Date(dayDate.getFullYear(), dayDate.getMonth(), dayDate.getDate());
        const endOfDay = new Date(startOfDay);
        endOfDay.setDate(endOfDay.getDate() + 1);
        const showStart = ev.start >= startOfDay && ev.start < endOfDay;
        const showEnd = ev.end > startOfDay && ev.end <= endOfDay;
        if (showStart) return this._formatTime(ev.start);
        if (showEnd) return `until ${this._formatTime(ev.end)}`;
        return 'continues';
    }

    _formatTime(date) {
        const pad2 = (n) => String(n).padStart(2, '0');
        let h = date.getHours();
        const m = pad2(date.getMinutes());
        const A = h >= 12 ? 'PM' : 'AM';
        h = h % 12; if (h === 0) h = 12;
        return `${h}:${m} ${A}`;
    }

    _queryWithin(fragment, selector) {
        if (!fragment) return null;
        if (fragment.querySelector) return fragment.querySelector(selector);
        // Fallback for DocumentFragment without querySelector in some environments
        const container = document.createElement('div');
        container.appendChild(fragment.cloneNode(true));
        return container.querySelector(selector);
    }

    _log(msg, data) {
        if (!this._debug) return;
        try { console.info('[calendar-month]', msg, data ?? ''); } catch {}
    }

    _getFirstDayOfWeekIndex() {
        const map = { sunday: 0, monday: 1, tuesday: 2, wednesday: 3, thursday: 4, friday: 5, saturday: 6 };
        const attr = (this.getAttribute('first-day-of-week') || 'sunday').toLowerCase();
        if (attr in map) return map[attr];
        const asNum = parseInt(attr, 10);
        if (!isNaN(asNum) && asNum >= 0 && asNum <= 6) return asNum;
        return 0;
    }

    _getOrderedDayNames() {
        const names = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        const start = this._getFirstDayOfWeekIndex();
        return names.slice(start).concat(names.slice(0, start));
    }
}

customElements.define('calendar-month', CalendarMonth);
