class RandomSimpleJoke extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this._jokes = [];
    this._current = -1;
    this._timers = [];
    this._isConnected = false;

    const style = document.createElement('style');
    style.textContent = `
      :host { display: block; width: 100%; height: 100%; }
      .wrap {
        --transition-time: 1000ms;
        height: 100%;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        text-align: center;
        padding: 1rem;
        box-sizing: border-box;
        gap: 0.6em;
        opacity: 0;
        transition: opacity var(--transition-time) ease-in-out;
      }
      .wrap.show { opacity: 1; }
      .setup {
        font-family: 'Comic Neue', 'Oswald', system-ui, sans-serif;
        font-weight: 700;
        font-size: clamp(1.25rem, 4vw, 2.5rem);
        line-height: 1.15;
      }
      .punchline {
        font-family: 'Comic Neue', 'Oswald', system-ui, sans-serif;
        font-weight: 400;
        font-size: clamp(1.1rem, 3.2vw, 2rem);
        line-height: 1.2;
        color: var(--muted, #bfc9d6);
        opacity: 0;
        transition: opacity var(--transition-time) ease-in-out;
      }
      .punchline.show { opacity: 1; }
    `;

    this._container = document.createElement('div');
    this._container.className = 'wrap';

    this._setupEl = document.createElement('div');
    this._setupEl.className = 'setup';

    this._punchEl = document.createElement('div');
    this._punchEl.className = 'punchline';

    this._container.append(this._setupEl, this._punchEl);
    this.shadowRoot.append(style, this._container);
  }

  static get observedAttributes() {
    return ['src', 'interval', 'punchline-reveal', 'transition-time'];
  }

  attributeChangedCallback(name, _old, _new) {
    if (!this._isConnected) return;
    if (name === 'transition-time') {
      const t = this._getMs('transition-time', 1000);
      this._container.style.setProperty('--transition-time', `${t}ms`);
    }
  }

  connectedCallback() {
    this._isConnected = true;
    const t = this._getMs('transition-time', 1000);
    this._container.style.setProperty('--transition-time', `${t}ms`);
    this._init();
  }

  disconnectedCallback() {
    this._isConnected = false;
    this._clearTimers();
  }

  async _init() {
    const src = this.getAttribute('src');
    if (!src) return;
    try {
      const res = await fetch(src, { cache: 'no-store' });
      if (!res.ok) throw new Error(`Fetch failed: ${res.status}`);
      const data = await res.json();
      this._jokes = this._normalize(data);
    } catch (e) {
      this._jokes = [{ setup: 'Failed to load jokes.', punchline: '' }];
      // eslint-disable-next-line no-console
      console.warn('[random-simple-joke]', e);
    }
    if (this._jokes.length === 0) {
      this._jokes = [{ setup: 'No jokes available.', punchline: '' }];
    }
    this._startCycle(true);
  }

  _normalize(data) {
    const out = [];
    const push = (setup, punchline) => {
      if (typeof setup !== 'string') return;
      out.push({ setup: setup.trim(), punchline: (punchline || '').toString().trim() });
    };

    if (Array.isArray(data)) {
      for (const item of data) {
        if (typeof item === 'string') {
          push(item, '');
        } else if (item && typeof item === 'object') {
          const s = item.setup ?? item.question ?? item.prompt ?? item.text ?? item.joke ?? '';
          const p = item.punchline ?? item.answer ?? item.response ?? '';
          if (s && p) push(s, p);
          else if (s) {
            // Try to split combined strings "setup — punchline" or "setup: punchline"
            const m = s.split(/[-–—:]\s+/, 2);
            if (m.length === 2) push(m[0], m[1]);
            else push(s, '');
          }
        }
      }
    } else if (data && typeof data === 'object') {
      // If it's a map with array inside (e.g., { jokes: [...] })
      const arr = Array.isArray(data.jokes) ? data.jokes : [];
      return this._normalize(arr);
    }
    return out;
  }

  _getMs(attr, def) {
    const v = parseInt(this.getAttribute(attr) || '', 10);
    return Number.isFinite(v) && v >= 0 ? v : def;
  }

  _clearTimers() {
    for (const t of this._timers) clearTimeout(t);
    this._timers.length = 0;
  }

  _pickNextIndex() {
    if (this._jokes.length <= 1) return 0;
    let idx;
    do { idx = Math.floor(Math.random() * this._jokes.length); } while (idx === this._current);
    return idx;
  }

  _setJoke(j) {
    this._setupEl.textContent = j.setup || '';
    this._punchEl.textContent = j.punchline || '';
    this._punchEl.classList.remove('show'); // hidden until reveal
  }

  _fadeInContainer() {
    // Force reflow before adding show to ensure transition
    this._container.classList.remove('show');
    // eslint-disable-next-line no-unused-expressions
    this._container.offsetHeight;
    this._container.classList.add('show');
  }

  _fadeOutContainer() {
    this._container.classList.remove('show');
  }

  _startCycle(first = false) {
    if (!this._isConnected) return;
    this._clearTimers();

    const interval = this._getMs('interval', 7000);
    const reveal = this._getMs('punchline-reveal', Math.min(3000, interval / 2));
    const ttime = this._getMs('transition-time', 1000);

    // Pick and render the next joke only after the previous content has fully faded out.
    // First cycle starts with the container hidden, subsequent cycles call this after fade-out completes.
    const idx = this._pickNextIndex();
    this._current = idx;
    this._setJoke(this._jokes[idx]);

    // Fade-in the pair (setup + punchline are children of the same fading wrapper)
    // Force reflow before adding the show class to ensure the transition occurs.
    this._container.classList.remove('show'); // ensure hidden (noop if already hidden)
    // eslint-disable-next-line no-unused-expressions
    this._container.offsetHeight;
    this._container.classList.add('show');

    // Reveal punchline after delay (independent of container fade)
    this._timers.push(setTimeout(() => {
      this._punchEl.classList.add('show');
    }, reveal));

    // Schedule fade-out of the entire pair, then show the next joke after the transition completes.
    const outAt = Math.max(0, interval - ttime);
    this._timers.push(setTimeout(() => {
      this._fadeOutContainer(); // fades setup + punchline together
      this._timers.push(setTimeout(() => this._startCycle(false), ttime));
    }, outAt));
  }
}

customElements.define('random-simple-joke', RandomSimpleJoke);
