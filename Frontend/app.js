// Configuração da API
const API_URL = window.location.origin;

// Estado da aplicação
const state = {
    token: localStorage.getItem('token') || null,
    username: localStorage.getItem('username') || null,
    currentPhoto: null,
    currentPhotoFile: null,
    originalPhoto: null, // Foto original para mockup
    mockupMode: false, // Indica se está em modo mockup
    cropData: {
        image: null,
        startX: 0,
        startY: 0,
        endX: 0,
        endY: 0,
        isDragging: false,
        scale: 1
    },
    mockupConfig: {
        tipo: 'simples',
        fundo: 'claro'
    }
};

// Elementos DOM
const elements = {
    loginScreen: document.getElementById('loginScreen'),
    mainScreen: document.getElementById('mainScreen'),
    cropScreen: document.getElementById('cropScreen'),
    historyScreen: document.getElementById('historyScreen'),
    mockupConfigScreen: document.getElementById('mockupConfigScreen'),
    mockupResultScreen: document.getElementById('mockupResultScreen'),
    loginForm: document.getElementById('loginForm'),
    loginError: document.getElementById('loginError'),
    captureBtn: document.getElementById('captureBtn'),
    fileInput: document.getElementById('fileInput'),
    photoPreview: document.getElementById('photoPreview'),
    previewImage: document.getElementById('previewImage'),
    clearPhotoBtn: document.getElementById('clearPhotoBtn'),
    adjustImageBtn: document.getElementById('adjustImageBtn'),
    resetImageBtn: document.getElementById('resetImageBtn'),
    uploadForm: document.getElementById('uploadForm'),
    submitBtn: document.getElementById('submitBtn'),
    uploadProgress: document.getElementById('uploadProgress'),
    uploadMessage: document.getElementById('uploadMessage'),
    userDisplay: document.getElementById('userDisplay'),
    logoutBtn: document.getElementById('logoutBtn'),
    historyBtn: document.getElementById('historyBtn'),
    backBtn: document.getElementById('backBtn'),
    historyList: document.getElementById('historyList'),
    cropCanvas: document.getElementById('cropCanvas'),
    cancelCropBtn: document.getElementById('cancelCropBtn'),
    resetCropBtn: document.getElementById('resetCropBtn'),
    confirmCropBtn: document.getElementById('confirmCropBtn'),
    mockupBtn: document.getElementById('mockupBtn'),
    photoIndicator: document.getElementById('photoIndicator'),
    cancelMockupBtn: document.getElementById('cancelMockupBtn'),
    continuarCropMockupBtn: document.getElementById('continuarCropMockupBtn'),
    backToMainBtn: document.getElementById('backToMainBtn'),
    downloadMockupBtn: document.getElementById('downloadMockupBtn'),
    newMockupBtn: document.getElementById('newMockupBtn'),
    mockupImage: document.getElementById('mockupImage'),
    mockupMessage: document.getElementById('mockupMessage')
};

// ========== INICIALIZAÇÃO ==========
document.addEventListener('DOMContentLoaded', () => {
    if (state.token) {
        showMainScreen();
    } else {
        showLoginScreen();
    }

    setupEventListeners();
});

// ========== EVENT LISTENERS ==========
function setupEventListeners() {
    elements.loginForm.addEventListener('submit', handleLogin);

    // Captura de foto - múltiplos eventos para compatibilidade mobile
    elements.captureBtn.addEventListener('click', () => elements.fileInput.click());
    elements.fileInput.addEventListener('change', handleFileSelect);
    elements.fileInput.addEventListener('input', handleFileSelect); // Fallback mobile

    elements.clearPhotoBtn.addEventListener('click', clearPhoto);
    elements.adjustImageBtn.addEventListener('click', abrirCropParaAjuste);
    elements.resetImageBtn.addEventListener('click', resetToOriginalImage);
    elements.uploadForm.addEventListener('submit', handleUpload);
    elements.logoutBtn.addEventListener('click', handleLogout);
    elements.historyBtn.addEventListener('click', showHistoryScreen);
    elements.backBtn.addEventListener('click', showMainScreen);

    // Crop event listeners
    elements.cancelCropBtn.addEventListener('click', cancelCrop);
    elements.resetCropBtn.addEventListener('click', resetCrop);
    elements.confirmCropBtn.addEventListener('click', confirmCrop);

    // Canvas mouse/touch events
    elements.cropCanvas.addEventListener('mousedown', startCrop);
    elements.cropCanvas.addEventListener('mousemove', updateCrop);
    elements.cropCanvas.addEventListener('mouseup', endCrop);
    elements.cropCanvas.addEventListener('touchstart', handleTouchStart, { passive: false });
    elements.cropCanvas.addEventListener('touchmove', handleTouchMove, { passive: false });
    elements.cropCanvas.addEventListener('touchend', endCrop);

    // Mockup event listeners
    elements.mockupBtn.addEventListener('click', startMockupFlow);
    elements.cancelMockupBtn.addEventListener('click', () => showMainScreen());
    elements.continuarCropMockupBtn.addEventListener('click', abrirCropParaMockup);
    elements.backToMainBtn.addEventListener('click', () => showMainScreen());
    elements.newMockupBtn.addEventListener('click', startMockupFlow);
    elements.downloadMockupBtn.addEventListener('click', downloadMockup);
}

// ========== AUTENTICAÇÃO ==========
async function handleLogin(e) {
    e.preventDefault();

    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch(`${API_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            throw new Error('Usuário ou senha inválidos');
        }

        const data = await response.json();

        state.token = data.token;
        state.username = data.username;

        localStorage.setItem('token', data.token);
        localStorage.setItem('username', data.username);

        showMainScreen();
        elements.loginError.classList.add('hidden');
    } catch (error) {
        elements.loginError.textContent = error.message;
        elements.loginError.classList.remove('hidden');
    }
}

function handleLogout() {
    state.token = null;
    state.username = null;
    localStorage.removeItem('token');
    localStorage.removeItem('username');

    clearPhoto();
    elements.uploadForm.reset();

    showLoginScreen();
}

// ========== NAVEGAÇÃO ==========
function showScreen(screen) {
    document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
    screen.classList.add('active');
}

function showLoginScreen() {
    showScreen(elements.loginScreen);
}

async function showMainScreen() {
    showScreen(elements.mainScreen);
    elements.userDisplay.textContent = `Olá, ${state.username}`;
    await loadMaterials();
}

async function showHistoryScreen() {
    showScreen(elements.historyScreen);
    await loadHistory();
}

// ========== MATERIAIS ==========
async function loadMaterials() {
    const materialSelect = document.getElementById('material');

    try {
        const response = await fetch(`${API_URL}/api/materiais`);

        if (!response.ok) {
            throw new Error('Erro ao carregar materiais');
        }

        const materiais = await response.json();

        // Limpa opções existentes
        materialSelect.innerHTML = '<option value="">Selecione um material</option>';

        // Adiciona materiais como opções
        materiais.forEach(material => {
            const option = document.createElement('option');
            option.value = material;
            option.textContent = material;
            materialSelect.appendChild(option);
        });

    } catch (error) {
        console.error('Erro ao carregar materiais:', error);
        materialSelect.innerHTML = '<option value="">Erro ao carregar materiais</option>';
    }
}

// ========== CAPTURA DE FOTO ==========
function handleFileSelect(e) {
    const file = e.target.files[0];

    if (!file) return;

    // Valida tipo de arquivo
    if (!file.type.startsWith('image/')) {
        showMessage('Por favor, selecione uma imagem válida', 'error');
        return;
    }

    // Valida tamanho (10MB)
    if (file.size > 10 * 1024 * 1024) {
        showMessage('Arquivo muito grande. Máximo 10MB', 'error');
        return;
    }

    // NOVA FOTO - Limpa estado anterior e oculta botões
    clearPhotoState();

    // Salva arquivo e exibe preview (SEM abrir crop automaticamente)
    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            // Salva imagem original (permanece até nova foto)
            state.originalPhoto = img;

            // Converte para arquivo e exibe preview
            compressAndPreviewImage(file);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function compressAndPreviewImage(file) {
    const reader = new FileReader();

    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            // Comprime imagem para máximo 1920x1080
            const canvas = document.createElement('canvas');
            let width = img.width;
            let height = img.height;

            if (width > 1920 || height > 1080) {
                const ratio = Math.min(1920 / width, 1080 / height);
                width = width * ratio;
                height = height * ratio;
            }

            canvas.width = width;
            canvas.height = height;

            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, width, height);

            canvas.toBlob((blob) => {
                state.currentPhotoFile = new File([blob], file.name, {
                    type: 'image/jpeg',
                    lastModified: Date.now()
                });

                // Exibe preview
                elements.previewImage.src = canvas.toDataURL('image/jpeg', 0.85);
                elements.photoPreview.classList.remove('hidden');
                elements.submitBtn.disabled = false;

            }, 'image/jpeg', 0.85);
        };
        img.src = e.target.result;
    };

    reader.readAsDataURL(file);
}

function clearPhoto() {
    // Limpa TUDO incluindo imagem original (usuário clicou no X)
    state.currentPhotoFile = null;
    state.originalPhoto = null;
    elements.previewImage.src = '';
    elements.photoPreview.classList.add('hidden');
    elements.fileInput.value = '';
    elements.submitBtn.disabled = true;
    elements.mockupBtn.classList.add('hidden');
    elements.photoIndicator.classList.add('hidden');
}

function clearPhotoState() {
    // Limpa apenas estado atual (para preparar nova foto)
    state.currentPhotoFile = null;
    elements.previewImage.src = '';
    elements.photoPreview.classList.add('hidden');
    elements.submitBtn.disabled = true;
    elements.mockupBtn.classList.add('hidden');
    elements.photoIndicator.classList.add('hidden');
}

// ========== UPLOAD ==========
async function handleUpload(e) {
    e.preventDefault();

    if (!state.currentPhotoFile) {
        showMessage('Por favor, tire uma foto primeiro', 'error');
        return;
    }

    const formData = new FormData();
    formData.append('Arquivo', state.currentPhotoFile);
    formData.append('Material', document.getElementById('material').value);
    formData.append('Bloco', document.getElementById('bloco').value);
    formData.append('Chapa', document.getElementById('chapa').value);

    const espessura = document.getElementById('espessura').value;
    if (espessura) {
        formData.append('Espessura', espessura);
    }

    try {
        elements.uploadProgress.classList.remove('hidden');
        elements.submitBtn.disabled = true;

        const response = await fetch(`${API_URL}/api/fotos/upload`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.mensagem || 'Erro ao enviar foto');
        }

        showMessage(data.mensagem, 'success');

        // Mostra botão de mockup (permanece visível)
        elements.mockupBtn.classList.remove('hidden');

        // Limpa apenas o preview e formulário (mantém imagem original)
        setTimeout(() => {
            state.currentPhotoFile = null;
            elements.previewImage.src = '';
            elements.photoPreview.classList.add('hidden');
            elements.fileInput.value = '';
            elements.submitBtn.disabled = true;
            elements.uploadForm.reset();
            // Mostra indicador de foto disponível
            elements.photoIndicator.classList.remove('hidden');
            // NÃO limpa state.originalPhoto - fica disponível para mockup/ajuste
            // NÃO oculta mockupBtn - fica acessível
        }, 2000);

    } catch (error) {
        showMessage(error.message, 'error');
        elements.submitBtn.disabled = false;
    } finally {
        elements.uploadProgress.classList.add('hidden');
    }
}

// ========== HISTÓRICO ==========
async function loadHistory() {
    elements.historyList.innerHTML = '<p class="loading">Carregando...</p>';

    try {
        const response = await fetch(`${API_URL}/api/fotos/historico?limite=50`, {
            headers: {
                'Authorization': `Bearer ${state.token}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao carregar histórico');
        }

        const data = await response.json();

        if (data.fotos.length === 0) {
            elements.historyList.innerHTML = '<p class="loading">Nenhuma foto encontrada</p>';
            return;
        }

        elements.historyList.innerHTML = data.fotos.map(foto => `
            <div class="history-item">
                <img src="${API_URL}/api/fotos/imagem/${foto.nomeArquivo}?token=${state.token}"
                     alt="${foto.nomeArquivo}"
                     style="width: 100%; max-width: 400px; border-radius: 8px; margin-bottom: 10px;"
                     onerror="this.style.display='none'">
                <h3>${foto.nomeArquivo}</h3>
                <p><strong>Material:</strong> ${foto.material || 'N/A'}</p>
                <p><strong>Bloco:</strong> ${foto.bloco || foto.lote || 'N/A'} | <strong>Chapa:</strong> ${foto.chapa}</p>
                <p>${foto.espessura ? `<strong>Espessura:</strong> ${foto.espessura}mm` : ''}</p>
                <small>Enviado por ${foto.usuario} em ${formatDate(foto.dataUpload)}</small>
                <br>
                <a href="${API_URL}/api/fotos/imagem/${foto.nomeArquivo}?token=${state.token}"
                   target="_blank"
                   style="color: #2563eb; text-decoration: none; font-size: 14px;">
                   🔗 Abrir imagem em nova aba
                </a>
            </div>
        `).join('');

    } catch (error) {
        elements.historyList.innerHTML = `<p class="loading">${error.message}</p>`;
    }
}

// ========== UTILIDADES ==========
function showMessage(message, type) {
    elements.uploadMessage.textContent = message;
    elements.uploadMessage.className = `message ${type}`;
    elements.uploadMessage.classList.remove('hidden');

    setTimeout(() => {
        elements.uploadMessage.classList.add('hidden');
    }, 5000);
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('pt-BR');
}

// ========== CROP DE IMAGEM ==========
function openCropScreen(file) {
    const reader = new FileReader();

    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            state.cropData.image = img;
            initializeCropCanvas();
            showCropScreen();
        };
        img.src = e.target.result;
    };

    reader.readAsDataURL(file);
}

function showCropScreen() {
    elements.mainScreen.classList.remove('active');
    elements.cropScreen.classList.add('active');
}

function initializeCropCanvas() {
    const canvas = elements.cropCanvas;
    const ctx = canvas.getContext('2d');
    const img = state.cropData.image;

    // Calcula escala para caber no canvas mantendo proporção
    const maxWidth = window.innerWidth - 64; // Margem
    const maxHeight = window.innerHeight * 0.6;

    let width = img.width;
    let height = img.height;

    if (width > maxWidth) {
        height = (maxWidth / width) * height;
        width = maxWidth;
    }

    if (height > maxHeight) {
        width = (maxHeight / height) * width;
        height = maxHeight;
    }

    canvas.width = width;
    canvas.height = height;

    // Calcula escala para conversão de coordenadas canvas -> imagem original
    state.cropData.scale = img.width / width;

    // Desenha imagem
    ctx.drawImage(img, 0, 0, width, height);

    // Define seleção inicial (imagem inteira)
    state.cropData.startX = 0;
    state.cropData.startY = 0;
    state.cropData.endX = width;
    state.cropData.endY = height;

    drawCropOverlay();
}

function getCanvasCoords(e) {
    const canvas = elements.cropCanvas;
    const rect = canvas.getBoundingClientRect();

    let clientX, clientY;

    if (e.touches && e.touches[0]) {
        clientX = e.touches[0].clientX;
        clientY = e.touches[0].clientY;
    } else {
        clientX = e.clientX;
        clientY = e.clientY;
    }

    // Calcula a posição relativa ao canvas
    const x = clientX - rect.left;
    const y = clientY - rect.top;

    // Converte coordenadas CSS para coordenadas do canvas
    // (necessário quando canvas é redimensionado via CSS)
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;

    return {
        x: Math.max(0, Math.min(canvas.width, x * scaleX)),
        y: Math.max(0, Math.min(canvas.height, y * scaleY))
    };
}

function startCrop(e) {
    e.preventDefault();
    const coords = getCanvasCoords(e);

    state.cropData.isDragging = true;
    state.cropData.startX = coords.x;
    state.cropData.startY = coords.y;
    state.cropData.endX = coords.x;
    state.cropData.endY = coords.y;
}

function updateCrop(e) {
    if (!state.cropData.isDragging) return;

    e.preventDefault();
    const coords = getCanvasCoords(e);

    state.cropData.endX = coords.x;
    state.cropData.endY = coords.y;

    drawCropOverlay();
}

function endCrop(e) {
    if (!state.cropData.isDragging) return;

    e.preventDefault();
    state.cropData.isDragging = false;

    // Garante que a área tenha pelo menos 50x50 pixels
    const width = Math.abs(state.cropData.endX - state.cropData.startX);
    const height = Math.abs(state.cropData.endY - state.cropData.startY);

    if (width < 50 || height < 50) {
        resetCrop();
    }
}

function handleTouchStart(e) {
    startCrop(e);
}

function handleTouchMove(e) {
    updateCrop(e);
}

function drawCropOverlay() {
    const canvas = elements.cropCanvas;
    const ctx = canvas.getContext('2d');
    const img = state.cropData.image;

    // Calcula dimensões atuais do canvas
    const canvasWidth = canvas.width;
    const canvasHeight = canvas.height;

    // Calcula retângulo de seleção
    const x = Math.min(state.cropData.startX, state.cropData.endX);
    const y = Math.min(state.cropData.startY, state.cropData.endY);
    const width = Math.abs(state.cropData.endX - state.cropData.startX);
    const height = Math.abs(state.cropData.endY - state.cropData.startY);

    // Limpa canvas
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    // Desenha imagem completa
    ctx.drawImage(img, 0, 0, canvasWidth, canvasHeight);

    // Desenha overlay escurecido em 4 retângulos ao redor da seleção
    ctx.fillStyle = 'rgba(0, 0, 0, 0.6)';

    // Topo
    ctx.fillRect(0, 0, canvasWidth, y);

    // Esquerda (da altura da seleção)
    ctx.fillRect(0, y, x, height);

    // Direita (da altura da seleção)
    ctx.fillRect(x + width, y, canvasWidth - (x + width), height);

    // Baixo
    ctx.fillRect(0, y + height, canvasWidth, canvasHeight - (y + height));

    // Desenha borda da seleção (tracejada branca)
    ctx.strokeStyle = '#FFFFFF';
    ctx.lineWidth = 2;
    ctx.setLineDash([8, 4]);
    ctx.strokeRect(x, y, width, height);
    ctx.setLineDash([]);

    // Desenha cantos brancos com sombra
    const cornerSize = 25;
    const cornerThickness = 4;
    ctx.fillStyle = '#FFFFFF';
    ctx.shadowColor = 'rgba(0, 0, 0, 0.8)';
    ctx.shadowBlur = 3;

    // Canto superior esquerdo
    ctx.fillRect(x, y, cornerSize, cornerThickness);
    ctx.fillRect(x, y, cornerThickness, cornerSize);

    // Canto superior direito
    ctx.fillRect(x + width - cornerSize, y, cornerSize, cornerThickness);
    ctx.fillRect(x + width - cornerThickness, y, cornerThickness, cornerSize);

    // Canto inferior esquerdo
    ctx.fillRect(x, y + height - cornerThickness, cornerSize, cornerThickness);
    ctx.fillRect(x, y + height - cornerSize, cornerThickness, cornerSize);

    // Canto inferior direito
    ctx.fillRect(x + width - cornerSize, y + height - cornerThickness, cornerSize, cornerThickness);
    ctx.fillRect(x + width - cornerThickness, y + height - cornerSize, cornerThickness, cornerSize);

    // Remove sombra
    ctx.shadowColor = 'transparent';
    ctx.shadowBlur = 0;
}

function resetCrop() {
    const canvas = elements.cropCanvas;

    state.cropData.startX = 0;
    state.cropData.startY = 0;
    state.cropData.endX = canvas.width;
    state.cropData.endY = canvas.height;

    drawCropOverlay();
}

function confirmCrop() {
    const canvas = elements.cropCanvas;
    const img = state.cropData.image;

    // Calcula coordenadas na imagem original
    const x = Math.min(state.cropData.startX, state.cropData.endX) * state.cropData.scale;
    const y = Math.min(state.cropData.startY, state.cropData.endY) * state.cropData.scale;
    const width = Math.abs(state.cropData.endX - state.cropData.startX) * state.cropData.scale;
    const height = Math.abs(state.cropData.endY - state.cropData.startY) * state.cropData.scale;

    // Cria canvas temporário para crop
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = width;
    tempCanvas.height = height;
    const tempCtx = tempCanvas.getContext('2d');

    // Desenha área cortada
    tempCtx.drawImage(img, x, y, width, height, 0, 0, width, height);

    // Converte para blob e cria arquivo
    tempCanvas.toBlob((blob) => {
        const file = new File([blob], 'cropped.jpg', { type: 'image/jpeg' });

        // Se for modo mockup, gera o mockup
        if (state.mockupMode) {
            gerarMockup(file);
        } else {
            compressAndPreviewImage(file);
            // Mostra botão mockup pois já tem imagem disponível
            elements.mockupBtn.classList.remove('hidden');
            // Mostra botão de reset pois a imagem foi modificada
            elements.resetImageBtn.classList.remove('hidden');
            showMainScreen();
        }
    }, 'image/jpeg', 0.9);
}

function cancelCrop() {
    // Limpa input file para permitir selecionar a mesma imagem novamente
    elements.fileInput.value = '';
    state.mockupMode = false;
    showMainScreen();
}

function abrirCropParaAjuste() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma imagem disponível para ajustar', 'error');
        return;
    }

    // Carrega imagem original no crop (modo ajuste normal)
    state.mockupMode = false;
    state.cropData.image = state.originalPhoto;
    initializeCropCanvas();
    showCropScreen();
}

function resetToOriginalImage() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma imagem original disponível', 'error');
        return;
    }

    // Converte a imagem original de volta para File e exibe
    const canvas = document.createElement('canvas');
    canvas.width = state.originalPhoto.width;
    canvas.height = state.originalPhoto.height;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(state.originalPhoto, 0, 0);

    canvas.toBlob((blob) => {
        const file = new File([blob], 'original.jpg', { type: 'image/jpeg' });
        compressAndPreviewImage(file);
        // Oculta botão de reset pois voltou ao original
        elements.resetImageBtn.classList.add('hidden');
        showMessage('Imagem original restaurada', 'success');
    }, 'image/jpeg', 0.9);
}

// ========== MOCKUP DE CAVALETES ==========
function startMockupFlow() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma foto disponível para mockup', 'error');
        return;
    }

    // Mostra tela de configuração
    showScreen(elements.mockupConfigScreen);
}

function abrirCropParaMockup() {
    // Captura configuração selecionada
    const tipoSelecionado = document.querySelector('input[name="tipoCavalete"]:checked');
    const fundoSelecionado = document.querySelector('input[name="fundoCavalete"]:checked');

    state.mockupConfig.tipo = tipoSelecionado ? tipoSelecionado.value : 'simples';
    state.mockupConfig.fundo = fundoSelecionado ? fundoSelecionado.value : 'claro';

    // Ativa modo mockup
    state.mockupMode = true;

    // Carrega imagem original no crop
    state.cropData.image = state.originalPhoto;
    initializeCropCanvas();
    showCropScreen();
}

async function gerarMockup(imagemCropada) {
    try {
        elements.uploadProgress.classList.remove('hidden');

        const formData = new FormData();
        formData.append('ImagemCropada', imagemCropada);
        formData.append('TipoCavalete', state.mockupConfig.tipo);
        formData.append('Fundo', state.mockupConfig.fundo);

        const response = await fetch(`${API_URL}/api/mockup/gerar`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.mensagem || 'Erro ao gerar mockup');
        }

        // Exibe resultado (ATENÇÃO: backend retorna com C maiúsculo)
        if (data.CaminhosGerados && data.CaminhosGerados.length > 0) {
            const mockupUrl = `${API_URL}/uploads/${data.CaminhosGerados[0]}`;
            elements.mockupImage.src = mockupUrl;
            showScreen(elements.mockupResultScreen);
            showMockupMessage(data.Mensagem, 'success');
        } else {
            throw new Error('Nenhum mockup foi gerado');
        }

        // Reseta modo mockup
        state.mockupMode = false;

    } catch (error) {
        showMockupMessage(error.message, 'error');
        state.mockupMode = false;
        showMainScreen();
    } finally {
        elements.uploadProgress.classList.add('hidden');
    }
}

function downloadMockup() {
    const link = document.createElement('a');
    link.href = elements.mockupImage.src;
    link.download = `mockup_${Date.now()}.jpg`;
    link.click();
}

function showMockupMessage(message, type) {
    elements.mockupMessage.textContent = message;
    elements.mockupMessage.className = `message ${type}`;
    elements.mockupMessage.classList.remove('hidden');

    setTimeout(() => {
        elements.mockupMessage.classList.add('hidden');
    }, 5000);
}

// ========== SERVICE WORKER (para PWA futuro) ==========
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        // Descomentado quando houver service-worker.js
        // navigator.serviceWorker.register('/service-worker.js');
    });
}
