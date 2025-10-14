/**
 * News Ticker Web Component
 * Displays scrolling news articles with separators
 */

class NewsTicker extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.articles = [];
        this._animId = null;
        this._position = 0; // current translateX of scroller
        this.container = null;
        this.content = null; // first content block
        this.clone = null; // duplicate block
        this.scroller = null; // inner scroller that will be translated
        this._lastTs = null;
        this._resizeHandler = this._onResize.bind(this);
    }

    static get observedAttributes() {
        return ['source', 'speed', 'separator', 'interval'];
    }

    connectedCallback() {
        this.render();
        this.loadNews();
        this._startAutoReload();
        window.addEventListener('resize', this._resizeHandler);
    }

    disconnectedCallback() {
        this.stopScrolling();
        this._stopAutoReload();
        window.removeEventListener('resize', this._resizeHandler);
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (oldValue !== newValue) {
            if (name === 'source') {
                this.loadNews();
                this._restartAutoReload();
            } else if (name === 'speed') {
                this.restartScrolling();
            } else if (name === 'separator') {
                // separator change affects layout; rebuild and restart
                if (this.articles.length) {
                    this.displayNews();
                    this.startScrolling();
                }
            }
            else if (name === 'interval') {
                // restart the auto reload timer with new value
                this._restartAutoReload();
            }
        }
    }

    render() {
        this.shadowRoot.innerHTML = `
            <style>
                :host {
                    display: block;
                    width: 100%;
                    height: 100%;
                    overflow: hidden;
                    position: relative;
                    background: rgba(0,0,0,0.3);
                    border-radius: 4px;
                }

                .container {
                    position: relative;
                    width: 100%;
                    height: 100%;
                    overflow: hidden;
                }

                /* scroller contains two copies of the news line (content + clone) */
                .scroller {
                    display: flex;
                    align-items: center;
                    height: 100%;
                    white-space: nowrap;
                    will-change: transform;
                }

                .content {
                    display: inline-flex;
                    align-items: center;
                    height: 100%;
                    white-space: nowrap;
                    font-size: 1rem;
                    line-height: 1.4;
                    color: var(--text);
                }

                .article {
                    display: inline-flex;
                    align-items: center;
                    margin-right: 2rem;
                    animation: none;
                }

                .separator {
                    margin: 0 1rem;
                    color: var(--accent);
                    font-weight: bold;
                }

                .title {
                    font-weight: 600;
                    color: var(--text);
                }

                .source {
                    margin-left: 0.5rem;
                    color: var(--muted);
                    font-size: 0.9em;
                }

                .summary {
                    margin-left: 0.5rem;
                    color: var(--muted);
                    font-size: 0.85em;
                    font-style: italic;
                }

                .loading {
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    width: 100%;
                    height: 100%;
                    color: var(--muted);
                    font-size: 1rem;
                }

                .error {
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    width: 100%;
                    height: 100%;
                    color: #ff6b6b;
                    font-size: 1rem;
                    text-align: center;
                    padding: 1rem;
                }

                @keyframes scroll {
                    0% { transform: translateX(100%); }
                    100% { transform: translateX(-100%); }
                }
            </style>
            <div class="container">
                <div class="loading">Loading news...</div>
            </div>
        `;

        this.container = this.shadowRoot.querySelector('.container');
    }

    async loadNews() {
        // if manual reload triggered, clear any pending reload so we don't double-fire
        if (this._reloadTimer) {
            clearTimeout(this._reloadTimer);
            this._reloadTimer = null;
        }
        const source = this.getAttribute('source');
        if (!source) {
            this.showError('No source attribute provided');
            return;
        }

        try {
            const response = await fetch(source);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            const data = await response.json();
            this.articles = data.articles || [];
            if (this.articles.length === 0) {
                throw new Error('No articles found in news feed');
            }
            this.displayNews();
            this.startScrolling();
        } catch (error) {
            console.error('Error loading news:', error);
            this.showError(`Error loading news: ${error.message}`);
        }
        // restart reload timer after successful or failed load
        this._restartAutoReload();
    }

    // Default auto-reload interval: 30 minutes
    static get DEFAULT_RELOAD_MS() { return 30 * 60 * 1000; }

    _parseInterval() {
        const raw = this.getAttribute('interval');
        if (!raw) return NewsTicker.DEFAULT_RELOAD_MS;
        const n = Number(raw);
        if (!Number.isFinite(n) || n <= 0) return NewsTicker.DEFAULT_RELOAD_MS;
        // if provided in seconds (small numbers), interpret >0 and <10000 as seconds
        if (n < 10000) return n * 1000;
        return n; // assume milliseconds if large
    }

    _startAutoReload() {
        this._stopAutoReload();
        const ms = this._parseInterval();
        this._reloadTimer = setTimeout(() => {
            this.loadNews();
        }, ms);
    }

    _stopAutoReload() {
        if (this._reloadTimer) {
            clearTimeout(this._reloadTimer);
            this._reloadTimer = null;
        }
    }

    _restartAutoReload() {
        this._stopAutoReload();
        this._startAutoReload();
    }

    displayNews() {
        if (!this.container) return;

        const separator = this.getAttribute('separator') || '•';

        // Build the content (single copy)
        this.content = document.createElement('div');
        this.content.className = 'content';

        this.articles.forEach((article, index) => {
            if (index > 0) {
                const sep = document.createElement('span');
                sep.className = 'separator';
                sep.textContent = separator;
                this.content.appendChild(sep);
            }

            const articleEl = document.createElement('span');
            articleEl.className = 'article';

            const titleEl = document.createElement('span');
            titleEl.className = 'title';
            titleEl.textContent = article.title;

            const sourceEl = document.createElement('span');
            sourceEl.className = 'source';
            sourceEl.textContent = `(${article.source})`;

            const summaryEl = document.createElement('span');
            summaryEl.className = 'summary';
            summaryEl.textContent = article.summary || '';

            articleEl.appendChild(titleEl);
            articleEl.appendChild(sourceEl);
            articleEl.appendChild(summaryEl);
            this.content.appendChild(articleEl);
        });

        // Create scroller and add copies of content for seamless looping
        this.scroller = document.createElement('div');
        this.scroller.className = 'scroller';

        // Clear container and append scroller
        this.container.innerHTML = '';
        this.container.appendChild(this.scroller);

        // Append content, then a visible separator, then the cloned copy so users
        // can tell where the loop restarts.
        const loopSep = document.createElement('span');
        loopSep.className = 'separator';
        loopSep.textContent = separator;

        this.scroller.appendChild(this.content);
        this.clone = this.content.cloneNode(true);
        this.scroller.appendChild(loopSep);
        this.scroller.appendChild(this.clone);

        const ensureEnough = () => {
            const containerWidth = this.container.offsetWidth || 0;
            if (containerWidth === 0) return;

            const maxExtraClones = 8; // safety cap
            let appended = 0;

            // Keep appending clones while scroller width is less than twice the container
            // (so there's always enough content to scroll through) but obey the cap.
            while (this.scroller.offsetWidth < containerWidth * 2 && appended < maxExtraClones) {
                const extra = this.content.cloneNode(true);
                this.scroller.appendChild(extra);
                appended++;
            }

            // If we still don't have enough width, add a spacer element as a fallback.
            if (this.scroller.offsetWidth < containerWidth * 2) {
                const spacer = document.createElement('span');
                spacer.className = 'article';
                spacer.style.display = 'inline-block';
                spacer.style.width = (containerWidth) + 'px';
                spacer.innerHTML = '&nbsp;';
                this.scroller.appendChild(spacer);
            }
        };

    requestAnimationFrame(ensureEnough);
    }

    startScrolling() {
        this.stopScrolling();
        const speed = parseFloat(this.getAttribute('speed')) || 50; // pixels per second

        if (!this.scroller || !this.content) return;

        // Ensure layout values are up to date
        const contentWidth = this.content.offsetWidth;
        if (contentWidth === 0) return; // nothing to scroll

        // Reset position and timestamp
        this._position = 0;
        this._lastTs = null;
        // reset transform so we start from a known position
        this.scroller.style.transform = `translateX(0px)`;

        const animate = (ts) => {
            if (!this._lastTs) this._lastTs = ts;
            const dt = (ts - this._lastTs) / 1000; // seconds
            this._lastTs = ts;

            // move left
            this._position -= speed * dt;

            // when we've moved one full content width, wrap by adding contentWidth
            if (this._position <= -contentWidth) {
                this._position += contentWidth;
            }

            this.scroller.style.transform = `translateX(${Math.round(this._position)}px)`;
            this._animId = requestAnimationFrame(animate);
        };

        this._animId = requestAnimationFrame(animate);
    }

    _onResize() {
        // Debounced resize handling
        if (this._resizeTimer) clearTimeout(this._resizeTimer);
        this._resizeTimer = setTimeout(() => {
            // rebuild clones to ensure no blank space and restart
            if (this.articles.length) {
                this.displayNews();
                this.startScrolling();
            }
        }, 150);
    }

    restartScrolling() {
        if (this.articles.length > 0) {
            this.startScrolling();
        }
    }

    stopScrolling() {
        if (this._animId) {
            cancelAnimationFrame(this._animId);
            this._animId = null;
        }
        this._lastTs = null;
    }

    showError(message) {
        if (!this.container) return;
        this.container.innerHTML = `<div class="error">${message}</div>`;
    }
}

// Register the custom element
customElements.define('news-ticker', NewsTicker);