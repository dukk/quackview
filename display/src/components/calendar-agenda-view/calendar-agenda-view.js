const DEFAULT_DAY_FORMAT = 'dddd, MMM D';
const DEFAULT_TIME_FORMAT = 'h:mm a';

class CalendarAgendaView extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.events = [];
        this.refreshIntervalId = null;
        this.dayFormat = this.getAttribute('day-format') || DEFAULT_DAY_FORMAT;
        this.timeFormat = this.getAttribute('time-format') || DEFAULT_TIME_FORMAT;
        this.timeZone = normalizeTimeZone(this.getAttribute('time-zone') || this.getAttribute('timezone'));
    }

    static get observedAttributes() {
        return ['url', 'refresh', 'day-format', 'time-format', 'external-scroll', 'time-zone', 'timezone'];
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'url' && newValue) {
            this.fetchEvents(newValue);
            this.setupRefresh();
        }
        if (name === 'refresh') {
            this.setupRefresh();
        }
        if (name === 'day-format') {
            this.dayFormat = newValue || DEFAULT_DAY_FORMAT;
            this.render();
        }
        if (name === 'time-format') {
            this.timeFormat = newValue || DEFAULT_TIME_FORMAT;
            this.render();
        }
        if (name === 'external-scroll') {
            this.render();
        }
        if (name === 'time-zone' || name === 'timezone') {
            this.timeZone = normalizeTimeZone(newValue);
            this.render();
        }
    }

    async fetchEvents(url) {
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error('Failed to fetch events');
            this.events = await response.json();
            this.render();
        } catch (e) {
            this.shadowRoot.innerHTML = `<p>Error loading events.</p>`;
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

    connectedCallback() {
        this.setupRefresh();
    }

    disconnectedCallback() {
        if (this.refreshIntervalId) {
            clearInterval(this.refreshIntervalId);
        }
    }

    render() {
        // Group events by day
        const groups = {};
        const targetTimeZone = this.timeZone;
        for (const event of this.events) {
            if (!event) {
                continue;
            }

            const isAllDay = Boolean(event.isAllDay);
            const parsedStart = parseEventDateValue(event.start, !isAllDay);
            if (!parsedStart) {
                continue;
            }

            const displayStart = isAllDay ? parsedStart : convertToTimeZone(parsedStart, targetTimeZone);
            const dayKeyDate = isAllDay ? parsedStart : displayStart;
            const dayKey = buildDayKey(dayKeyDate);

            if (!groups[dayKey]) {
                groups[dayKey] = [];
            }

            groups[dayKey].push({
                ...event,
                _parsedStart: displayStart,
                _originalStart: parsedStart
            });
        }

        // Sort days ascending
        const sortedDays = Object.keys(groups).sort();

        if (sortedDays.length === 0) {
            this.shadowRoot.innerHTML = `
                <style>
                    :host { display: block; width: 100%; height: 100%; box-sizing: border-box; }
                    .empty { font-style: italic; color: #888; }
                </style>
                <div class="empty">No upcoming events.</div>
            `;
            return;
        }

        // Render grouped agenda
        this.shadowRoot.innerHTML = `
            <style>
                :host { display: block; width: 100%; height: 100%; box-sizing: border-box; }
                .agenda-list { display: flex; flex-direction: column; gap: .5em; height: 100%; overflow: auto; padding-right: 0.25rem; }
                .agenda-day { margin-bottom: .1em; }
                .agenda-date { font-weight: bold; font-size: 1.1em; margin-bottom: 0.1em; display: block; border-bottom: 1px solid #ccc; }
                ul { list-style: none; padding: 0; margin: 0; }
                li { margin: 0.5em 0; padding: 0.5em; display: flex; align-items: baseline; }
                .event-time { font-weight: bold; display: inline-flex; align-items: baseline; gap: 0.25em; min-width: 4.5em; }
                .event-time.all-day { display: inline-block; }
                .event-details { display: flex; flex-direction: column; text-align: left; margin-left: 1em; }
                .event-title { flex: 1; }
                .event-location { color: #888; font-size: 0.80em; }
                .hours, .minutes, .seconds, .ampm { display: inline-block; monospace }
                .event-time .sep { opacity: 0.75; margin: 0 2px; }
                .agenda-date .sep { opacity: 0.75; margin: 0 4px; }
                .agenda-date .date-segment { display: inline-block; }
                .agenda-day > div { display: block; }
                :host([external-scroll]) .agenda-list { height: auto; overflow: visible; }
            </style>
            <div class="agenda-list">
                ${sortedDays.map(dayKey => {
                    const events = groups[dayKey];
                    // Sort: all-day first, then by start time
                    const allDay = events.filter(ev => ev.isAllDay);
                    const timed = events.filter(ev => !ev.isAllDay).sort((a,b) => a._parsedStart - b._parsedStart);
                    const dayDate = events[0]._parsedStart;
                    const dayLabel = renderDateSegments(dayDate, this.dayFormat);
                    return `
                        <div class="agenda-day">
                            <span class="agenda-date">${dayLabel}</span>
                            <ul>
                                ${allDay.map(ev => `
                                    <li>
                                        <span class="event-time all-day">All day</span>
                                        <span class="event-details">
                                            <span class="event-title">${ev.subject}</span>
                                            ${ev.location ? `<span class="event-location">${ev.location}</span>` : ''}
                                        </span>
                                    </li>
                                `).join('')}
                                ${timed.map(ev => {
                                    const timeStr = renderTimeSegments(ev._parsedStart, this.timeFormat);
                                    return `
                                        <li>
                                            <span class="event-time">${timeStr}</span>
                                            <span class="event-details">
                                                <span class="event-title">${ev.subject}</span>
                                                ${ev.location ? `<span class="event-location">${ev.location}</span>` : ''}
                                            </span>
                                        </li>
                                    `;
                                }).join('')}
                            </ul>
                        </div>
                    `;
                }).join('')}
            </div>
        `;
    }
}

const WEEKDAYS = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
const WEEKDAYS_SHORT = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
const MONTHS = ['January','February','March','April','May','June','July','August','September','October','November','December'];
const MONTHS_SHORT = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

function normalizeTimeZone(value) {
    const trimmed = typeof value === 'string' ? value.trim() : '';
    return trimmed.length > 0 ? trimmed : null;
}

function parseEventDateValue(value, assumeUtc) {
    if (!value) {
        return null;
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : new Date(value.getTime());
    }

    if (typeof value === 'string') {
        let text = value.trim();
        if (text.length === 0) {
            return null;
        }

        if (assumeUtc && !hasExplicitTimeZone(text)) {
            text += 'Z';
        }

        const parsed = new Date(text);
        return Number.isNaN(parsed.getTime()) ? null : parsed;
    }

    return null;
}

function hasExplicitTimeZone(text) {
    return /([zZ]|[+-]\d{2}:?\d{2})$/.test(text);
}

function convertToTimeZone(dateObj, timeZone) {
    if (!timeZone) {
        return new Date(dateObj.getTime());
    }

    try {
        const formatter = new Intl.DateTimeFormat('en-US', {
            timeZone,
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });

        const parts = formatter.formatToParts(dateObj);
        const lookup = {};
        for (const part of parts) {
            if (part.type !== 'literal') {
                lookup[part.type] = part.value;
            }
        }

        const year = lookup.year || '0000';
        const month = lookup.month || '01';
        const day = lookup.day || '01';
        const hour = lookup.hour || '00';
        const minute = lookup.minute || '00';
        const second = lookup.second || '00';

        // Create a local Date that reflects the wall-clock time of the target zone
        return new Date(`${year}-${month}-${day}T${hour}:${minute}:${second}`);
    } catch (err) {
        console.warn('calendar-agenda-view: invalid time-zone attribute', timeZone, err);
        return new Date(dateObj.getTime());
    }
}

function buildDayKey(dateObj) {
    const year = dateObj.getFullYear();
    const month = dateObj.getMonth() + 1;
    const day = dateObj.getDate();
    return `${year}-${month.toString().padStart(2, '0')}-${day.toString().padStart(2, '0')}`;
}

function renderDateSegments(dateObj, format) {
    const fmt = typeof format === 'string' && format.length > 0 ? format : DEFAULT_DAY_FORMAT;
    const tokenRegex = /(dddd|ddd|MMMM|MMM|YYYY|YY|DD|D|MM|M)/g;
    const htmlParts = [];
    let lastIndex = 0;
    let match;

    while ((match = tokenRegex.exec(fmt)) !== null) {
        if (match.index > lastIndex) {
            const literal = fmt.slice(lastIndex, match.index);
            htmlParts.push(`<span class="sep">${escapeHtml(literal)}</span>`);
        }

        const token = match[0];
        const value = escapeHtml(getDateTokenValue(token, dateObj));
        const cls = getDateTokenClass(token);
        htmlParts.push(`<span class="date-segment ${cls}">${value}</span>`);
        lastIndex = tokenRegex.lastIndex;
    }

    if (lastIndex < fmt.length) {
        const trailing = fmt.slice(lastIndex);
        htmlParts.push(`<span class="sep">${escapeHtml(trailing)}</span>`);
    }

    return htmlParts.join('');
}

function renderTimeSegments(dateObj, format) {
    const fmt = typeof format === 'string' && format.length > 0 ? format : DEFAULT_TIME_FORMAT;
    const hours24 = dateObj.getHours();
    const minutes = dateObj.getMinutes();
    const seconds = dateObj.getSeconds();
    const ampm = hours24 >= 12 ? 'PM' : 'AM';
    const hours12 = hours24 % 12 || 12;

    const tokenRegex = /(hh|h|mm|m|ss|s|a)/g;
    const htmlParts = [];
    let lastIndex = 0;
    let match;

    const pad = (n) => (n < 10 ? '0' + n : '' + n);

    while ((match = tokenRegex.exec(fmt)) !== null) {
        if (match.index > lastIndex) {
            const literal = fmt.slice(lastIndex, match.index);
            htmlParts.push(`<span class="sep">${escapeHtml(literal)}</span>`);
        }

        const token = match[0];
        let value = '';
        switch (token) {
            case 'hh': value = pad(hours12); break;
            case 'h': value = '' + hours12; break;
            case 'mm': value = pad(minutes); break;
            case 'm': value = '' + minutes; break;
            case 'ss': value = pad(seconds); break;
            case 's': value = '' + seconds; break;
            case 'a': value = ampm; break;
            default: value = token; break;
        }

        const cls = getTimeTokenClass(token);
        htmlParts.push(`<span class="${cls}">${escapeHtml(value)}</span>`);
        lastIndex = tokenRegex.lastIndex;
    }

    if (lastIndex < fmt.length) {
        const trailing = fmt.slice(lastIndex);
        htmlParts.push(`<span class="sep">${escapeHtml(trailing)}</span>`);
    }

    return htmlParts.join('');
}

function getDateTokenValue(token, dateObj) {
    const day = dateObj.getDate();
    const monthIdx = dateObj.getMonth();
    const year = dateObj.getFullYear();
    const weekday = dateObj.getDay();
    const pad = (n) => (n < 10 ? '0' + n : '' + n);

    switch (token) {
        case 'dddd': return WEEKDAYS[weekday];
        case 'ddd': return WEEKDAYS_SHORT[weekday];
        case 'MMMM': return MONTHS[monthIdx];
        case 'MMM': return MONTHS_SHORT[monthIdx];
        case 'MM': return pad(monthIdx + 1);
        case 'M': return '' + (monthIdx + 1);
        case 'DD': return pad(day);
        case 'D': return '' + day;
        case 'YYYY': return '' + year;
        case 'YY': return String(year).slice(-2);
        default: return token;
    }
}

function getDateTokenClass(token) {
    switch (token) {
        case 'dddd': return 'date-weekday date-weekday-long';
        case 'ddd': return 'date-weekday date-weekday-short';
        case 'MMMM': return 'date-month date-month-long';
        case 'MMM': return 'date-month date-month-short';
        case 'MM': return 'date-month date-month-number-padded';
        case 'M': return 'date-month date-month-number';
        case 'DD': return 'date-day date-day-number-padded';
        case 'D': return 'date-day date-day-number';
        case 'YYYY': return 'date-year date-year-full';
        case 'YY': return 'date-year date-year-short';
        default: return 'date-segment';
    }
}

function getTimeTokenClass(token) {
    switch (token) {
        case 'hh':
        case 'h':
            return 'hours';
        case 'mm':
        case 'm':
            return 'minutes';
        case 'ss':
        case 's':
            return 'seconds';
        case 'a':
            return 'ampm';
        default:
            return 'segment';
    }
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

/* Example event object:
{
    "subject": "Columbus Day",
    "location": "United States",
    "start": "2025-10-13T00:00:00.0000000",
    "end": "2025-10-14T00:00:00.0000000",
    "isAllDay": true,
    "calendar": "United States holidays",
    "account": "blah@example.com"
  }
*/
customElements.define('calendar-agenda-view', CalendarAgendaView);