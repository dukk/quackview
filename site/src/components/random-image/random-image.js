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
        this.imgElement = null;
    }

    static get observedAttributes() {
        return ['src', 'interval'];
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
                img {
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                    border-radius: 8px;
                }
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

        if (!this.imgElement) {
            this.imgElement = document.createElement('img');
            this.shadowRoot.appendChild(this.imgElement);
            // Remove loading message
            const loading = this.shadowRoot.querySelector('.loading');
            if (loading) loading.remove();
        }

        this.imgElement.src = imageUrl;
        this.imgElement.alt = `Random image ${this.currentIndex + 1}`;
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
}

// Register the custom element
customElements.define('random-image', RandomImageComponent);