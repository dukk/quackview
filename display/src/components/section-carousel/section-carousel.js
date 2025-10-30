class SectionCarousel extends HTMLElement {
            constructor() {
                super();
                this.currentIndex = 0;
                this.sections = [];
                this.timer = null;
                this.isTransitioning = false;
                this.lastShown = [];
                this._handleKeyDown = this._handleKeyDown.bind(this);
            }

            connectedCallback() {
                // Small delay to ensure children are fully parsed
                requestAnimationFrame(() => {
                    this.initialize();
                });
                // Add keyboard navigation
                document.addEventListener('keydown', this._handleKeyDown);
            }

            initialize() {
                // Collect all <section> children
                this.sections = Array.from(this.querySelectorAll(':scope > section'));

                console.log('Section carousel initialized with', this.sections.length, 'sections');

                if (this.sections.length === 0) return;

                const direction = this.getAttribute('slide-direction') || 'right-to-left';
                const fadeIn = this.hasAttribute('fade-in');

                // Determine eligible slides based on optional condition attribute
                const eligible = this._getEligibleIndices();
                if (eligible.length === 0) {
                    console.warn('SectionCarousel: No eligible slides based on conditions');
                }

                // If current index is not eligible, pick the first eligible as start
                if (eligible.length > 0 && !eligible.includes(this.currentIndex)) {
                    this.currentIndex = eligible[0];
                }

                // Initialize lastShown tracking to match sections
                this.lastShown = new Array(this.sections.length).fill(0);

                // Prepare all slides in absolute layout; current at center, others off-screen
                this.sections.forEach((section, idx) => {
                    section.style.willChange = 'transform, opacity';
                    section.style.transition = 'none';
                    // Set initial positions
                    if (idx === this.currentIndex) {
                        section.style.transform = 'translate(0, 0)';
                        section.style.opacity = '1';
                        this.lastShown[idx] = Date.now();
                    } else {
                        section.style.transform = this._offscreenTransform(direction, +1);
                        // If slide is ineligible, keep it hidden; otherwise follow fade-in preference
                        const isEligible = eligible.length === 0 ? true : eligible.includes(idx);
                        section.style.opacity = isEligible ? (fadeIn ? '0' : '1') : '0';
                    }
                });

                // Start autoplay if enabled
                if (this.hasAttribute('autoplay')) {
                    this.startAutoplay();
                }
            }

            disconnectedCallback() {
                this.stopAutoplay();
                document.removeEventListener('keydown', this._handleKeyDown);
            }

            _handleKeyDown(event) {
                // Only handle arrow keys
                if (event.key === 'ArrowRight') {
                    event.preventDefault();
                    this.stopAutoplay(); // Stop autoplay when manually navigating
                    this.next();
                } else if (event.key === 'ArrowLeft') {
                    event.preventDefault();
                    this.stopAutoplay(); // Stop autoplay when manually navigating
                    this.previous();
                }
            }

            startAutoplay() {
                this.stopAutoplay();
                const delay = this.getCurrentDisplayDuration();
                this.timer = setTimeout(() => {
                    this.next();
                }, delay);
            }

            stopAutoplay() {
                if (this.timer) {
                    clearTimeout(this.timer);
                    this.timer = null;
                }
            }

            getCurrentDisplayDuration() {
                const currentSection = this.sections[this.currentIndex];
                // Check section-specific display-duration
                if (currentSection && currentSection.hasAttribute('display-duration')) {
                    return this.parseDuration(currentSection.getAttribute('display-duration'));
                }
                // Fall back to carousel display-duration
                if (this.hasAttribute('display-duration')) {
                    return this.parseDuration(this.getAttribute('display-duration'));
                }
                // Default to 5 seconds
                return 5000;
            }

            parseDuration(value) {
                if (!value) return 5000;
                const match = value.match(/^(\d+(?:\.\d+)?)(ms|s)?$/);
                if (!match) return 5000;
                const num = parseFloat(match[1]);
                const unit = match[2] || 's';
                return unit === 'ms' ? num : num * 1000;
            }

            async next() {
                if (this.isTransitioning || this.sections.length === 0) return;
                this.isTransitioning = true;
                const eligible = this._getEligibleIndices();

                // If nothing is eligible, just reschedule and bail
                if (eligible.length === 0) {
                    this.isTransitioning = false;
                    if (this.hasAttribute('autoplay')) this.startAutoplay();
                    return;
                }

                // Find the next eligible index after current
                const nextIndex = this._findNextEligibleIndex(this.currentIndex, eligible);
                // If we couldn't find a next index (shouldn't happen with wrap-around), bail
                if (nextIndex === -1) {
                    this.isTransitioning = false;
                    if (this.hasAttribute('autoplay')) this.startAutoplay();
                    return;
                }

                await this._slideTo(nextIndex, false);
            }

            _getEligibleIndices() {
                const list = [];
                for (let i = 0; i < this.sections.length; i++) {
                    if (this._evaluateCondition(this.sections[i], i)) list.push(i);
                }
                return list;
            }

            _findNextEligibleIndex(fromIdx, eligible) {
                if (!eligible || eligible.length === 0) return -1;
                const n = this.sections.length;
                for (let step = 1; step <= n; step++) {
                    const idx = (fromIdx + step) % n;
                    if (eligible.includes(idx)) return idx;
                }
                return -1;
            }

            _findPreviousEligibleIndex(fromIdx, eligible) {
                if (!eligible || eligible.length === 0) return -1;
                const n = this.sections.length;
                for (let step = 1; step <= n; step++) {
                    const idx = (fromIdx - step + n) % n;
                    if (eligible.includes(idx)) return idx;
                }
                return -1;
            }

            _evaluateCondition(section, index) {
                const expr = section.getAttribute('condition');
                if (!expr) return true;
                try {
                    const ctx = this._buildConditionContext(index);
                    // Evaluate expression in a restricted scope of provided context
                    const fn = new Function('ctx',
                        'const { now, nowMs, hour, minute, second, day, date, month, isWeekend, isWeekday, index, count, param, matchesMedia, random, lastShownAtMs, lastShownAgoMs, lastShownAgoMinutes, neverShown } = ctx;\n' +
                        'return !!(' + expr + ');'
                    );
                    return !!fn(ctx);
                } catch (err) {
                    console.warn('SectionCarousel condition error:', err);
                    return false;
                }
            }

            _buildConditionContext(index) {
                const now = new Date();
                const nowMs = Date.now();
                const day = now.getDay();
                const url = new URL(window.location.href);
                const lastShownAtMs = this.lastShown?.[index] ?? 0;
                const neverShown = !lastShownAtMs;
                const lastShownAgoMs = neverShown 
                    ? Number.POSITIVE_INFINITY 
                    : (nowMs - lastShownAtMs);
                const lastShownAgoSeconds = lastShownAgoMs / 1000;
                const lastShownAgoMinutes = lastShownAgoMs / 60000;
                const lastShownAgoHours = lastShownAgoMs / 3600000;
                return {
                    now,
                    nowMs,
                    hour: now.getHours(),
                    minute: now.getMinutes(),
                    second: now.getSeconds(),
                    day, // 0=Sun..6=Sat
                    date: now.getDate(), // 1..31
                    month: now.getMonth() + 1, // 1..12
                    isWeekend: day === 0 || day === 6,
                    isWeekday: !(day === 0 || day === 6),
                    index,
                    count: this.sections.length,
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

            _offscreenTransform(direction, sign) {
                switch (direction) {
                    case 'left-to-right':
                        return `translateX(${sign * -100}%)`;
                    case 'bottom-to-top':
                        return `translateY(${sign * 100}%)`;
                    case 'top-to-bottom':
                        return `translateY(${sign * -100}%)`;
                    case 'right-to-left':
                    default:
                        return `translateX(${sign * 100}%)`;
                }
            }

            _waitForTransitionEnd(el, timeoutMs) {
                return new Promise(resolve => {
                    let done = false;
                    const onEnd = () => {
                        if (done) return;
                        done = true;
                        el.removeEventListener('transitionend', onEnd);
                        resolve();
                    };
                    el.addEventListener('transitionend', onEnd, { once: true });
                    setTimeout(onEnd, timeoutMs);
                });
            }

            previous() {
                if (this.isTransitioning || this.sections.length === 0) return;
                this.isTransitioning = true;
                const currentSection = this.sections[this.currentIndex];
                const eligible = this._getEligibleIndices();

                // If nothing is eligible, just bail
                if (eligible.length === 0) {
                    this.isTransitioning = false;
                    return;
                }

                // Find the previous eligible index before current
                const prevIndex = this._findPreviousEligibleIndex(this.currentIndex, eligible);
                // If only one eligible slide or can't find previous, stay put
                if (prevIndex === this.currentIndex || prevIndex === -1) {
                    this.isTransitioning = false;
                    return;
                }
                
                this._slideTo(prevIndex, true);
            }

            async _slideTo(targetIndex, isReverse = false) {
                const currentSection = this.sections[this.currentIndex];
                const targetSection = this.sections[targetIndex];

                const slideDuration = this.parseDuration(this.getAttribute('slide-duration') || '1s');
                const fadeOut = this.hasAttribute('fade-out');
                const fadeIn = this.hasAttribute('fade-in');
                const direction = this.getAttribute('slide-direction') || 'right-to-left';

                // When going backward (previous), reverse the slide direction
                const incomingSign = isReverse ? -1 : +1;
                const outgoingSign = isReverse ? +1 : -1;

                // Ensure initial positions
                currentSection.style.transition = 'none';
                targetSection.style.transition = 'none';
                currentSection.style.transform = 'translate(0, 0)';
                targetSection.style.transform = this._offscreenTransform(direction, incomingSign);
                targetSection.style.opacity = fadeIn ? '0' : '1';
                currentSection.style.opacity = '1';

                // Manage stacking order
                currentSection.style.zIndex = '1';
                targetSection.style.zIndex = '2';

                // Force reflow
                void currentSection.offsetHeight;
                void targetSection.offsetHeight;

                // Apply transitions
                const base = `transform ${slideDuration}ms ease-in-out`;
                currentSection.style.transition = fadeOut ? `${base}, opacity ${slideDuration}ms ease-in-out` : base;
                targetSection.style.transition = fadeIn ? `${base}, opacity ${slideDuration}ms ease-in-out` : base;

                // Start animations
                await new Promise(resolve => requestAnimationFrame(resolve));

                currentSection.style.transform = this._offscreenTransform(direction, outgoingSign);
                if (fadeOut) currentSection.style.opacity = '0';

                targetSection.style.transform = 'translate(0, 0)';
                if (fadeIn) targetSection.style.opacity = '1';

                // Wait for transitions
                await Promise.race([
                    this._waitForTransitionEnd(currentSection, slideDuration + 50),
                    this._waitForTransitionEnd(targetSection, slideDuration + 50)
                ]);

                // Set end-state styles
                currentSection.style.transition = 'none';
                currentSection.style.transform = this._offscreenTransform(direction, incomingSign);
                if (fadeOut) currentSection.style.opacity = '0';

                targetSection.style.transition = 'none';
                targetSection.style.transform = 'translate(0, 0)';
                targetSection.style.opacity = '1';

                // Reset stacking
                currentSection.style.zIndex = '';
                targetSection.style.zIndex = '';

                // Update shown timestamp and current index
                this.lastShown[targetIndex] = Date.now();
                this.currentIndex = targetIndex;
                this.isTransitioning = false;

                // Continue autoplay if it was active
                if (this.hasAttribute('autoplay')) {
                    this.startAutoplay();
                }
            }
        }

        customElements.define('section-carousel', SectionCarousel);