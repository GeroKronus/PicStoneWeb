/**
 * EDITOR DE IMAGENS - Módulo Principal
 *
 * Gerencia estado, comunicação com Worker e orquestração
 * Singleton pattern para fácil acesso global
 */

class ImageEditor {
    constructor() {
        // Estado do editor
        this.state = {
            originalImage: null,
            currentImage: null,
            fileName: 'edited-image',
            filters: {
                brightness: 0,
                contrast: 0,
                gamma: 0,
                saturation: 0,
                hue: 0,
                red: 0,
                green: 0,
                blue: 0,
                temperature: 0,
                shadows: 0
            },
            isProcessing: false,
            worker: null
        };

        // Elementos do DOM - Lado-a-Lado
        this.canvasOriginal = null;
        this.canvasEdited = null;
        this.ctxOriginal = null;
        this.ctxEdited = null;

        // Elementos do DOM - Slider Comparativo
        this.canvasSliderBefore = null;
        this.canvasSliderAfter = null;
        this.ctxSliderBefore = null;
        this.ctxSliderAfter = null;
        this.comparisonDivider = null;
        this.comparisonContainer = null;
        this.comparisonBeforeWrapper = null;

        // Estado do slider comparativo
        this.sliderState = {
            isDragging: false,
            currentPosition: 50 // Porcentagem (0-100)
        };

        // Flag para indicar se já foi inicializado
        this.initialized = false;
        this.viewMode = 'slider'; // 'side-by-side' ou 'slider' - default: slider
    }

    /**
     * Inicializa o editor (chamado quando tela é mostrada)
     */
    init() {
        if (this.initialized) return;

        // Pega referências dos canvas - Lado-a-Lado
        this.canvasOriginal = document.getElementById('editorCanvasOriginal');
        this.canvasEdited = document.getElementById('editorCanvasEdited');

        if (!this.canvasOriginal || !this.canvasEdited) {
            console.error('Canvas elements not found');
            return;
        }

        this.ctxOriginal = this.canvasOriginal.getContext('2d', { willReadFrequently: true });
        this.ctxEdited = this.canvasEdited.getContext('2d', { willReadFrequently: true });

        // Pega referências dos canvas - Slider Comparativo
        this.canvasSliderBefore = document.getElementById('editorCanvasSliderBefore');
        this.canvasSliderAfter = document.getElementById('editorCanvasSliderAfter');
        this.comparisonDivider = document.querySelector('.comparison-divider');
        this.comparisonContainer = document.querySelector('.comparison-container');
        this.comparisonBeforeWrapper = document.querySelector('.comparison-before-wrapper');

        if (this.canvasSliderBefore && this.canvasSliderAfter) {
            this.ctxSliderBefore = this.canvasSliderBefore.getContext('2d', { willReadFrequently: true });
            this.ctxSliderAfter = this.canvasSliderAfter.getContext('2d', { willReadFrequently: true });

            // Inicializa eventos do slider comparativo
            this.initSliderEvents();
        }

        // Inicializa Worker
        this.initWorker();

        this.initialized = true;
    }

    /**
     * Inicializa Web Worker
     */
    initWorker() {
        try {
            this.state.worker = new Worker('editor-worker.js');

            this.state.worker.onmessage = (e) => {
                const { type, imageData, error } = e.data;

                if (type === 'FILTER_RESULT') {
                    this.handleFilterResult(imageData);
                } else if (type === 'ERROR') {
                    console.error('Worker error:', error);
                    this.state.isProcessing = false;
                }
            };

            this.state.worker.onerror = (error) => {
                console.error('Worker error:', error);
                this.state.isProcessing = false;
            };
        } catch (error) {
            console.error('Erro ao criar Worker:', error);
            // Fallback: processa na thread principal
            this.state.worker = null;
        }
    }

    /**
     * Carrega imagem do File
     */
    async loadImage(file) {
        if (!this.initialized) {
            this.init();
        }

        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = (e) => {
                const img = new Image();

                img.onload = () => {
                    // Redimensiona se necessário (max 2048px)
                    const maxSize = 2048;
                    let width = img.width;
                    let height = img.height;

                    if (width > maxSize || height > maxSize) {
                        if (width > height) {
                            height = Math.round((height * maxSize) / width);
                            width = maxSize;
                        } else {
                            width = Math.round((width * maxSize) / height);
                            height = maxSize;
                        }
                    }

                    // Define tamanho dos canvas
                    this.canvasOriginal.width = width;
                    this.canvasOriginal.height = height;
                    this.canvasEdited.width = width;
                    this.canvasEdited.height = height;

                    // Desenha original
                    this.ctxOriginal.drawImage(img, 0, 0, width, height);
                    this.ctxEdited.drawImage(img, 0, 0, width, height);

                    // Armazena ImageData
                    this.state.originalImage = this.ctxOriginal.getImageData(0, 0, width, height);
                    this.state.currentImage = this.ctxEdited.getImageData(0, 0, width, height);
                    this.state.fileName = file.name.replace(/\.[^/.]+$/, '');

                    resolve();
                };

                img.onerror = reject;
                img.src = e.target.result;
            };

            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }

    /**
     * Aplica filtros (envia para Worker ou processa localmente)
     */
    applyFilters() {
        if (!this.state.originalImage || this.state.isProcessing) {
            return;
        }

        this.state.isProcessing = true;

        // Clone imageData
        const imageData = new ImageData(
            new Uint8ClampedArray(this.state.originalImage.data),
            this.state.originalImage.width,
            this.state.originalImage.height
        );

        if (this.state.worker) {
            // Processa no Worker (thread separada)
            try {
                this.state.worker.postMessage({
                    type: 'APPLY_FILTERS',
                    imageData: imageData,
                    filters: { ...this.state.filters }
                }, [imageData.data.buffer]);
            } catch (error) {
                console.error('Erro ao enviar para Worker:', error);
                // Fallback: processa localmente
                this.applyFiltersLocal(imageData);
            }
        } else {
            // Processa localmente
            this.applyFiltersLocal(imageData);
        }
    }

    /**
     * Aplica filtros na thread principal (fallback)
     */
    applyFiltersLocal(imageData) {
        try {
            const result = applyAllFilters(imageData, this.state.filters);
            this.handleFilterResult(result);
        } catch (error) {
            console.error('Erro ao aplicar filtros:', error);
            this.state.isProcessing = false;
        }
    }

    /**
     * Recebe resultado do Worker
     */
    handleFilterResult(imageData) {
        this.state.currentImage = imageData;
        this.ctxEdited.putImageData(imageData, 0, 0);

        // Atualiza também o canvas do slider se estiver no modo slider
        if (this.viewMode === 'slider' && this.ctxSliderAfter) {
            this.ctxSliderAfter.putImageData(imageData, 0, 0);
        }

        this.state.isProcessing = false;
    }

    /**
     * Atualiza um filtro específico
     */
    updateFilter(filterName, value) {
        this.state.filters[filterName] = value;
        this.applyFilters();
    }

    /**
     * Reseta todos os filtros
     */
    resetFilters() {
        this.state.filters = {
            brightness: 0,
            contrast: 0,
            gamma: 0,
            saturation: 0,
            hue: 0,
            red: 0,
            green: 0,
            blue: 0,
            temperature: 0,
            shadows: 0
        };

        // Restaura original
        if (this.state.originalImage) {
            this.ctxEdited.putImageData(this.state.originalImage, 0, 0);
            this.state.currentImage = new ImageData(
                new Uint8ClampedArray(this.state.originalImage.data),
                this.state.originalImage.width,
                this.state.originalImage.height
            );

            // Atualiza também o canvas do slider se estiver no modo slider
            if (this.viewMode === 'slider' && this.ctxSliderAfter) {
                this.ctxSliderAfter.putImageData(this.state.originalImage, 0, 0);
            }
        }
    }

    /**
     * Download da imagem editada
     */
    downloadImage(format = 'jpeg', quality = 0.95) {
        const link = document.createElement('a');
        // Usa nome original do arquivo + _StoneEditor
        const originalName = this.state.fileName || `stone-editor_${Date.now()}`;
        const fileName = `${originalName}_StoneEditor.${format}`;

        this.canvasEdited.toBlob((blob) => {
            const url = URL.createObjectURL(blob);
            link.href = url;
            link.download = fileName;
            link.click();
            URL.revokeObjectURL(url);
        }, `image/${format}`, quality);
    }

    /**
     * Inicializa eventos do slider comparativo
     */
    initSliderEvents() {
        if (!this.comparisonDivider || !this.comparisonContainer) return;

        // Mouse events
        this.comparisonDivider.addEventListener('mousedown', this.onSliderDragStart.bind(this));
        document.addEventListener('mousemove', this.onSliderDrag.bind(this));
        document.addEventListener('mouseup', this.onSliderDragEnd.bind(this));

        // Touch events (mobile)
        this.comparisonDivider.addEventListener('touchstart', this.onSliderDragStart.bind(this), { passive: false });
        document.addEventListener('touchmove', this.onSliderDrag.bind(this), { passive: false });
        document.addEventListener('touchend', this.onSliderDragEnd.bind(this));

        // Clique direto no container para mover o divisor
        this.comparisonContainer.addEventListener('click', this.onContainerClick.bind(this));
    }

    /**
     * Início do drag (mouse/touch)
     */
    onSliderDragStart(e) {
        e.preventDefault();
        this.sliderState.isDragging = true;
        this.comparisonContainer.style.cursor = 'ew-resize';
    }

    /**
     * Durante o drag
     */
    onSliderDrag(e) {
        if (!this.sliderState.isDragging) return;
        e.preventDefault();

        const containerRect = this.comparisonContainer.getBoundingClientRect();
        let clientX;

        if (e.type === 'touchmove') {
            clientX = e.touches[0].clientX;
        } else {
            clientX = e.clientX;
        }

        // Calcula posição relativa (0-100%)
        const x = clientX - containerRect.left;
        const percentage = Math.max(0, Math.min(100, (x / containerRect.width) * 100));

        this.updateSliderPosition(percentage);
    }

    /**
     * Fim do drag
     */
    onSliderDragEnd(e) {
        if (!this.sliderState.isDragging) return;
        this.sliderState.isDragging = false;
        this.comparisonContainer.style.cursor = '';
    }

    /**
     * Clique direto no container
     */
    onContainerClick(e) {
        // Ignora se clicou no divisor
        if (e.target.closest('.comparison-divider')) return;

        const containerRect = this.comparisonContainer.getBoundingClientRect();
        const x = e.clientX - containerRect.left;
        const percentage = Math.max(0, Math.min(100, (x / containerRect.width) * 100));

        this.updateSliderPosition(percentage);
    }

    /**
     * Atualiza posição do slider e clip-path
     */
    updateSliderPosition(percentage) {
        this.sliderState.currentPosition = percentage;

        // Atualiza posição do divisor
        this.comparisonDivider.style.left = `${percentage}%`;

        // Atualiza clip-path da imagem original (before)
        this.comparisonBeforeWrapper.style.clipPath =
            `polygon(0 0, ${percentage}% 0, ${percentage}% 100%, 0 100%)`;
    }

    /**
     * Sincroniza canvas do lado-a-lado para o slider
     */
    syncCanvasToSlider() {
        if (!this.state.originalImage || !this.canvasSliderBefore || !this.canvasSliderAfter) {
            return;
        }

        const width = this.state.originalImage.width;
        const height = this.state.originalImage.height;

        // Configura dimensões
        this.canvasSliderBefore.width = width;
        this.canvasSliderBefore.height = height;
        this.canvasSliderAfter.width = width;
        this.canvasSliderAfter.height = height;

        // Copia imagem original para canvas Before
        this.ctxSliderBefore.putImageData(this.state.originalImage, 0, 0);

        // Copia imagem editada para canvas After
        if (this.state.currentImage) {
            this.ctxSliderAfter.putImageData(this.state.currentImage, 0, 0);
        }

        // Reseta posição do slider para 50%
        this.updateSliderPosition(50);
    }

    /**
     * Alterna entre modo lado-a-lado e slider
     */
    toggleViewMode() {
        const previewSection = document.getElementById('editorPreviewSection');
        const sliderSection = document.getElementById('editorSliderSection');
        const viewModeLabel = document.getElementById('viewModeLabel');

        if (this.viewMode === 'side-by-side') {
            // Muda para slider
            previewSection.classList.add('hidden');
            sliderSection.classList.remove('hidden');
            viewModeLabel.textContent = 'Modo Lado-a-Lado';
            this.viewMode = 'slider';

            // Sincroniza os canvas
            this.syncCanvasToSlider();
        } else {
            // Muda para lado-a-lado
            sliderSection.classList.add('hidden');
            previewSection.classList.remove('hidden');
            viewModeLabel.textContent = 'Modo Comparação';
            this.viewMode = 'side-by-side';
        }
    }

    /**
     * Cleanup
     */
    destroy() {
        if (this.state.worker) {
            this.state.worker.terminate();
            this.state.worker = null;
        }
        this.initialized = false;
    }
}

// Export singleton
const editorInstance = new ImageEditor();
window.ImageEditor = editorInstance;
