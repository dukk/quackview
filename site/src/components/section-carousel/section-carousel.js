class SectionCarousel extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });

        this.currentIndex = 0;
        this.slots = [];
        this.timer = null;
        this.isAnimating = false;

        const style = document.createElement('style');
        style.textContent = `
            :host{ display:block; position:relative; overflow:hidden; }
            .viewport { display:flex; width:100%; height:100%; transition: transform 0.6s cubic-bezier(.2,.9,.4,1); }
            ::slotted([slot="section"]),
            ::slotted([slot="carousel-clone"]) {
                flex:0 0 100%;
                height:100%;
                box-sizing:border-box;
                opacity: 1;
                transition: opacity 0.6s ease-in-out;
            }
        `;

        this.viewport = document.createElement('div');
        this.viewport.className = 'viewport';

        this.slotElement = document.createElement('slot');
        this.slotElement.name = 'section';

        this.cloneSlot = document.createElement('slot');
        this.cloneSlot.name = 'carousel-clone';

        this.shadowRoot.append(style, this.viewport);
        this.viewport.append(this.slotElement, this.cloneSlot);

        this.slotElement.addEventListener('slotchange', () => this.handleSlotChange());
        this.cloneSlot.addEventListener('slotchange', () => this.updateHeight());

        this.cloneLightNode = null;
    }

    static get observedAttributes() {
        return ['slide-duration'];
    }

    connectedCallback() {
        // Default per-section interval (ms) when not specified on the child: 60_000ms (1 minute)
        this.defaultInterval = (parseInt(this.getAttribute('default-interval')) || 60000);
        this.slideDuration = Math.min(Math.max(parseInt(this.getAttribute('slide-duration')) || 3000, 100), 3000); // 100ms..3000ms

        // Update the opacity transition to match slide duration
        const style = this.shadowRoot.querySelector('style');
        style.textContent = style.textContent.replace('opacity 0.6s', `opacity ${this.slideDuration}ms`);

        this.refreshSlots();

        // Resize observer to keep height consistent
        this.resizeObserver = new ResizeObserver(() => this.updateHeight());
        this.resizeObserver.observe(this);

        this.updateHeight();

        // Pause on hover/focus
        this.addEventListener('mouseenter', () => this.pause());
        this.addEventListener('mouseleave', () => this.resume());
        this.addEventListener('focusin', () => this.pause());
        this.addEventListener('focusout', () => this.resume());

        this.start();
    }

    disconnectedCallback() {
        this.stop();
        this.resizeObserver?.disconnect();
        if (this._processHash) window.removeEventListener('hashchange', this._processHash);
        if (this.cloneLightNode) {
            this.cloneLightNode.remove();
            this.cloneLightNode = null;
        }
    }

    updateHeight() {
        // Match host height to tallest section to avoid jumpiness
        let max = 0;
        this.slots.forEach(node => {
            const rect = node.getBoundingClientRect();
            if (rect.height > max) max = rect.height;
        });
        if (this.cloneLightNode) {
            const rect = this.cloneLightNode.getBoundingClientRect();
            if (rect.height > max) max = rect.height;
        }
        if (max === 0) max = this.clientHeight || 300;
        this.style.height = max + 'px';
    }

    start() {
        if (this.timer) return;
        if (!this.slots.length) return;
        this.scheduleNext();
    }

    stop() {
        clearTimeout(this.timer);
        this.timer = null;
    }

    pause() {
        this.stop();
    }

    resume() {
        if (!this.timer) this.scheduleNext();
    }

    scheduleNext() {
        if (!this.slots.length) return;
        const currentNode = this.slots[this.currentIndex] || this.slots[0];
        const interval = currentNode?._carouselInterval || this.defaultInterval;

        this.timer = setTimeout(() => this.advance(), interval);
    }

    async advance() {
        if (this.isAnimating || this.slots.length === 0) return;
        this.isAnimating = true;

        const currentSection = this.slots[this.currentIndex];
        const nextIndex = (this.currentIndex + 1) % this.slots.length;
        const nextSection = this.slots[nextIndex];

        // Start fade out current section
        if (currentSection) {
            currentSection.style.opacity = '0';
        }

        // animate by updating transform
        this.viewport.style.transition = `transform ${this.slideDuration}ms cubic-bezier(.3,0,.7,1)`;
        this.viewport.style.transform = `translateX(-${(this.currentIndex + 1) * 100}%)`;

        // Wait for transition to finish
        await new Promise(res => setTimeout(res, this.slideDuration + 20));

        this.currentIndex++;

        // Reset opacity for all sections
        this.slots.forEach(section => {
            section.style.opacity = '1';
        });

        // If we've reached the clone (wrap), reset to 0 without animation
        if (this.currentIndex >= this.slots.length) {
            this.viewport.style.transition = 'none';
            this.viewport.style.transform = 'translateX(0)';
            this.currentIndex = 0;
            // Force reflow
            this.viewport.getBoundingClientRect();
        }

        this.isAnimating = false;
        this.scheduleNext();
    }

    // Jump to a specific section index (0-based). If pause=true, pause rotation.
    goTo(index, pause = false) {
        if (!Number.isFinite(index) || index < 0 || index >= this.slots.length) return;
        this.stop();
        this.currentIndex = index;
        // translate to the requested index
        this.viewport.style.transition = 'none';
        this.viewport.style.transform = `translateX(-${index * 100}%)`;
        // force reflow
        this.viewport.getBoundingClientRect();
        if (pause) {
            this.pausedByHash = true;
            return;
        }
        // resume normally
        this.scheduleNext();
    }

    handleSlotChange() {
        this.refreshSlots();
        this.updateHeight();
    }

    refreshSlots() {
        const assigned = this.slotElement.assignedElements({ flatten: true });
        this.slots = assigned;

        this.slots.forEach(node => {
            const ms = parseInt(node.getAttribute && node.getAttribute('data-interval')) || this.defaultInterval;
            node._carouselInterval = ms;
        });

        this.slotIds = this.slots.map((node, i) => {
            const id = (node.id && node.id.trim()) || (node.getAttribute && node.getAttribute('data-id')) || `section-${i+1}`;
            return id;
        });

        this.dataset.slotIds = this.slotIds.join(',');

        this.ensureClone();

        this.viewport.style.transition = `transform ${this.slideDuration}ms cubic-bezier(.3,0,.7,1)`;
        this.currentIndex = Math.min(this.currentIndex, Math.max(0, this.slots.length - 1));
        this.viewport.style.transform = `translateX(-${this.currentIndex * 100}%)`;

        this.stop();
        this.start();
    }

    ensureClone() {
        if (this.cloneLightNode) {
            this.cloneLightNode.remove();
            this.cloneLightNode = null;
        }

        if (this.slots.length > 0) {
            const clone = this.slots[0].cloneNode(true);
            clone.setAttribute('slot', 'carousel-clone');
            clone.setAttribute('data-carousel-clone', 'true');
            clone.setAttribute('aria-hidden', 'true');
            if (clone.hasAttribute('id')) clone.removeAttribute('id');
            clone.querySelectorAll('[id]').forEach(el => el.removeAttribute('id'));
            this.appendChild(clone);
            this.cloneLightNode = clone;
        }
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'slide-duration' && oldValue !== newValue) {
            this.slideDuration = Math.min(Math.max(parseInt(newValue) || 3000, 100), 3000);
            // Update the opacity transition to match new slide duration
            const style = this.shadowRoot.querySelector('style');
            if (style) {
                style.textContent = style.textContent.replace(/opacity \d+ms/, `opacity ${this.slideDuration}ms`);
            }
        }
    }
}

customElements.define('section-carousel', SectionCarousel);
