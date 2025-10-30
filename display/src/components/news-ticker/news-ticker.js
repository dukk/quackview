class NewsTicker extends HTMLElement {
	constructor() {
		super();
		this._lists = [];
		this._articles = [];
		this._mounted = false;
		this._io = null;
		this._animationId = null;
		this._scrollPosition = 0;
		this._containerWidth = 0;
		this._contentWidth = 0;
		this._isPaused = false;
		this._debug = false;
		this._itemTemplate = null;
		this._defaultRefreshMs = 0;
		this._refreshTimers = [];
	}

	connectedCallback() {
		if (this._mounted) return;
		this._mounted = true;
		this._debug = this.hasAttribute('debug');

		// Parse default refresh interval on the ticker, if provided
		this._defaultRefreshMs = this._parseInterval(this.getAttribute('refresh')) || 0;

		// Set minimal required styles
		this.style.display = this.style.display || 'block';
		this.style.position = this.style.position || 'relative';
		this.style.overflow = 'hidden';
		this.style.whiteSpace = 'nowrap';

		this._log('connected');

		// Discover item template
		this._discoverItemTemplate();

		// Create scrolling container
		this._createScrollContainer();

		// Load lists then start
		this._collectLists();
		this._loadAllLists().then(() => {
			this._log('lists loaded', {
				count: this._lists.length,
				loaded: this._lists.filter(l => l.loaded && !l.error).length,
				errors: this._lists.filter(l => !!l.error).map(l => ({ src: l.src, error: l.error }))
			});
			this._renderArticles();
			this._scheduleRefreshes();
			this._setupIntersectionObserver();
			if (this.hasAttribute('autoplay')) {
				this._startScrolling();
			}
		});
	}

	disconnectedCallback() {
		this._stopScrolling();
		if (this._io) {
			try { this._io.disconnect(); } catch {}
			this._io = null;
		}
		this._clearRefreshTimers();
	}

	_createScrollContainer() {
		this._container = document.createElement('div');
		this._container.style.display = 'inline-block';
		this._container.style.whiteSpace = 'nowrap';
		this._container.style.willChange = 'transform';
		this.appendChild(this._container);
	}

	_discoverItemTemplate() {
		const templateEl = this.querySelector(':scope > news-list-item-template');
		if (!templateEl) {
			this._log('no item template found, using default');
			return;
		}
		this._itemTemplate = templateEl;
		this._log('discovered item template', templateEl);
	}

	_collectLists() {
		const children = Array.from(this.querySelectorAll(':scope > news-list'));
		this._lists = children.map((el, idx) => ({
			el,
			index: idx,
			src: el.getAttribute('src'),
			condition: el.getAttribute('condition') || '',
			maxArticles: parseInt(el.getAttribute('max-articles')) || 0,
			refreshMs: this._parseInterval(el.getAttribute('refresh')) || this._defaultRefreshMs || 0,
			articles: [],
			loaded: false,
			error: null,
			_signature: '',
			_isRefreshing: false
		}));
		this._log('collected lists', this._lists.map(l => ({ index: l.index, src: l.src, hasCondition: !!l.condition, maxArticles: l.maxArticles, refreshMs: l.refreshMs })));
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
			list.articles = this._normalizeArticles(data);
			
			// Apply max-articles limit if specified
			if (list.maxArticles > 0 && list.articles.length > list.maxArticles) {
				// Sort by date first to keep the newest articles
				list.articles.sort((a, b) => {
					if (!a.date && !b.date) return 0;
					if (!a.date) return 1;
					if (!b.date) return -1;
					const aTime = a.date instanceof Date ? a.date.getTime() : 0;
					const bTime = b.date instanceof Date ? b.date.getTime() : 0;
					return bTime - aTime; // newest first
				});
				// Keep only the first maxArticles items (newest)
				list.articles = list.articles.slice(0, list.maxArticles);
			}
			
			// Compute signature to detect changes
			const sig = this._computeArticlesSignature(list.articles);
			const changed = sig !== list._signature;
			list._signature = sig;
			list.loaded = true;
			this._log('loaded list', { src: list.src, count: list.articles.length, maxArticles: list.maxArticles, changed });
			if (changed) {
				this._onListsUpdated();
			}
		} catch (err) {
			list.error = String(err);
			list.loaded = true;
			// eslint-disable-next-line no-console
			console.warn('news-ticker: failed to load list', list.src, err);
		}
	}

	_parseInterval(value) {
		if (!value) return 0;
		const m = String(value).trim().match(/^([\d.]+)\s*(ms|s|m|h)?$/i);
		if (!m) return 0;
		const num = parseFloat(m[1]);
		const unit = (m[2] || 's').toLowerCase();
		switch (unit) {
			case 'ms': return num;
			case 's': return num * 1000;
			case 'm': return num * 60 * 1000;
			case 'h': return num * 60 * 60 * 1000;
			default: return 0;
		}
	}

	_scheduleRefreshes() {
		this._clearRefreshTimers();
		for (const list of this._lists) {
			if (!list.refreshMs || list.refreshMs <= 0) continue;
			const id = setInterval(() => {
				this._reloadList(list);
			}, list.refreshMs);
			this._refreshTimers.push(id);
		}
		this._log('scheduled refresh timers', this._lists.map(l => ({ src: l.src, refreshMs: l.refreshMs })));
	}

	_clearRefreshTimers() {
		if (this._refreshTimers && this._refreshTimers.length) {
			for (const id of this._refreshTimers) {
				try { clearInterval(id); } catch {}
			}
		}
		this._refreshTimers = [];
	}

	async _reloadList(list) {
		if (list._isRefreshing) return;
		list._isRefreshing = true;
		try {
			await this._loadList(list);
		} finally {
			list._isRefreshing = false;
		}
	}

	_computeArticlesSignature(articles) {
		try {
			return articles.map(a => `${a.title}|${a.url}|${a.source}|${a.summary}|${a.date instanceof Date ? a.date.getTime() : ''}`).join('\n');
		} catch {
			return String(Math.random());
		}
	}

	_onListsUpdated() {
		// Re-render with new combined article set
		this._renderArticles();
		// Ensure scrolling continues if autoplay is set
		if (this.hasAttribute('autoplay') && this._animationId === null) {
			this._startScrolling();
		}
	}

	_normalizeArticles(data) {
		// Accept: [{ title, ... }, ...] or { articles: [{ title, ... }, ...] }
		let arr = [];
		if (Array.isArray(data)) {
			arr = data;
		} else if (data && Array.isArray(data.articles)) {
			arr = data.articles;
		} else if (data && Array.isArray(data.news)) {
			arr = data.news;
		} else {
			return [];
		}
		return arr
			.map(item => {
				if (!item || typeof item !== 'object') return null;
				// Support various property names for the headline/title
				const title = item.title || item.headline || item.text || item.description || '';
				if (!title) return null;
				
				// Parse date from various possible fields
				let date = null;
				const dateStr = item.date || item.publishedAt || item.published || item.pubDate || item.timestamp || '';
				if (dateStr) {
					try {
						date = new Date(dateStr);
						// Validate the date
						if (isNaN(date.getTime())) date = null;
					} catch {
						date = null;
					}
				}
				// Important: spread first, then override normalized fields so item.date (string)
				// doesn't overwrite our parsed Date object
				return {
					...item,
					title,
					url: item.url || item.link || '',
					source: item.source || '',
					summary: item.summary || item.description || item.excerpt || '',
					date
				};
			})
			.filter(Boolean);
	}

	_buildConditionContext(index) {
		const now = new Date();
		const nowMs = Date.now();
		const day = now.getDay();
		const url = new URL(window.location.href);
		return {
			now,
			nowMs,
			hour: now.getHours(),
			minute: now.getMinutes(),
			second: now.getSeconds(),
			day,
			date: now.getDate(),
			month: now.getMonth() + 1,
			isWeekend: day === 0 || day === 6,
			isWeekday: !(day === 0 || day === 6),
			index,
			count: this._lists.length,
			param: (name) => url.searchParams.get(name),
			matchesMedia: (q) => window.matchMedia(q).matches,
			random: Math.random()
		};
	}

	_evaluateCondition(expr, index) {
		if (!expr) return true;
		try {
			const ctx = this._buildConditionContext(index);
			const fn = new Function('ctx',
				'const { now, nowMs, hour, minute, second, day, date, month, isWeekend, isWeekday, index, count, param, matchesMedia, random } = ctx;\n' +
				'return !!(' + expr + ');'
			);
			const result = !!fn(ctx);
			this._log('condition', { index, expr, result });
			return result;
		} catch (err) {
			// eslint-disable-next-line no-console
			console.warn('news-ticker condition error:', err);
			return false;
		}
	}

	_eligibleLists() {
		return this._lists.filter((list, i) => this._evaluateCondition(list.condition, i));
	}

	_combinedArticles() {
		const lists = this._eligibleLists().filter(l => l.loaded && !l.error);
		const out = [];
		for (const l of lists) {
			out.push(...l.articles);
		}
		
		// Sort by date, newest first (articles without dates go to the end)
		out.sort((a, b) => {
			if (!a.date && !b.date) return 0;
			if (!a.date) return 1; // a goes after b
			if (!b.date) return -1; // b goes after a
			// Check if dates are valid Date objects
			const aTime = a.date instanceof Date ? a.date.getTime() : 0;
			const bTime = b.date instanceof Date ? b.date.getTime() : 0;
			return bTime - aTime; // newest first
		});
		
		this._log('combined articles', { count: out.length });
		return out;
	}

	_renderArticles() {
		this._articles = this._combinedArticles();
		this._container.innerHTML = '';

		if (this._articles.length === 0) {
			this._log('no articles to display');
			return;
		}

		// Duplicate articles to create seamless loop
		const articlesToRender = [...this._articles, ...this._articles];

		articlesToRender.forEach((article, idx) => {
			const item = document.createElement('span');
			item.style.display = 'inline-block';
			item.style.paddingRight = '3em'; // Spacing between articles
			
			if (this._itemTemplate) {
				// Use custom template
				const content = this._renderArticleFromTemplate(article);
				item.appendChild(content);
			} else {
				// Use default rendering
				const content = this._renderArticleDefault(article);
				item.appendChild(content);
			}

			this._container.appendChild(item);
		});

		// Measure content width
		this._contentWidth = this._container.scrollWidth;
		this._containerWidth = this.clientWidth;
		
		this._log('rendered articles', {
			count: this._articles.length,
			contentWidth: this._contentWidth,
			containerWidth: this._containerWidth
		});
	}

	_renderArticleFromTemplate(article) {
		// Clone the template content
		const content = document.createElement('span');
		const templateContent = this._itemTemplate.cloneNode(true);
		
		// Find all elements with data-item attributes
		const dataItems = templateContent.querySelectorAll('[data-item]');
		
		dataItems.forEach(el => {
			const itemKey = el.getAttribute('data-item');
			let value = '';
			
			switch (itemKey) {
				case 'title':
					if (article.url) {
						const link = document.createElement('a');
						link.href = article.url;
						link.textContent = article.title || '';
						link.style.textDecoration = 'none';
						link.style.color = 'inherit';
						el.innerHTML = '';
						el.appendChild(link);
					} else {
						el.textContent = article.title || '';
					}
					break;
				case 'source':
					value = article.source ? `[${article.source}]` : '';
					el.textContent = value;
					break;
				case 'summary':
					el.textContent = article.summary || '';
					break;
				case 'date':
					if (article.date instanceof Date) {
						// Format date as a readable string
						value = article.date.toLocaleDateString();
					} else if (article.date) {
						// Attempt to parse string/number dates safely
						try {
							const d = new Date(article.date);
							if (!isNaN(d.getTime())) value = d.toLocaleDateString();
						} catch {}
					}
					el.textContent = value;
					break;
				case 'url':
					el.textContent = article.url || '';
					break;
				default:
					// Support any custom property
					value = article[itemKey] || '';
					el.textContent = value;
					break;
			}
		});
		
		// Copy all children from template to content
		while (templateContent.firstChild) {
			content.appendChild(templateContent.firstChild);
		}
		
		return content;
	}

	_renderArticleDefault(article) {
		// Create the content wrapper
		const content = document.createElement('span');
		
		// Add title
		const titleEl = document.createElement('span');
		titleEl.style.fontWeight = 'bold';
		if (article.url) {
			const link = document.createElement('a');
			link.href = article.url;
			link.textContent = article.title;
			link.style.textDecoration = 'none';
			link.style.color = 'inherit';
			titleEl.appendChild(link);
		} else {
			titleEl.textContent = article.title;
		}
		content.appendChild(titleEl);

		// Add source if available
		if (article.source) {
			const source = document.createElement('span');
			source.textContent = ` [${article.source}]`;
			source.style.opacity = '0.7';
			source.style.fontSize = '0.9em';
			content.appendChild(source);
		}

		// Add summary if available
		if (article.summary) {
			const summary = document.createElement('span');
			summary.textContent = ` - ${article.summary}`;
			summary.style.opacity = '0.9';
			content.appendChild(summary);
		}
		
		return content;
	}

	_setupIntersectionObserver() {
		if (!this.hasAttribute('pause-off-screen')) return;
		this._io = new IntersectionObserver(entries => {
			for (const entry of entries) {
				if (entry.target !== this) continue;
				if (entry.isIntersecting) {
					this._log('observer: intersecting, resume scrolling');
					if (this.hasAttribute('autoplay')) this._startScrolling();
				} else {
					this._log('observer: not intersecting, pause scrolling');
					this._stopScrolling();
				}
			}
		}, { threshold: 0.01 });
		this._io.observe(this);
	}

	_getScrollSpeed() {
		// Speed in pixels per second
		const speed = parseFloat(this.getAttribute('scroll-speed')) || 50;
		return speed;
	}

	_getScrollDirection() {
		return this.getAttribute('scroll-direction') || 'right-to-left';
	}

	_startScrolling() {
		if (this._animationId !== null) return;
		if (this._articles.length === 0) return;

		this._isPaused = false;
		this._lastTime = performance.now();

		const animate = (currentTime) => {
			if (this._isPaused) return;

			const deltaTime = (currentTime - this._lastTime) / 1000; // Convert to seconds
			this._lastTime = currentTime;

			const speed = this._getScrollSpeed();
			const direction = this._getScrollDirection();
			const increment = speed * deltaTime;

			if (direction === 'right-to-left') {
				this._scrollPosition += increment;
			} else {
				this._scrollPosition -= increment;
			}

			// Loop seamlessly: we duplicated the content, so reset at halfway point
			const halfWidth = this._contentWidth / 2;
			if (this._scrollPosition >= halfWidth) {
				this._scrollPosition -= halfWidth;
			} else if (this._scrollPosition < 0) {
				this._scrollPosition += halfWidth;
			}

			// Apply transform
			const translateX = -this._scrollPosition;
			this._container.style.transform = `translateX(${translateX}px)`;

			this._animationId = requestAnimationFrame(animate);
		};

		this._animationId = requestAnimationFrame(animate);
		this._log('started scrolling', { speed: this._getScrollSpeed(), direction: this._getScrollDirection() });
	}

	_stopScrolling() {
		if (this._animationId !== null) {
			cancelAnimationFrame(this._animationId);
			this._animationId = null;
		}
		this._isPaused = true;
		this._log('stopped scrolling');
	}

	_log(msg, data) {
		if (!this._debug) return;
		try {
			console.info('[news-ticker]', msg, data ?? '');
		} catch {}
	}
}

customElements.define('news-ticker', NewsTicker);
