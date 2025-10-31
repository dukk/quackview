class VerticalBounceBack extends HTMLElement {
    constructor() {
        super();
        this._mounted = false;
        this._debug = false;
        this._viewport = null; // scrollable container
        this._content = null;  // content wrapper
        this._animId = null;
        this._lastTs = 0;
        this._dir = 1; // 1=down, -1=up
        this._pausedUntil = 0; // timestamp until resume after bounce
        this._resizeObs = null;
        this._running = false;
        this._fillObs = null;
        this._onWindowResize = () => this._applyFillToViewport();
        this._styleEl = null;
        this._scopeId = null;
    }

    static get observedAttributes() { return ['autostart', 'fill']; }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'autostart' && oldValue !== newValue && this._mounted) {
            const shouldStart = this._getAutostart();
            if (shouldStart) this._startLoop(); else this._stopLoop();
            this._log('autostart-changed', { value: newValue, running: this._running });
        }
        if (name === 'fill' && oldValue !== newValue && this._mounted) {
            this._teardownFillBehavior();
            this._setupFillBehavior();
            this._log('fill-changed', { value: newValue });
        }
    }

    connectedCallback() {
        if (this._mounted) return;
        this._mounted = true;
        this._debug = this.hasAttribute('debug');

        // Minimal critical styles
        this.style.display = this.style.display || 'block';
        this.style.position = this.style.position || 'relative';

        // Build viewport and move children inside
        this._buildViewport();

    // Inject minimal, scoped CSS to hide scrollbars by default
    this._ensureScopedScrollbarStyle();

        // Observe size changes to adapt max scroll
        this._setupResizeObserver();

        // Default fill behavior
        this._setupFillBehavior();

        // Start animation loop if autostart
        if (this._getAutostart()) {
            this._startLoop();
        }

        this._log('connected', {
            speed: this._getSpeed(),
            bounceDelayMs: this._getBounceDelay(),
            autostart: this._getAutostart(),
        });
    }

    disconnectedCallback() {
        this._stopLoop();
        if (this._resizeObs) {
            try { this._resizeObs.disconnect(); } catch {}
            this._resizeObs = null;
        }
        this._teardownFillBehavior();
    }

    _buildViewport() {
        const viewport = document.createElement('div');
        viewport.className = 'vbb-viewport';
        viewport.style.width = '100%';
        viewport.style.height = '100%';
        viewport.style.overflow = 'auto'; // real scrolling so position:sticky works
        viewport.style.willChange = 'scroll-position';

        const content = document.createElement('div');
        // Avoid forcing styles here; allow page to style children
        while (this.firstChild) {
            content.appendChild(this.firstChild);
        }
        viewport.appendChild(content);
        this.appendChild(viewport);

        this._viewport = viewport;
        this._content = content;
    }

    _ensureScopedScrollbarStyle() {
        if (!this._scopeId) {
            this._scopeId = `vbb-${Math.random().toString(36).slice(2, 9)}`;
            this.setAttribute('data-vbb-id', this._scopeId);
        }
        if (this._styleEl) return;
        const style = document.createElement('style');
        style.type = 'text/css';
        style.textContent = `
/* Hide scrollbars only within this component's viewport */
[data-vbb-id="${this._scopeId}"] .vbb-viewport { scrollbar-width: none; -ms-overflow-style: none; }
[data-vbb-id="${this._scopeId}"] .vbb-viewport::-webkit-scrollbar { display: none; }
`;
        this.insertBefore(style, this.firstChild);
        this._styleEl = style;
    }

    _getFillMode() {
        // Modes: 'parent' (default), 'vh' (use window.innerHeight), 'none' (disable)
        const raw = (this.getAttribute('fill') || '').trim().toLowerCase();
        if (!raw) return 'parent';
        if (raw === 'none' || raw === 'false' || raw === 'off' || raw === '0') return 'none';
        if (raw === 'vh' || raw === 'viewport' || raw === 'window') return 'vh';
        return 'parent';
    }

    _setupFillBehavior() {
        const mode = this._getFillMode();
        if (mode === 'none') return; // do nothing

        // Observe parent size and window resize to keep fill accurate
        const parent = this.parentElement;
        if (typeof ResizeObserver !== 'undefined' && parent) {
            this._fillObs = new ResizeObserver(() => this._applyFillToViewport());
            this._fillObs.observe(parent);
        }
        try { window.addEventListener('resize', this._onWindowResize, { passive: true }); } catch {}
        // Apply on next frame to ensure layout is ready
        requestAnimationFrame(() => this._applyFillToViewport());
    }

    _teardownFillBehavior() {
        if (this._fillObs) {
            try { this._fillObs.disconnect(); } catch {}
            this._fillObs = null;
        }
        try { window.removeEventListener('resize', this._onWindowResize); } catch {}
        if (this._viewport) {
            // Remove explicit height only if we set it
            this._viewport.style.height = '100%';
        }
    }

    _applyFillToViewport() {
        if (!this._viewport) return;
        const mode = this._getFillMode();
        if (mode === 'none') return;

        let targetHeight = 0;
        if (mode === 'vh') {
            targetHeight = window.innerHeight || 0;
        } else {
            const parent = this.parentElement;
            if (parent) {
                const rect = parent.getBoundingClientRect();
                targetHeight = rect.height;
            }
            // Fallback when parent has no height
            if (!targetHeight || targetHeight <= 0) {
                targetHeight = window.innerHeight || 0;
            }
        }
        if (targetHeight && targetHeight > 0) {
            this._viewport.style.height = `${Math.round(targetHeight)}px`;
            // Ensure host wraps the viewport
            if (!this.style.height) this.style.height = 'auto';
        }
    }

    _setupResizeObserver() {
        if (typeof ResizeObserver !== 'undefined') {
            this._resizeObs = new ResizeObserver(() => {
                // No action needed; we read scroll metrics each frame
            });
            this._resizeObs.observe(this._viewport);
            this._resizeObs.observe(this._content);
        }
    }

    _getSpeed() {
        const raw = this.getAttribute('speed');
        const v = raw ? parseFloat(raw) : 50;
        return isNaN(v) ? 50 : v; // pixels per second
    }

    _getBounceDelay() {
        const raw = this.getAttribute('bounce-delay') || '0s';
        const m = String(raw).trim().match(/^([\d.]+)\s*(ms|s)?$/i);
        if (!m) return 0;
        const num = parseFloat(m[1]);
        const unit = (m[2] || 's').toLowerCase();
        return unit === 'ms' ? num : num * 1000;
    }

    _getAutostart() {
        const attr = this.getAttribute('autostart');
        if (attr === null) return true; // default preserves previous behavior
        const v = String(attr).trim().toLowerCase();
        if (v === '' || v === 'true' || v === '1' || v === 'yes' || v === 'on') return true;
        if (v === 'false' || v === '0' || v === 'no' || v === 'off') return false;
        return true; // treat unknown values as true
    }

    start() { this._startLoop(); }
    stop() { this._stopLoop(); }

    _startLoop() {
        if (this._running) return;
        this._running = true;
        this._lastTs = performance.now();
        this._animId = requestAnimationFrame((t) => this._tick(t));
        this._log('start');
    }

    _stopLoop() {
        if (!this._running) return;
        if (this._animId !== null) {
            cancelAnimationFrame(this._animId);
            this._animId = null;
        }
        this._running = false;
        this._log('stop');
    }

    _tick(ts) {
        if (!this._running) return;
        const dt = Math.max(0, ts - this._lastTs) / 1000; // seconds
        this._lastTs = ts;

        const viewport = this._viewport;
        if (viewport) {
            const maxScroll = Math.max(0, viewport.scrollHeight - viewport.clientHeight);
            const speed = this._getSpeed();

            // Handle bounce pause
            if (performance.now() < this._pausedUntil) {
                // keep position, do nothing
            } else if (maxScroll > 0 && speed > 0) {
                let next = viewport.scrollTop + this._dir * speed * dt;
                if (next <= 0) {
                    next = 0;
                    // Hit top, reverse after delay
                    this._scheduleBounce(1);
                } else if (next >= maxScroll) {
                    next = maxScroll;
                    // Hit bottom, reverse after delay
                    this._scheduleBounce(-1);
                }
                viewport.scrollTop = next;
            }
        }

        this._animId = requestAnimationFrame((t) => this._tick(t));
    }

    _scheduleBounce(nextDir) {
        const delay = this._getBounceDelay();
        this._pausedUntil = performance.now() + delay;
        if (this._dir !== nextDir) {
            this._log('bounce', { from: this._dir, to: nextDir, delayMs: delay });
        }
        this._dir = nextDir;
    }

    _log(msg, data) {
        if (!this._debug) return;
        try { console.info('[vertical-bounce-back]', msg, data ?? ''); } catch {}
    }
}

customElements.define('vertical-bounce-back', VerticalBounceBack);
