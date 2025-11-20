/**
 * EDITOR UI - Controles de Interface
 *
 * Gerencia sliders, eventos e feedback visual
 * Debouncing para performance otimizada
 */

class EditorUI {
    constructor(editorInstance) {
        this.editor = editorInstance;
        this.sliders = {};
        this.values = {};

        // Debounce para evitar processamento excessivo
        this.debouncedApply = this.debounce(
            () => this.editor.applyFilters(),
            16 // ~60fps
        );

        this.init();
    }

    init() {
        this.setupSliders();
        this.attachEventListeners();
    }

    /**
     * Configura todos os sliders
     */
    setupSliders() {
        const sliderConfigs = [
            { id: 'brightness', label: 'Brilho', min: -100, max: 100, default: 0 },
            { id: 'contrast', label: 'Contraste', min: -100, max: 100, default: 0 },
            { id: 'gamma', label: 'Gama', min: -10, max: 10, default: 0 },
            { id: 'saturation', label: 'Saturação', min: -100, max: 100, default: 0 },
            { id: 'hue', label: 'Matiz', min: -180, max: 180, default: 0 },
            { id: 'red', label: 'Vermelho', min: -100, max: 100, default: 0 },
            { id: 'green', label: 'Verde', min: -100, max: 100, default: 0 },
            { id: 'blue', label: 'Azul', min: -100, max: 100, default: 0 },
            { id: 'temperature', label: 'Temperatura', min: -100, max: 100, default: 0 },
            { id: 'shadows', label: 'Sombras', min: -100, max: 100, default: 0 }
        ];

        sliderConfigs.forEach(config => {
            const slider = document.getElementById(`slider${this.capitalize(config.id)}`);
            const valueDisplay = document.getElementById(`value${this.capitalize(config.id)}`);

            if (slider && valueDisplay) {
                this.sliders[config.id] = slider;
                this.values[config.id] = valueDisplay;

                // Event listener para cada slider
                slider.addEventListener('input', (e) => {
                    this.handleSliderChange(config.id, parseFloat(e.target.value));
                });

                // Double-click para resetar individual
                slider.addEventListener('dblclick', () => {
                    this.resetSlider(config.id, config.default);
                });
            }
        });
    }

    /**
     * Anexa event listeners globais
     */
    attachEventListeners() {
        // Botão Reset Todos
        const resetBtn = document.getElementById('resetAllSlidersBtn');
        if (resetBtn) {
            resetBtn.addEventListener('click', () => {
                this.resetAll();
            });
        }

        // Collapse/Expand sliders (accordion exclusivo com reordenação)
        const sliderHeaders = document.querySelectorAll('.slider-header');
        sliderHeaders.forEach(header => {
            header.addEventListener('click', () => {
                const sliderGroup = header.closest('.slider-group');
                const container = sliderGroup.parentElement;
                const isExpanded = sliderGroup.classList.contains('expanded');

                // Colapsa todos os outros primeiro
                document.querySelectorAll('.slider-group.expanded').forEach(group => {
                    if (group !== sliderGroup) {
                        group.classList.remove('expanded');
                    }
                });

                // Toggle no clicado
                if (isExpanded) {
                    // Se estava expandido, apenas colapsa (não move)
                    sliderGroup.classList.remove('expanded');
                } else {
                    // Se vai expandir, move para o topo e expande
                    sliderGroup.classList.add('expanded');

                    // Move para o primeiro da lista (logo após o h3)
                    const firstSlider = container.querySelector('.slider-group');
                    if (firstSlider && firstSlider !== sliderGroup) {
                        container.insertBefore(sliderGroup, firstSlider);

                        // Scroll suave para o topo dos controles
                        setTimeout(() => {
                            container.scrollIntoView({ behavior: 'smooth', block: 'start' });
                        }, 50);
                    }
                }
            });
        });
    }

    /**
     * Manipula mudança de slider
     */
    handleSliderChange(filterName, value) {
        // Atualiza valor visual
        if (this.values[filterName]) {
            this.values[filterName].textContent = value;
        }

        // Atualiza estado do editor
        this.editor.updateFilter(filterName, value);
    }

    /**
     * Reseta slider individual
     */
    resetSlider(filterName, defaultValue = 0) {
        if (this.sliders[filterName]) {
            this.sliders[filterName].value = defaultValue;
        }
        if (this.values[filterName]) {
            this.values[filterName].textContent = defaultValue;
        }
        this.editor.updateFilter(filterName, defaultValue);
    }

    /**
     * Reseta todos os sliders
     */
    resetAll() {
        Object.keys(this.sliders).forEach(filterName => {
            if (this.sliders[filterName]) {
                this.sliders[filterName].value = 0;
            }
            if (this.values[filterName]) {
                this.values[filterName].textContent = 0;
            }
        });

        this.editor.resetFilters();
    }

    /**
     * Utilitário: Capitaliza primeira letra
     */
    capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }

    /**
     * Utilitário: Debounce
     */
    debounce(func, wait) {
        let timeout;
        return function(...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    }
}

// Export
window.EditorUI = EditorUI;
