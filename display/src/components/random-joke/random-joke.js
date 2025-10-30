class RandomJoke extends HTMLElement {
    constructor() {
        super();
        this._lists = [];
        this._jokes = [];
        this._mounted = false;
        this._debug = false;
        this._io = null;
        this._timer = null; // autoplay timer
        this._punchlineTimer = null; // reveal timer
        this._currentIndex = -1;
        this._templateEl = null; // <random-joke-template>
        this._contentEl = null; // container for rendered joke
    }

    connectedCallback() {
        if (this._mounted) return;
        this._mounted = true;
        this._debug = this.hasAttribute('debug');

        // Minimal styles
        this.style.display = this.style.display || 'block';
        this.style.position = this.style.position || 'relative';
        this.style.overflow = this.style.overflow || 'hidden';
        this.style.willChange = this.style.willChange || 'opacity, transform';

        this._log('connected');
        this._log('attributes', {
            autoplay: this.hasAttribute('autoplay'),
            fadeIn: this.hasAttribute('fade-in'),
            fadeOut: this.hasAttribute('fade-out'),
            pauseOffScreen: this.hasAttribute('pause-off-screen'),
            refreshOnEnter: this.hasAttribute('refresh-on-screen-enter'),
            displayDurationMs: this._getDisplayDuration(),
            punchlineDelayMs: this._getPunchlineDelay(),
        });

        // Discover template
        this._discoverTemplate();

        // Content container
        this._contentEl = document.createElement('div');
        this._contentEl.style.width = '100%';
        this._contentEl.style.height = '100%';
        this._contentEl.style.opacity = '1';
        this._contentEl.style.transition = 'opacity 400ms ease';
        this.appendChild(this._contentEl);

        // Load lists then start
        this._collectLists();
        this._loadAllLists().then(() => {
            this._log('lists loaded', {
                count: this._lists.length,
                loaded: this._lists.filter(l => l.loaded && !l.error).length,
                errors: this._lists.filter(l => !!l.error).map(l => ({ src: l.src, error: l.error }))
            });
            // Show the first joke (random)
            this.next();
            // IO + autoplay
            this._setupIntersectionObserver();
            if (this.hasAttribute('autoplay')) {
                this._startAutoplay();
            }
        });
    }

    disconnectedCallback() {
        this._stopAutoplay();
        if (this._io) {
            try { this._io.disconnect(); } catch {}
            this._io = null;
        }
    }

    // Template discovery: <random-joke-template> direct child
    _discoverTemplate() {
        const tpl = this.querySelector(':scope > random-joke-template');
        if (!tpl) {
            this._log('no joke template found, will use default rendering');
            return;
        }
        this._templateEl = tpl;
        this._log('discovered random-joke-template');
    }

    // Gather child lists
    _collectLists() {
        const children = Array.from(this.querySelectorAll(':scope > random-joke-list'));
        this._lists = children.map((el, idx) => ({
            el,
            index: idx,
            src: el.getAttribute('src'),
            condition: el.getAttribute('condition') || '',
            jokes: [],
            loaded: false,
            error: null,
        }));
        this._log('collected lists', this._lists.map(l => ({ index: l.index, src: l.src, hasCondition: !!l.condition })));
    }

    async _loadAllLists() {
        await Promise.all(this._lists.map(list => this._loadList(list)));
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
            list.jokes = this._normalizeJokes(data);
            list.loaded = true;
            this._log('loaded list', { src: list.src, count: list.jokes.length });
        } catch (err) {
            list.error = String(err);
            list.loaded = true;
            // eslint-disable-next-line no-console
            console.warn('random-joke: failed to load list', list.src, err);
        }
    }

    _normalizeJokes(data) {
        // Accept: [{ joke, punchline }, ...] or { jokes: [...] } or strings
        let arr = [];
        if (Array.isArray(data)) arr = data;
        else if (data && Array.isArray(data.jokes)) arr = data.jokes;
        else return [];
        return arr
            .map(item => {
                if (typeof item === 'string') {
                    return { setup: item, punchline: '' };
                }
                if (!item || typeof item !== 'object') return null;
                const setup = item.setup || item.joke || item.question || item.text || '';
                const punchline = item.punchline || item.answer || '';
                if (!setup && !punchline) return null;
                return { setup, punchline, ...item };
            })
            .filter(Boolean);
    }

    _eligibleLists() {
        const count = this._lists.length;
        return this._lists.filter((list, i) => this._evaluateCondition(list.condition, i, count));
    }

    _combinedJokes() {
        const lists = this._eligibleLists().filter(l => l.loaded && !l.error);
        const out = [];
        for (const l of lists) out.push(...l.jokes);
        return out;
    }

    _parseDuration(value, fallbackMs = 5000) {
        if (!value) return fallbackMs;
        const m = String(value).trim().match(/^(\d+(?:\.\d+)?)(ms|s)?$/);
        if (!m) return fallbackMs;
        const num = parseFloat(m[1]);
        const unit = m[2] || 's';
        return unit === 'ms' ? num : num * 1000;
    }

    _getDisplayDuration() {
        return this._parseDuration(this.getAttribute('display-duration'), 8000);
    }

    _getPunchlineDelay() {
        return this._parseDuration(this.getAttribute('punchline-display-delay'), 4000);
    }

    async next() {
        // Prepare data
        this._jokes = this._combinedJokes();
        if (this._jokes.length === 0) {
            this._log('no jokes available');
            return;
        }

        // Pick random index avoiding immediate repeat
        const last = this._currentIndex;
        let idx = Math.floor(Math.random() * this._jokes.length);
        if (this._jokes.length > 1 && idx === last) {
            idx = (idx + 1 + Math.floor(Math.random() * (this._jokes.length - 1))) % this._jokes.length;
        }
        this._currentIndex = idx;
        const joke = this._jokes[idx];
        this._log('next joke selected', {
            index: idx,
            total: this._jokes.length,
            setupLen: (joke.setup || '').length,
            punchlineLen: (joke.punchline || '').length
        });

        // Fade out current content if requested
        if (this.hasAttribute('fade-out') && this._contentEl.childElementCount) {
            this._log('fade-out start');
            await this._fadeTo(0);
            this._log('fade-out end');
        }

        // Render new joke
        this._renderJoke(joke);

        // Fade in new content if requested
        if (this.hasAttribute('fade-in')) {
            this._contentEl.style.opacity = '0';
            await Promise.resolve(); // allow style to apply
            this._log('fade-in start');
            await this._fadeTo(1);
            this._log('fade-in end');
        } else {
            this._contentEl.style.opacity = '1';
        }

        // Schedule punchline reveal
        this._clearPunchlineTimer();
        const delay = this._getPunchlineDelay();
        this._punchlineTimer = setTimeout(() => {
            this._revealPunchline();
        }, delay);
        this._log('punchline scheduled', { delayMs: delay });
        const displayMs = this._getDisplayDuration();
        if (delay >= displayMs) {
            this._log('warning: punchline delay >= display duration; punchline may never show before next()', { delayMs: delay, displayMs });
        }

        // Reset autoplay timer
        if (this.hasAttribute('autoplay')) {
            this._restartAutoplay();
        }
    }

    _renderJoke(joke) {
        this._contentEl.innerHTML = '';

        if (this._templateEl) {
            // Clone template and fill [data-joke]
            const wrapper = document.createElement('div');
            const tplClone = this._templateEl.cloneNode(true);
            const nodes = tplClone.querySelectorAll('[data-joke]');
            this._log('render with template', { nodes: nodes.length });
            nodes.forEach(node => {
                const key = node.getAttribute('data-joke');
                if (key === 'setup') node.textContent = joke.setup || '';
                else if (key === 'punchline') node.textContent = joke.punchline || '';
                else node.textContent = joke[key] || '';
            });
            // Initially hide punchline
            const punchNodes = tplClone.querySelectorAll('[data-joke="punchline"]');
            this._log('hide punchline nodes', { count: punchNodes.length });
            punchNodes.forEach(n => {
                n.style.visibility = 'hidden';
            });
            while (tplClone.firstChild) wrapper.appendChild(tplClone.firstChild);
            this._contentEl.appendChild(wrapper);
        } else {
            // Default rendering
            const setupEl = document.createElement('div');
            setupEl.style.fontSize = '2rem';
            setupEl.style.fontWeight = 'bold';
            setupEl.style.marginBottom = '1rem';
            setupEl.textContent = joke.setup || '';

            const punchEl = document.createElement('div');
            punchEl.style.fontSize = '1.5rem';
            punchEl.textContent = joke.punchline || '';
            punchEl.style.visibility = 'hidden';
            punchEl.classList.add('punchline');

            this._contentEl.appendChild(setupEl);
            this._contentEl.appendChild(punchEl);
            this._log('render with default template');
        }
    }

    _revealPunchline() {
        const punchNodes = this._contentEl.querySelectorAll('[data-joke="punchline"], .punchline');
        if (punchNodes.length > 0) {
            this._log('reveal punchline', { count: punchNodes.length });
            punchNodes.forEach(n => {
                n.style.visibility = 'visible';
            });
        } else {
            // Fallback: reveal all children if no explicit punchline marker found
            const hiddenNodes = Array.from(this._contentEl.querySelectorAll('*'));
            this._log('reveal punchline fallback', { hiddenNodes: hiddenNodes.length });
            hiddenNodes.forEach(n => {
                if (n && n.style && n.style.visibility === 'hidden') {
                    n.style.visibility = 'visible';
                }
            });
        }
    }

    _restartAutoplay() {
        this._clearAutoplayTimer();
        this._startAutoplay();
    }

    _startAutoplay() {
        this._clearAutoplayTimer();
        const duration = this._getDisplayDuration();
        this._timer = setTimeout(() => {
            this.next();
        }, duration);
        this._log('autoplay started', { durationMs: duration });
    }

    _stopAutoplay() {
        this._clearAutoplayTimer();
        this._clearPunchlineTimer();
        this._log('autoplay stopped');
    }

    _clearAutoplayTimer() {
        if (this._timer) {
            try { clearTimeout(this._timer); } catch {}
            this._timer = null;
        }
    }

    _clearPunchlineTimer() {
        if (this._punchlineTimer) {
            try { clearTimeout(this._punchlineTimer); } catch {}
            this._punchlineTimer = null;
        }
    }

    _setupIntersectionObserver() {
        if (!this.hasAttribute('pause-off-screen')) return;
        this._io = new IntersectionObserver(entries => {
            for (const entry of entries) {
                if (entry.target !== this) continue;
                if (entry.isIntersecting) {
                    this._log('observer: intersecting, resume');
                    if (this.hasAttribute('refresh-on-screen-enter')) {
                        this._log('refresh-on-screen-enter: showing new joke');
                        this.next().then(() => {
                            if (this.hasAttribute('autoplay')) this._startAutoplay();
                        });
                    } else {
                        if (this.hasAttribute('autoplay')) this._startAutoplay();
                    }
                } else {
                    this._log('observer: not intersecting, pause');
                    this._stopAutoplay();
                }
            }
        }, { threshold: 0.01 });
        this._io.observe(this);
    }

    _evaluateCondition(expr, index, count) {
        if (!expr) return true;
        try {
            const ctx = this._buildConditionContext(index, count);
            const fn = new Function('ctx',
                'const { now, nowMs, hour, minute, second, day, date, month, isWeekend, isWeekday, index, count, param, matchesMedia, random } = ctx;\n' +
                'return !!(' + expr + ');'
            );
            const result = !!fn(ctx);
            this._log('condition', { index, expr, result });
            return result;
        } catch (err) {
            // eslint-disable-next-line no-console
            console.warn('random-joke condition error:', err);
            return false;
        }
    }

    _buildConditionContext(index, count) {
        const now = new Date();
        const day = now.getDay();
        const url = new URL(window.location.href);
        return {
            now,
            nowMs: Date.now(),
            hour: now.getHours(),
            minute: now.getMinutes(),
            second: now.getSeconds(),
            day,
            date: now.getDate(),
            month: now.getMonth() + 1,
            isWeekend: day === 0 || day === 6,
            isWeekday: !(day === 0 || day === 6),
            index,
            count,
            param: (name) => url.searchParams.get(name),
            matchesMedia: (q) => window.matchMedia(q).matches,
            random: Math.random(),
        };
    }

    _fadeTo(targetOpacity) {
        return new Promise(resolve => {
            const duration = 400; // match CSS transition
            this._contentEl.style.transition = `opacity ${duration}ms ease`;
            const onEnd = () => {
                this._contentEl.removeEventListener('transitionend', onEnd);
                resolve();
            };
            this._contentEl.addEventListener('transitionend', onEnd, { once: true });
            requestAnimationFrame(() => {
                this._contentEl.style.opacity = String(targetOpacity);
            });
        });
    }

    _log(msg, data) {
        if (!this._debug) return;
        try { console.info('[random-joke]', msg, data ?? ''); } catch {}
    }
}

customElements.define('random-joke', RandomJoke);
