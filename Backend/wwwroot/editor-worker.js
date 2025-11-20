/**
 * WEB WORKER - Processamento de Imagens
 *
 * Roda em thread separada (não bloqueia UI)
 * Recebe ImageData + filtros, retorna ImageData processado
 * Usa Transferable Objects para zero-copy performance
 */

// Import do módulo de filtros
importScripts('editor-filters.js');

self.onmessage = function(e) {
    const { type, imageData, filters } = e.data;

    if (type === 'APPLY_FILTERS') {
        try {
            // Aplica todos os filtros em sequência
            const result = applyAllFilters(imageData, filters);

            // Retorna resultado (transferable object)
            self.postMessage({
                type: 'FILTER_RESULT',
                imageData: result
            }, [result.data.buffer]);

        } catch (error) {
            self.postMessage({
                type: 'ERROR',
                error: error.message
            });
        }
    }
};
