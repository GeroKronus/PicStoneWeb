// BookMatch Feature - Usando sistema de crop reutilizável do app.js

// Estado do BookMatch
const bookmatchState = {
    originalImage: null,      // Imagem original em Base64
    currentImage: null,       // Imagem atual (pode ser cropada)
    results: null
};

// Elements
const bookmatchElements = {
    bookmatchScreen: document.getElementById('bookmatchScreen'),
    bookmatchCard: document.getElementById('bookmatchCard'),
    backToMainFromBookmatchBtn: document.getElementById('backToMainFromBookmatchBtn'),

    // Photo capture
    captureBtnBookmatch: document.getElementById('captureBtnBookmatch'),
    fileInputBookmatch: document.getElementById('fileInputBookmatch'),
    photoPreviewBookmatch: document.getElementById('photoPreviewBookmatch'),
    previewImageBookmatch: document.getElementById('previewImageBookmatch'),
    clearPhotoBtnBookmatch: document.getElementById('clearPhotoBtnBookmatch'),
    adjustImageBtnBookmatch: document.getElementById('adjustImageBtnBookmatch'),
    resetImageBtnBookmatch: document.getElementById('resetImageBtnBookmatch'),
    cropOverlayBookmatch: document.getElementById('cropOverlayBookmatch'),
    cropIndicatorBookmatch: document.getElementById('cropIndicatorBookmatch'),
    captureSectionBookmatch: document.getElementById('captureSectionBookmatch'),

    // Options
    bookmatchOptions: document.getElementById('bookmatchOptions'),
    targetWidth: document.getElementById('targetWidth'),
    addSeparatorLines: document.getElementById('addSeparatorLines'),
    generateBookmatchBtn: document.getElementById('generateBookmatchBtn'),

    // Results
    bookmatchResults: document.getElementById('bookmatchResults'),
    mosaicImage: document.getElementById('mosaicImage'),
    quadrant1Image: document.getElementById('quadrant1Image'),
    quadrant2Image: document.getElementById('quadrant2Image'),
    quadrant3Image: document.getElementById('quadrant3Image'),
    quadrant4Image: document.getElementById('quadrant4Image'),
    newBookmatchBtn: document.getElementById('newBookmatchBtn')
};

// Inicializar BookMatch
function initBookMatch() {
    // Navegação
    if (bookmatchElements.bookmatchCard) {
        bookmatchElements.bookmatchCard.addEventListener('click', showBookmatchScreen);
    }
    if (bookmatchElements.backToMainFromBookmatchBtn) {
        bookmatchElements.backToMainFromBookmatchBtn.addEventListener('click', () => {
            document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
            document.getElementById('mainScreen').classList.add('active');
        });
    }

    // Captura de foto
    if (bookmatchElements.captureBtnBookmatch) {
        bookmatchElements.captureBtnBookmatch.addEventListener('click', () => bookmatchElements.fileInputBookmatch.click());
    }
    if (bookmatchElements.fileInputBookmatch) {
        bookmatchElements.fileInputBookmatch.addEventListener('change', handleBookmatchFileSelect);
    }
    if (bookmatchElements.clearPhotoBtnBookmatch) {
        bookmatchElements.clearPhotoBtnBookmatch.addEventListener('click', clearBookmatchPhoto);
    }
    if (bookmatchElements.adjustImageBtnBookmatch) {
        bookmatchElements.adjustImageBtnBookmatch.addEventListener('click', startBookmatchCropMode);
    }
    if (bookmatchElements.resetImageBtnBookmatch) {
        bookmatchElements.resetImageBtnBookmatch.addEventListener('click', resetToOriginalBookmatch);
    }

    // Gerar BookMatch
    if (bookmatchElements.generateBookmatchBtn) {
        bookmatchElements.generateBookmatchBtn.addEventListener('click', generateBookmatch);
    }

    // Novo BookMatch
    if (bookmatchElements.newBookmatchBtn) {
        bookmatchElements.newBookmatchBtn.addEventListener('click', resetBookmatch);
    }

    // Adiciona event listeners genéricos ao canvas do BookMatch (APENAS UMA VEZ)
    if (bookmatchElements.cropOverlayBookmatch && !bookmatchElements.cropOverlayBookmatch.hasAttribute('data-listeners-added')) {
        bookmatchElements.cropOverlayBookmatch.addEventListener('mousedown', iniciarSelecaoCrop);
        bookmatchElements.cropOverlayBookmatch.addEventListener('touchstart', iniciarSelecaoCropTouch, { passive: false });
        bookmatchElements.cropOverlayBookmatch.setAttribute('data-listeners-added', 'true');
    }
}

// Mostrar tela do BookMatch
function showBookmatchScreen() {
    document.querySelectorAll('.screen').forEach(screen => screen.classList.remove('active'));
    bookmatchElements.bookmatchScreen.classList.add('active');

    // Carrega automaticamente imagem compartilhada se existir (função definida em app.js)
    if (typeof hasSharedImage === 'function' && typeof loadSharedImage === 'function' && hasSharedImage()) {
        const sharedImage = loadSharedImage('bookmatch');
        if (sharedImage) {
            bookmatchState.originalImage = sharedImage.originalImage;
            bookmatchState.currentImage = sharedImage.currentImage;
            bookmatchElements.previewImageBookmatch.src = sharedImage.currentImage;
            bookmatchElements.photoPreviewBookmatch.classList.remove('hidden');
            bookmatchElements.bookmatchOptions.classList.remove('hidden');
            bookmatchElements.captureSectionBookmatch.classList.add('hidden');
        }
    } else {
        resetBookmatch();
    }
}

// Reset completo
function resetBookmatch() {
    bookmatchState.originalImage = null;
    bookmatchState.currentImage = null;
    bookmatchState.results = null;

    bookmatchElements.photoPreviewBookmatch.classList.add('hidden');
    bookmatchElements.bookmatchOptions.classList.add('hidden');
    bookmatchElements.bookmatchResults.classList.add('hidden');
    bookmatchElements.resetImageBtnBookmatch.classList.add('hidden');
    bookmatchElements.cropOverlayBookmatch.classList.add('hidden');
    bookmatchElements.cropIndicatorBookmatch.classList.add('hidden');
    bookmatchElements.fileInputBookmatch.value = '';

    // Mostra botão "Escolher/Tirar Foto" novamente
    bookmatchElements.captureSectionBookmatch.classList.remove('hidden');

    // Limpa estado compartilhado (função definida em app.js)
    if (typeof clearSharedImage === 'function') {
        clearSharedImage();
    }
}

// Manipular seleção de arquivo
function handleBookmatchFileSelect(e) {
    const file = e.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
        // Salvar imagem original
        bookmatchState.originalImage = e.target.result;
        bookmatchState.currentImage = e.target.result;

        // Mostrar preview
        bookmatchElements.previewImageBookmatch.src = e.target.result;
        bookmatchElements.photoPreviewBookmatch.classList.remove('hidden');
        bookmatchElements.bookmatchOptions.classList.remove('hidden');
        bookmatchElements.bookmatchResults.classList.add('hidden');
        bookmatchElements.resetImageBtnBookmatch.classList.add('hidden');

        // Esconde botão "Escolher/Tirar Foto"
        bookmatchElements.captureSectionBookmatch.classList.add('hidden');

        // Salva imagem no estado compartilhado (acessível de app.js)
        if (typeof saveSharedImage === 'function') {
            saveSharedImage(e.target.result, e.target.result, file.name, file, 'bookmatch');
        }
    };
    reader.readAsDataURL(file);
}

// Limpar foto
function clearBookmatchPhoto() {
    resetBookmatch();
}

// Restaurar imagem original
function resetToOriginalBookmatch() {
    if (bookmatchState.originalImage) {
        bookmatchState.currentImage = bookmatchState.originalImage;
        bookmatchElements.previewImageBookmatch.src = bookmatchState.originalImage;
        bookmatchElements.resetImageBtnBookmatch.classList.add('hidden');
        bookmatchElements.cropOverlayBookmatch.classList.add('hidden');
        bookmatchElements.cropIndicatorBookmatch.classList.add('hidden');
    }
}

// ===== CROP USANDO SISTEMA GENÉRICO REUTILIZÁVEL =====
// WHY: Mesma lógica da Integração e Ambientes, zero duplicação de código (DRY)

function startBookmatchCropMode() {
    // Usa o sistema de crop overlay genérico do app.js
    ativarCropOverlay(
        bookmatchElements.previewImageBookmatch,
        bookmatchElements.cropOverlayBookmatch,
        bookmatchElements.resetImageBtnBookmatch,
        (croppedBase64, croppedFile) => {
            // Atualiza a imagem atual com a versão cropada
            bookmatchState.currentImage = croppedBase64;
            bookmatchElements.previewImageBookmatch.src = croppedBase64;
        },
        bookmatchElements.cropIndicatorBookmatch  // Passa o indicador visual
    );
}

// ===== GERAR BOOKMATCH =====

async function generateBookmatch() {
    if (!bookmatchState.currentImage) {
        alert('Selecione uma foto primeiro');
        return;
    }

    const loadingOverlay = document.getElementById('loadingOverlay');
    if (loadingOverlay) loadingOverlay.classList.remove('hidden');

    try {
        // Obter dimensões da imagem atual
        const img = bookmatchElements.previewImageBookmatch;

        const requestData = {
            imageData: bookmatchState.currentImage,
            cropX: 0,
            cropY: 0,
            cropWidth: img.naturalWidth,
            cropHeight: img.naturalHeight,
            targetWidth: parseInt(bookmatchElements.targetWidth.value) || 800,
            addSeparatorLines: bookmatchElements.addSeparatorLines.checked
        };

        const response = await fetch('/api/bookmatch/generate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao gerar BookMatch');
        }

        const result = await response.json();

        // Mostrar resultados
        bookmatchElements.mosaicImage.src = result.mosaic;
        bookmatchElements.quadrant1Image.src = result.quadrant1;
        bookmatchElements.quadrant2Image.src = result.quadrant2;
        bookmatchElements.quadrant3Image.src = result.quadrant3;
        bookmatchElements.quadrant4Image.src = result.quadrant4;

        bookmatchState.results = result;

        bookmatchElements.photoPreviewBookmatch.classList.add('hidden');
        bookmatchElements.bookmatchOptions.classList.add('hidden');
        bookmatchElements.bookmatchResults.classList.remove('hidden');

    } catch (error) {
        console.error('Erro ao gerar BookMatch:', error);
        alert('Erro ao gerar BookMatch: ' + error.message);
    } finally {
        if (loadingOverlay) loadingOverlay.classList.add('hidden');
    }
}

// ===== DOWNLOAD E COMPARTILHAR =====

function downloadBookmatchImage(type) {
    if (!bookmatchState.results) return;

    const mapping = {
        'mosaic': { url: bookmatchState.results.mosaic, filename: 'bookmatch-mosaico.jpg' },
        'quad1': { url: bookmatchState.results.quadrant1, filename: 'bookmatch-quadrante-1.jpg' },
        'quad2': { url: bookmatchState.results.quadrant2, filename: 'bookmatch-quadrante-2.jpg' },
        'quad3': { url: bookmatchState.results.quadrant3, filename: 'bookmatch-quadrante-3.jpg' },
        'quad4': { url: bookmatchState.results.quadrant4, filename: 'bookmatch-quadrante-4.jpg' }
    };

    const { url, filename } = mapping[type];

    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

async function shareBookmatchImage(type) {
    if (!bookmatchState.results) return;

    const mapping = {
        'mosaic': { url: bookmatchState.results.mosaic, filename: 'bookmatch-mosaico.jpg' },
        'quad1': { url: bookmatchState.results.quadrant1, filename: 'bookmatch-quadrante-1.jpg' },
        'quad2': { url: bookmatchState.results.quadrant2, filename: 'bookmatch-quadrante-2.jpg' },
        'quad3': { url: bookmatchState.results.quadrant3, filename: 'bookmatch-quadrante-3.jpg' },
        'quad4': { url: bookmatchState.results.quadrant4, filename: 'bookmatch-quadrante-4.jpg' }
    };

    const { url, filename } = mapping[type];

    try {
        if (navigator.share) {
            const response = await fetch(url);
            const blob = await response.blob();
            const file = new File([blob], filename, { type: 'image/jpeg' });

            await navigator.share({
                title: 'BookMatch - PicStone',
                text: 'Confira este BookMatch!',
                files: [file]
            });
        } else {
            // Fallback 1: Compartilhar via WhatsApp Web
            const texto = encodeURIComponent('Confira este BookMatch! Make with PicStone® mobile');
            const whatsappUrl = `https://wa.me/?text=${texto}`;
            window.open(whatsappUrl, '_blank');
        }
    } catch (error) {
        if (error.name !== 'AbortError') {
            console.error('Erro ao compartilhar:', error);

            // Fallback 2: Copiar link para clipboard
            try {
                await navigator.clipboard.writeText(url);
                alert('Link copiado! Cole no WhatsApp');
            } catch (clipError) {
                // Fallback 3: Download como última opção
                downloadBookmatchImage(type);
            }
        }
    }
}

// Inicializar quando o DOM estiver pronto
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initBookMatch);
} else {
    initBookMatch();
}
