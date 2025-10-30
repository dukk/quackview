class RandomImage extends HTMLElement {
	constructor() {
		super();
		this._lists = []; // [{el, src, images:[{url}], loaded:bool, error?:string}]
		this._currentUrl = null;
		this._timer = null;
		this._io = null;
		this._imgA = null;
		this._imgB = null;
		this._showingA = true;
		this._isTransitioning = false;
		this._mounted = false;
		this._debug = false;
		this._defaultTemplate = null;
	}

	connectedCallback() {
		if (this._mounted) return;
		this._mounted = true;
		this._debug = this.hasAttribute('debug');
		this.style.display = this.style.display || 'block';
		this.style.position = this.style.position || 'relative';
		this.style.overflow = this.style.overflow || 'hidden';

		this._log('connected');

		// Discover default template
		this._defaultTemplate = this._discoverDefaultTemplate();

		// Ensure the host has size; since children are absolutely positioned, a zero-height host would hide images
		const desiredHeightAttr = this.getAttribute('height') || this.getAttribute('min-height');
		if (desiredHeightAttr) {
			this.style.height = desiredHeightAttr;
			this._log('applied explicit height from attribute', desiredHeightAttr);
		} else {
			const rect = this.getBoundingClientRect();
			this._log('initial host size', { width: rect.width, height: rect.height });
			if (!rect || rect.height < 1) {
				// Fallback: give it a sensible default size
				this.style.minHeight = '300px';
				// Provide an aspect ratio hint so it grows with width if allowed by layout
				this.style.aspectRatio = this.style.aspectRatio || '16 / 9';
				this._log('applied fallback size', { minHeight: this.style.minHeight, aspectRatio: this.style.aspectRatio });
			}
		}

		// Create two stacked img elements for cross-fade using template
		this._imgA = this._createImgFromTemplate(this._defaultTemplate);
		this._imgB = this._createImgFromTemplate(this._defaultTemplate);
		[this._imgA, this._imgB].forEach((img) => {
			// Override critical positioning and transition styles
			img.style.position = 'absolute';
			img.style.top = '0';
			img.style.left = '0';
			img.style.width = '100%';
			img.style.height = '100%';
			img.style.opacity = '0';
			img.style.transition = 'none';
			img.decoding = 'async';
			img.loading = 'eager';
			if (!img.alt) img.alt = '';
			this.appendChild(img);
		});

		// Load lists then start
		this._collectLists();
		this._loadAllLists().then(() => {
			this._log('lists loaded', {
				count: this._lists.length,
				loaded: this._lists.filter(l => l.loaded && !l.error).length,
				errors: this._lists.filter(l => !!l.error).map(l => ({ src: l.src, error: l.error }))
			});
			this._showInitial();
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

	// Discover default template from <random-image-template> direct children
	_discoverDefaultTemplate() {
		const templateEls = Array.from(this.querySelectorAll(':scope > random-image-template'));
		if (templateEls.length === 0) return null;
		const firstTemplate = templateEls[0];
		const img = firstTemplate.querySelector('img');
		if (!img) return null;
		this._log('discovered default template', img);
		return img;
	}

	// Discover per-list template from <random-image-template> within <random-image-list>
	_discoverListTemplate(listEl) {
		const templateEl = listEl.querySelector(':scope > random-image-template');
		if (!templateEl) return null;
		const img = templateEl.querySelector('img');
		if (!img) return null;
		this._log('discovered list template for', listEl.getAttribute('src'), img);
		return img;
	}

	// Create img element from template (or fallback to default creation)
	_createImgFromTemplate(template) {
		if (!template) {
			// No template: create default img
			const img = document.createElement('img');
			img.style.objectFit = 'contain';
			return img;
		}
		// Clone template img and merge attributes
		const img = template.cloneNode(true);
		return img;
	}

	// Apply template attributes to an existing img element
	_applyTemplateAttributes(img, template) {
		if (!template) return;
		
		// Copy className
		img.className = template.className;
		
		// Copy id if present
		if (template.id) {
			img.id = template.id;
		} else {
			img.removeAttribute('id');
		}
		
		// Copy inline styles that aren't critical for positioning/animation
		// We'll preserve critical properties by re-applying them after
		const criticalStyles = {
			position: img.style.position,
			top: img.style.top,
			left: img.style.left,
			width: img.style.width,
			height: img.style.height,
			opacity: img.style.opacity,
			transition: img.style.transition
		};
		
		// Copy template's inline styles
		img.style.cssText = template.style.cssText;
		
		// Re-apply critical styles
		Object.assign(img.style, criticalStyles);
		
		// Copy other attributes (except src which we set separately)
		for (const attr of template.attributes) {
			if (attr.name !== 'src' && attr.name !== 'style' && attr.name !== 'class' && attr.name !== 'id') {
				img.setAttribute(attr.name, attr.value);
			}
		}
		
		this._log('applied template attributes', { className: img.className, id: img.id });
	}

	// Gather child <random-image-list> definitions
	_collectLists() {
		const children = Array.from(this.querySelectorAll(':scope > random-image-list'));
		this._lists = children.map((el, idx) => ({
			el,
			index: idx,
			src: el.getAttribute('src'),
			srcBaseUrl: el.getAttribute('src-base-url') || '',
			condition: el.getAttribute('condition') || '',
			template: this._discoverListTemplate(el),
			images: [],
			loaded: false,
			error: null,
			lastShownMs: 0
		}));
		this._log('collected lists', this._lists.map(l => ({ index: l.index, src: l.src, hasCondition: !!l.condition, hasTemplate: !!l.template })));
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
			list.images = this._normalizeImages(data);
			// Determine base URL: prefer explicit srcBaseUrl, else derive from list.src
			const base = list.srcBaseUrl && list.srcBaseUrl.trim().length > 0
				? list.srcBaseUrl
				: this._deriveBaseFromSrc(list.src);
			if (base) {
				list.images = list.images.map(item => ({ ...item, url: this._resolveUrl(base, item.url) }));
			}
			list.loaded = true;
			this._log('loaded list', { src: list.src, base, count: list.images.length });
		} catch (err) {
			list.error = String(err);
			list.loaded = true;
			// Keep empty list on failure
			// eslint-disable-next-line no-console
			console.warn('random-image: failed to load list', list.src, err);
		}
	}

	_deriveBaseFromSrc(src) {
		try {
			if (!src) return '';
			// Build absolute URL using current location origin, then strip filename
			const u = new URL(src, window.location.origin);
			const path = u.pathname;
			const idx = path.lastIndexOf('/');
			const dir = idx >= 0 ? path.slice(0, idx + 1) : '/';
			return u.origin + dir;
		} catch {
			return '';
		}
	}

	_normalizeImages(data) {
		// Accept: ["/path/a.jpg", ...] or { images: ["/path/a.jpg", ...] } or objects {url: ...}
		let arr = [];
		if (Array.isArray(data)) arr = data;
		else if (data && Array.isArray(data.images)) arr = data.images;
		else return [];
		return arr
			.map(item => typeof item === 'string' ? { url: item } : (item && item.url ? { url: item.url, ...item } : null))
			.filter(Boolean);
	}

	_resolveUrl(base, url) {
		if (!url) return url;
		try {
			// If url is absolute, the URL constructor will preserve it
			if (/^https?:\/\//i.test(url) || url.startsWith('/')) {
				return url;
			}
			if (!base) return url;
			let baseAbs = base;
			if (!/^https?:\/\//i.test(base)) {
				// If base is a root-relative path, anchor it to current origin
				if (base.startsWith('/')) {
					baseAbs = window.location.origin + base;
				} else {
					baseAbs = new URL(base, window.location.origin).toString();
				}
			}
			const resolved = new URL(url, baseAbs);
			// Prefer returning a root-absolute path when base was root-relative
			return base.startsWith('/') ? resolved.pathname : resolved.toString();
		} catch (err) {
			return url;
		}
	}

	_setupIntersectionObserver() {
		if (!this.hasAttribute('pause-off-screen')) return;
		this._io = new IntersectionObserver(entries => {
			for (const entry of entries) {
				if (entry.target !== this) continue;
				if (entry.isIntersecting) {
					this._log('observer: intersecting, resume autoplay');
					// If refresh-on-screen-enter is set, pick a new image before resuming
					if (this.hasAttribute('refresh-on-screen-enter')) {
						this._log('refresh-on-screen-enter: picking new image');
						this.next().then(() => {
							if (this.hasAttribute('autoplay')) this._startAutoplay();
						});
					} else {
						if (this.hasAttribute('autoplay')) this._startAutoplay();
					}
				} else {
					this._log('observer: not intersecting, pause autoplay');
					this._stopAutoplay();
				}
			}
		}, { threshold: 0.01 });
		this._io.observe(this);
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
		const ms = this._parseDuration(this.getAttribute('display-duration'), 5000);
		this._log('display-duration', ms);
		return ms;
	}

	_buildConditionContext(index, count) {
		const now = new Date();
		const nowMs = Date.now();
		const day = now.getDay();
		const url = new URL(window.location.href);
        const list = this._lists[index];
        const lastShownAtMs = list?.lastShownMs || 0;
        const neverShown = !lastShownAtMs;
        const lastShownAgoMs = neverShown ? Number.POSITIVE_INFINITY : (nowMs - lastShownAtMs);
        const lastShownAgoSeconds = lastShownAgoMs / 1000;
        const lastShownAgoMinutes = lastShownAgoMs / 60000;
        const lastShownAgoHours = lastShownAgoMs / 3600000;
		return {
			now,
			nowMs,
			hour: now.getHours(),
			minute: now.getMinutes(),
			second: now.getSeconds(),
			day, // 0..6
			date: now.getDate(),
			month: now.getMonth() + 1,
			isWeekend: day === 0 || day === 6,
			isWeekday: !(day === 0 || day === 6),
			index,
			count,
			param: (name) => url.searchParams.get(name),
			matchesMedia: (q) => window.matchMedia(q).matches,
			random: Math.random(),
			lastShownAtMs,
			lastShownAgoMs,
			lastShownAgoSeconds,
			lastShownAgoMinutes,
			lastShownAgoHours,
			neverShown
		};
	}

	_evaluateCondition(expr, index, count) {
		if (!expr) return true;
		try {
			const ctx = this._buildConditionContext(index, count);
			const fn = new Function('ctx',
				'const { now, nowMs, hour, minute, second, day, date, month, isWeekend, isWeekday, index, count, param, matchesMedia, random, lastShownAtMs, lastShownAgoMs, lastShownAgoSeconds, lastShownAgoMinutes, lastShownAgoHours, neverShown } = ctx;\n' +
				'return !!(' + expr + ');'
			);
			const result = !!fn(ctx);
			this._log('condition', { index, expr, result, ctx });
			return result;
		} catch (err) {
			// eslint-disable-next-line no-console
			console.warn('random-image condition error:', err);
			return false;
		}
	}

	_eligibleLists() {
		const count = this._lists.length;
		return this._lists.filter((list, i) => this._evaluateCondition(list.condition, i, count));
	}

	_combinedImages() {
		const lists = this._eligibleLists().filter(l => l.loaded && !l.error);
		const urls = new Set();
		const out = [];
		for (const l of lists) {
			for (const item of l.images) {
				const url = item.url;
				if (!url) continue;
				if (!urls.has(url)) {
					urls.add(url);
					// Use list-specific template if available, otherwise use default template
					const template = l.template || this._defaultTemplate;
					out.push({ url, lists: new Set([l.index]), template });
				} else {
					// add list index to existing mapping
					const existing = out.find(o => o.url === url);
					if (existing) {
						existing.lists.add(l.index);
						// If this list has a template and we don't have one yet, use it
						// Prefer list-specific templates over default
						if (l.template && !existing.template) {
							existing.template = l.template;
						}
					}
				}
			}
		}
		this._log('combined images', { count: out.length });
		return out;
	}

	async _showInitial() {
		const imgs = this._combinedImages();
		if (imgs.length === 0) {
			this._log('no images to show initially');
			return; // Nothing to show yet
		}
		const pick = await this._pickNextLoadable(imgs, null);
		if (!pick) {
			this._log('no loadable images initially (all failed to preload)');
			return;
		}
		await this._setImage(pick.url, true, pick.lists, true, pick.template);
	}

	_startAutoplay() {
		this._stopAutoplay();
		const delay = this._getDisplayDuration();
		this._timer = setTimeout(() => this.next(), delay);
	}

	_stopAutoplay() {
		if (this._timer) {
			clearTimeout(this._timer);
			this._timer = null;
		}
	}

	async next() {
		if (this._isTransitioning) return;
		const imgs = this._combinedImages();
		if (imgs.length === 0) {
			this._log('no eligible images on next()');
			if (this.hasAttribute('autoplay')) this._startAutoplay();
			return;
		}
		const pick = await this._pickNextLoadable(imgs, this._currentUrl);
		if (!pick) {
			this._log('no loadable images on next() (all failed to preload)');
			if (this.hasAttribute('autoplay')) this._startAutoplay();
			return;
		}
		await this._setImage(pick.url, false, pick.lists, true, pick.template);
		if (this.hasAttribute('autoplay')) this._startAutoplay();
	}

	async _pickNextLoadable(list, current) {
		const n = list.length;
		if (n === 0) return null;
		if (n === 1) {
			const ok = await this._preload(list[0].url);
			this._log("preload", list[0].url, ok);
			return ok ? list[0] : null;
		}
		
		// Build list of candidates excluding current to avoid immediate repeats
		const candidates = current 
			? list.filter(o => o.url !== current)
			: list;
		
		// If we filtered out the only image, use full list
		const pool = candidates.length > 0 ? candidates : list;
		
		// Shuffle the pool and try each until one loads successfully
		const shuffled = [...pool].sort(() => Math.random() - 0.5);
		
		for (const cand of shuffled) {
			const ok = await this._preload(cand.url);
			this._log("preload", cand.url, ok);
			if (ok) return cand;
		}
		
		return null;
	}

	async _setImage(url, immediate, listsForUrl = new Set(), preloaded = false, template = null) {
		if (!url || (url === this._currentUrl && !immediate)) return;
		this._isTransitioning = true;
		const fadeIn = this.hasAttribute('fade-in');
		const fadeOut = this.hasAttribute('fade-out');
		const duration = this._parseDuration(this.getAttribute('slide-duration') || this.getAttribute('fade-duration') || '1s', 1000);

		this._log('show image', { url, immediate, duration, hasTemplate: !!template });

		const incoming = this._showingA ? this._imgB : this._imgA;
		const outgoing = this._showingA ? this._imgA : this._imgB;

		// Apply template attributes to incoming image if template is provided
		if (template) {
			this._applyTemplateAttributes(incoming, template);
		}

		incoming.style.transition = 'none';
		outgoing.style.transition = 'none';
		incoming.style.opacity = '0';

		if (!preloaded) {
			const ok = await this._preload(url);
			if (!ok) {
				this._isTransitioning = false;
				this._log('preload failed inside _setImage, skipping', url);
				return;
			}
		}
		incoming.src = url;

		// Force reflow
		void incoming.offsetHeight;

		const transIn = fadeIn ? `opacity ${duration}ms ease-in-out` : 'none';
		const transOut = fadeOut ? `opacity ${duration}ms ease-in-out` : 'none';
		if (transIn !== 'none') incoming.style.transition = transIn;
		if (transOut !== 'none') outgoing.style.transition = transOut;

		// Start transitions
		requestAnimationFrame(() => {
			if (fadeOut) outgoing.style.opacity = '0';
			if (fadeIn) incoming.style.opacity = '1'; else incoming.style.opacity = '1';
		});

		await new Promise(resolve => setTimeout(resolve, duration));

		// Finalize
		outgoing.style.transition = 'none';
		outgoing.style.opacity = '0';
		incoming.style.transition = 'none';
		incoming.style.opacity = '1';
		this._showingA = !this._showingA;
		this._currentUrl = url;
		this._isTransitioning = false;

		// Update lastShown for all lists that contributed this image
		const now = Date.now();
		for (const idx of listsForUrl) {
			if (this._lists[idx]) this._lists[idx].lastShownMs = now;
		}
		this._log('updated lastShown for lists', Array.from(listsForUrl));
	}

	_preload(url) {
		return new Promise((resolve) => {
			try {
				const img = new Image();
				img.onload = () => resolve(true);
				img.onerror = () => resolve(false);
				img.src = url;
			} catch (e) {
				resolve(false);
			}
		});
	}

	_log(msg, data) {
		// Basic logging helper
		try {
			console.info('[random-image]', msg, data ?? '');
		} catch {}
	}
}

customElements.define('random-image', RandomImage);
