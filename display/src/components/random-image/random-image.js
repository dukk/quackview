/**
 * Random Image Web Component
 * Displays random images from a JSON array with configurable interval
 */

class RandomImageComponent extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.images = [];
        this.currentIndex = -1;
        this.intervalId = null;
        this._imgA = null;
        this._imgB = null;
        this._front = 0; // 0 => imgA on top, 1 => imgB on top
    }

    static get observedAttributes() {
        return ['src', 'interval', 'transition-duration'];
    }

    connectedCallback() {
        this.render();
        this.loadImages();
    }

    disconnectedCallback() {
        this.stopRotation();
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (oldValue !== newValue) {
            if (name === 'src') {
                this.loadImages();
            } else if (name === 'interval') {
                this.restartRotation();
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
                }
                .frame { display: grid; width:100%; height:100%; overflow:hidden; border-radius:8px }
                img { grid-area: 1 / 1 / 2 / 2; width:100%; height:100%; object-fit: cover; border-radius: 8px; transition: opacity var(--img-fade-duration, 2s) ease-in-out; opacity: 0; }
                img.show { opacity: 1; }
                .loading {
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    width: 100%;
                    height: 100%;
                    background: rgba(255,255,255,0.1);
                    color: var(--text);
                    font-size: 1.2rem;
                }
                .error {
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    width: 100%;
                    height: 100%;
                    background: rgba(255,0,0,0.1);
                    color: #ff6b6b;
                    font-size: 1rem;
                }
            </style>
            <div class="loading">Loading images...</div>
        `;
    }

    async loadImages() {
        const src = this.getAttribute('src');
        if (!src) {
            this.showError('No src attribute provided');
            return;
        }

        try {
            const response = await fetch(src);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            const data = await response.json();
            this.images = data.images || [];
            if (this.images.length === 0) {
                throw new Error('No images found in JSON');
            }
            this.startRotation();
        } catch (error) {
            console.error('Error loading images:', error);
            this.showError(`Error loading images: ${error.message}`);
        }
    }

    startRotation() {
        this.stopRotation();
        this._ensureImageElements();
        this.showRandomImage();
        const interval = parseInt(this.getAttribute('interval')) || 5000; // Default 5 seconds
        this.intervalId = setInterval(() => {
            this.showRandomImage();
        }, interval);
    }

    restartRotation() {
        if (this.images.length > 0) {
            this.startRotation();
        }
    }

    stopRotation() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    }

    showRandomImage() {
        if (this.images.length === 0) return;

        let newIndex;
        do {
            newIndex = Math.floor(Math.random() * this.images.length);
        } while (this.images.length > 1 && newIndex === this.currentIndex);

        this.currentIndex = newIndex;
        const imageUrl = this.images[this.currentIndex].url;

        this._ensureImageElements();

        // determine which image element is currently front and which is next
        const frontImg = this._front === 0 ? this._imgA : this._imgB;
        const backImg = this._front === 0 ? this._imgB : this._imgA;

        // prepare back image (it will fade in)
        backImg.classList.remove('show');
        backImg.alt = `Random image ${this.currentIndex + 1}`;

        // load new image on back img, then crossfade on load
        backImg.onload = () => {
            // force style calculation then start fade
            requestAnimationFrame(() => {
                backImg.classList.add('show');
                frontImg.classList.remove('show');
                // flip front after transition
                this._front = this._front === 0 ? 1 : 0;
            });
        };
        backImg.onerror = () => {
            console.error('random-image: failed to load', imageUrl);
        };
        // start loading
        backImg.src = imageUrl;
    }

    showError(message) {
        this.shadowRoot.innerHTML = `
            <style>
                .error {
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    width: 100%;
                    height: 100%;
                    background: rgba(255,0,0,0.1);
                    color: #ff6b6b;
                    font-size: 1rem;
                    padding: 1rem;
                    text-align: center;
                }
            </style>
            <div class="error">${message}</div>
        `;
    }

    _ensureImageElements() {
        // if already created, nothing to do
        if (this._imgA && this._imgB) return;

        // remove loading message if present
        const loading = this.shadowRoot.querySelector('.loading');
        if (loading) loading.remove();

        const frame = document.createElement('div');
        frame.className = 'frame';
        const imgA = document.createElement('img');
        const imgB = document.createElement('img');
        imgA.decoding = 'async';
        imgB.decoding = 'async';
        // initially show front img
        imgA.classList.add('show');
        imgB.classList.remove('show');

        frame.appendChild(imgA);
        frame.appendChild(imgB);

        // clear shadow and append frame
        this.shadowRoot.appendChild(frame);

        this._imgA = imgA;
        this._imgB = imgB;
        this._front = 0;

        // set transition duration from attribute
        this._applyTransitionDuration();
    }

    _applyTransitionDuration() {
        const attr = this.getAttribute('transition-duration');
        let ms = 2000;
        if (attr) {
            const n = Number(attr);
            if (!Number.isNaN(n) && n > 0) ms = n;
        }
        // write CSS variable on host
        const seconds = (ms / 1000) + 's';
        this.style.setProperty('--img-fade-duration', seconds);
    }
}

// Register the custom element
customElements.define('random-image', RandomImageComponent);