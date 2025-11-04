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
    },
    // Estado específico para bancadas (countertops)
    countertopState: {
        croppedImage: null,    // Imagem cortada (reutilizável)
        selectedType: null,     // 'bancada1' ou 'bancada2'
        flip: false            // Opção global de flip
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
    userInitials: document.getElementById('userInitials'),
    userMenuBtn: document.getElementById('userMenuBtn'),
    userMenuDropdown: document.getElementById('userMenuDropdown'),
    dropdownUsername: document.getElementById('dropdownUsername'),
    dropdownEmail: document.getElementById('dropdownEmail'),
    logoutBtn: document.getElementById('logoutBtn'),
    historyBtn: document.getElementById('historyBtn'),
    backBtn: document.getElementById('backBtn'),
    historyList: document.getElementById('historyList'),
    cropCanvas: document.getElementById('cropCanvas'),
    cancelCropBtn: document.getElementById('cancelCropBtn'),
    resetCropBtn: document.getElementById('resetCropBtn'),
    mockupBtn: document.getElementById('mockupBtn'),
    nichoBtn: document.getElementById('nichoBtn'),
    photoIndicator: document.getElementById('photoIndicator'),
    cancelMockupBtn: document.getElementById('cancelMockupBtn'),
    nichoConfigScreen: document.getElementById('nichoConfigScreen'),
    cancelNichoBtn: document.getElementById('cancelNichoBtn'),
    continuarCropNichoBtn: document.getElementById('continuarCropNichoBtn'),
    continuarCropMockupBtn: document.getElementById('continuarCropMockupBtn'),
    countertopsBtn: document.getElementById('countertopsBtn'),
    countertopSelectionScreen: document.getElementById('countertopSelectionScreen'),
    cancelCountertopSelectionBtn: document.getElementById('cancelCountertopSelectionBtn'),
    flipCountertop: document.getElementById('flipCountertop'),
    backToMainBtn: document.getElementById('backToMainBtn'),
    downloadAllMockupsBtn: document.getElementById('downloadAllMockupsBtn'),
    newMockupBtn: document.getElementById('newMockupBtn'),
    mockupsGallery: document.getElementById('mockupsGallery'),
    mockupMessage: document.getElementById('mockupMessage'),
    cropInfo: document.getElementById('cropInfo'),
    cropInfoArea: document.getElementById('cropInfoArea'),
    cropInfoMP: document.getElementById('cropInfoMP'),
    cropInfoSize: document.getElementById('cropInfoSize'),
    changePasswordBtn: document.getElementById('changePasswordBtn'),
    manageUsersBtn: document.getElementById('manageUsersBtn'),
    changePasswordScreen: document.getElementById('changePasswordScreen'),
    changePasswordForm: document.getElementById('changePasswordForm'),
    changePasswordMessage: document.getElementById('changePasswordMessage'),
    backFromPasswordBtn: document.getElementById('backFromPasswordBtn'),
    usersScreen: document.getElementById('usersScreen'),
    usersList: document.getElementById('usersList'),
    backFromUsersBtn: document.getElementById('backFromUsersBtn'),
    addUserBtn: document.getElementById('addUserBtn'),
    addUserScreen: document.getElementById('addUserScreen'),
    addUserForm: document.getElementById('addUserForm'),
    addUserMessage: document.getElementById('addUserMessage'),
    backFromAddUserBtn: document.getElementById('backFromAddUserBtn')
};

// ========== AUTO-RENOVAÇÃO DE TOKEN ==========

/**
 * Decodifica JWT token para extrair payload
 */
function decodeToken(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (error) {
        console.error('Erro ao decodificar token:', error);
        return null;
    }
}

/**
 * Verifica se token expira em menos de 1 hora
 */
function tokenExpiresInLessThanOneHour(token) {
    const decoded = decodeToken(token);
    if (!decoded || !decoded.exp) {
        return false;
    }

    const expirationTime = decoded.exp * 1000; // Converte para milissegundos
    const currentTime = Date.now();
    const oneHour = 60 * 60 * 1000; // 1 hora em milissegundos

    return (expirationTime - currentTime) < oneHour;
}

/**
 * Renova token JWT automaticamente
 */
async function renovarTokenAutomaticamente() {
    if (!state.token) {
        return;
    }

    try {
        const response = await fetch(`${API_URL}/api/auth/refresh`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            state.token = data.token;
            localStorage.setItem('token', data.token);
            console.log('✅ Token renovado automaticamente');
        } else if (response.status === 401) {
            // Token expirado ou inválido - redireciona para login
            console.log('⚠️ Token expirado, redirecionando para login...');
            logout();
        }
    } catch (error) {
        console.error('Erro ao renovar token:', error);
    }
}

/**
 * Verifica token periodicamente e renova se necessário
 */
function iniciarVerificacaoToken() {
    // Verifica a cada 30 minutos
    setInterval(() => {
        if (state.token && tokenExpiresInLessThanOneHour(state.token)) {
            console.log('⏰ Token expirando em menos de 1 hora, renovando...');
            renovarTokenAutomaticamente();
        }
    }, 30 * 60 * 1000); // 30 minutos

    // Também verifica imediatamente ao iniciar
    if (state.token && tokenExpiresInLessThanOneHour(state.token)) {
        console.log('⏰ Token expirando em menos de 1 hora, renovando...');
        renovarTokenAutomaticamente();
    }
}

// ========== INICIALIZAÇÃO ==========
document.addEventListener('DOMContentLoaded', () => {
    if (state.token) {
        showMainScreen();
        iniciarVerificacaoToken();
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

    // Canvas mouse/touch events
    elements.cropCanvas.addEventListener('mousedown', startCrop);
    elements.cropCanvas.addEventListener('mousemove', updateCrop);
    elements.cropCanvas.addEventListener('mouseup', endCrop);
    elements.cropCanvas.addEventListener('touchstart', handleTouchStart, { passive: false });
    elements.cropCanvas.addEventListener('touchmove', handleTouchMove, { passive: false });
    elements.cropCanvas.addEventListener('touchend', endCrop);

    // Mockup event listeners
    elements.mockupBtn.addEventListener('click', startMockupFlow);
    elements.nichoBtn.addEventListener('click', startNichoFlow);
    elements.countertopsBtn.addEventListener('click', startCountertopFlow);
    elements.cancelMockupBtn.addEventListener('click', () => showMainScreen());
    elements.cancelNichoBtn.addEventListener('click', () => showMainScreen());
    elements.cancelCountertopSelectionBtn.addEventListener('click', () => showMainScreen());
    elements.continuarCropMockupBtn.addEventListener('click', abrirCropParaMockup);
    elements.continuarCropNichoBtn.addEventListener('click', abrirCropParaNicho);
    elements.backToMainBtn.addEventListener('click', () => showMainScreen());
    elements.newMockupBtn.addEventListener('click', startMockupFlow);
    elements.downloadAllMockupsBtn.addEventListener('click', downloadAllMockups);

    // Event delegation para botões de download individual
    elements.mockupsGallery.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-download-single')) {
            const url = e.target.dataset.url;
            const nome = e.target.dataset.nome;
            downloadMockup(url, nome);
        }
    });

    // Event delegation para seleção de countertop
    document.querySelectorAll('.btn-select-countertop').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const card = e.target.closest('.countertop-card');
            const type = card.dataset.type;
            selectCountertopAndGenerate(type);
        });
    });

    // User menu dropdown
    elements.userMenuBtn.addEventListener('click', toggleUserMenu);

    // Fecha dropdown ao clicar fora
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.user-menu')) {
            elements.userMenuDropdown.classList.add('hidden');
        }
    });

    // Gerenciamento de usuários
    elements.changePasswordBtn.addEventListener('click', () => {
        elements.userMenuDropdown.classList.add('hidden');
        showChangePasswordScreen();
    });
    elements.manageUsersBtn.addEventListener('click', () => {
        elements.userMenuDropdown.classList.add('hidden');
        showUsersScreen();
    });
    elements.backFromPasswordBtn.addEventListener('click', showMainScreen);
    elements.backFromUsersBtn.addEventListener('click', showMainScreen);
    elements.backFromAddUserBtn.addEventListener('click', showUsersScreen);
    elements.changePasswordForm.addEventListener('submit', handleChangePassword);
    elements.addUserBtn.addEventListener('click', showAddUserScreen);
    elements.addUserForm.addEventListener('submit', handleAddUser);

    // Event delegation para botões de gerenciar usuários
    elements.usersList.addEventListener('click', async (e) => {
        if (e.target.classList.contains('btn-deactivate-user')) {
            const userId = e.target.dataset.userId;
            await deactivateUser(userId);
        } else if (e.target.classList.contains('btn-reactivate-user')) {
            const userId = e.target.dataset.userId;
            await reactivateUser(userId);
        }
    });
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
        iniciarVerificacaoToken(); // Inicia verificação automática do token
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

    // Atualiza informações do usuário
    elements.userDisplay.textContent = state.username;
    elements.dropdownUsername.textContent = state.username;
    elements.dropdownEmail.textContent = `@${state.username}`;

    // Gera iniciais do usuário
    const initials = state.username.substring(0, 2).toUpperCase();
    elements.userInitials.textContent = initials;

    // Mostra botão de gerenciar usuários apenas para admin
    if (state.username === 'admin') {
        elements.manageUsersBtn.classList.remove('hidden');
    } else {
        elements.manageUsersBtn.classList.add('hidden');
    }

    await loadMaterials();
}

/**
 * Toggle do menu dropdown do usuário
 */
function toggleUserMenu(e) {
    e.stopPropagation();
    elements.userMenuDropdown.classList.toggle('hidden');
}

async function showHistoryScreen() {
    showScreen(elements.historyScreen);
    await loadHistory();
}

function showChangePasswordScreen() {
    showScreen(elements.changePasswordScreen);
    elements.changePasswordForm.reset();
    elements.changePasswordMessage.classList.add('hidden');
}

async function showUsersScreen() {
    if (state.username !== 'admin') {
        alert('Acesso negado. Apenas admin pode gerenciar usuários.');
        return;
    }
    showScreen(elements.usersScreen);
    await loadUsers();
}

function showAddUserScreen() {
    showScreen(elements.addUserScreen);
    elements.addUserForm.reset();
    elements.addUserMessage.classList.add('hidden');
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
            // Usa imagem original sem redimensionamento
            const canvas = document.createElement('canvas');
            let width = img.width;
            let height = img.height;

            canvas.width = width;
            canvas.height = height;

            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, width, height);

            canvas.toBlob((blob) => {
                state.currentPhotoFile = new File([blob], file.name, {
                    type: 'image/jpeg',
                    lastModified: Date.now()
                });

                // Exibe preview (qualidade reduzida apenas para preview)
                elements.previewImage.src = canvas.toDataURL('image/jpeg', 0.85);
                elements.photoPreview.classList.remove('hidden');
                elements.submitBtn.disabled = false;

            }, 'image/jpeg', 0.95);
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
    elements.nichoBtn.classList.add('hidden');
    elements.countertopsBtn.classList.add('hidden');
    elements.photoIndicator.classList.add('hidden');
}

function clearPhotoState() {
    // Limpa apenas estado atual (para preparar nova foto)
    state.currentPhotoFile = null;
    elements.previewImage.src = '';
    elements.photoPreview.classList.add('hidden');
    elements.submitBtn.disabled = true;
    elements.mockupBtn.classList.add('hidden');
    elements.nichoBtn.classList.add('hidden');
    elements.countertopsBtn.classList.add('hidden');
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
    formData.append('Bloco', document.getElementById('bloco').value.toUpperCase());
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

        // Mostra botões de mockup (permanecem visíveis)
        elements.mockupBtn.classList.remove('hidden');
        elements.nichoBtn.classList.remove('hidden');
        elements.countertopsBtn.classList.remove('hidden');

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
    elements.cropInfo.classList.add('hidden'); // Esconde até haver uma seleção
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
    state.cropData.scaleX = img.width / width;
    state.cropData.scaleY = img.height / height;

    console.log('=== DEBUG INITIALIZE CROP ===');
    console.log('Imagem original:', img.width, 'x', img.height);
    console.log('Canvas final:', width, 'x', height);
    console.log('ScaleX calculado:', state.cropData.scaleX);
    console.log('ScaleY calculado:', state.cropData.scaleY);
    console.log('=============================');

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
        return;
    }

    // Executa o crop automaticamente após pequeno delay para feedback visual
    setTimeout(() => {
        confirmCrop();
    }, 300);
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

    // Calcula tamanho estimado do arquivo cropado
    const cropWidthReal = width * state.cropData.scaleX;
    const cropHeightReal = height * state.cropData.scaleY;
    const totalPixels = cropWidthReal * cropHeightReal;

    // Fórmula: bytes = pixels * bytesPerPixel (0.25 para JPEG Q95)
    const estimatedBytes = totalPixels * 0.25;
    const estimatedMB = (estimatedBytes / 1048576).toFixed(2);
    const estimatedMP = (totalPixels / 1000000).toFixed(1);

    // Atualiza os elementos HTML com as informações do crop
    elements.cropInfoArea.textContent = `${Math.round(cropWidthReal)} x ${Math.round(cropHeightReal)} px`;
    elements.cropInfoMP.textContent = `${estimatedMP} MP`;
    elements.cropInfoSize.textContent = `${estimatedMB} MB`;
    elements.cropInfo.classList.remove('hidden');

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

    console.log('=== DEBUG CROP ===');
    console.log('Imagem original:', img.width, 'x', img.height);
    console.log('Canvas:', canvas.width, 'x', canvas.height);
    console.log('ScaleX:', state.cropData.scaleX, 'ScaleY:', state.cropData.scaleY);
    console.log('Seleção no canvas - startX:', state.cropData.startX, 'startY:', state.cropData.startY);
    console.log('Seleção no canvas - endX:', state.cropData.endX, 'endY:', state.cropData.endY);
    console.log('Largura seleção canvas:', Math.abs(state.cropData.endX - state.cropData.startX));
    console.log('Altura seleção canvas:', Math.abs(state.cropData.endY - state.cropData.startY));

    // Calcula coordenadas na imagem original usando escalas separadas para X e Y
    const x = Math.min(state.cropData.startX, state.cropData.endX) * state.cropData.scaleX;
    const y = Math.min(state.cropData.startY, state.cropData.endY) * state.cropData.scaleY;
    const width = Math.abs(state.cropData.endX - state.cropData.startX) * state.cropData.scaleX;
    const height = Math.abs(state.cropData.endY - state.cropData.startY) * state.cropData.scaleY;

    console.log('Crop na imagem original - X:', x, 'Y:', y);
    console.log('Crop na imagem original - Width:', width, 'Height:', height);
    console.log('==================');

    // Cria canvas temporário para crop
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = width;
    tempCanvas.height = height;
    const tempCtx = tempCanvas.getContext('2d');

    // Desenha área cortada
    tempCtx.drawImage(img, x, y, width, height, 0, 0, width, height);

    // Converte para blob e cria arquivo (qualidade 95%)
    tempCanvas.toBlob((blob) => {
        const file = new File([blob], 'cropped.jpg', { type: 'image/jpeg' });

        // Verifica se é flow de countertop (crop primeiro, depois escolha)
        if (state.countertopState.croppedImage !== null || state.mockupConfig.tipo === 'countertop') {
            // Flow de countertop: salva crop e mostra tela de seleção
            showCountertopSelection(file);
        } else if (state.mockupMode) {
            // Outros mockups (cavalete, nicho): gera diretamente
            gerarMockup(file);
        } else {
            // Modo ajuste normal
            compressAndPreviewImage(file);
            // Mostra botões mockup pois já tem imagem disponível
            elements.mockupBtn.classList.remove('hidden');
            elements.nichoBtn.classList.remove('hidden');
            elements.countertopsBtn.classList.remove('hidden');
            // Mostra botão de reset pois a imagem foi modificada
            elements.resetImageBtn.classList.remove('hidden');
            showMainScreen();
        }
    }, 'image/jpeg', 0.95);
}

function cancelCrop() {
    // Limpa input file para permitir selecionar a mesma imagem novamente
    elements.fileInput.value = '';
    state.mockupMode = false;
    elements.cropInfo.classList.add('hidden');
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
    }, 'image/jpeg', 0.95);
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
    // Captura configuração de fundo
    const fundoSelecionado = document.querySelector('input[name="fundoCavalete"]:checked');
    state.mockupConfig.fundo = fundoSelecionado ? fundoSelecionado.value : 'claro';
    state.mockupConfig.tipo = 'cavalete'; // Define tipo como cavalete

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

        // Verifica o tipo de mockup
        const isNicho = state.mockupConfig.tipo === 'nicho1';
        const isBancada1 = state.mockupConfig.tipo === 'bancada1';

        const formData = new FormData();
        formData.append((isNicho || isBancada1) ? 'imagem' : 'ImagemCropada', imagemCropada);

        if (isNicho) {
            // Parâmetros específicos do nicho
            formData.append('fundoEscuro', state.mockupConfig.fundo === 'escuro');
            formData.append('incluirShampoo', state.mockupConfig.incluirShampoo || false);
            formData.append('incluirSabonete', state.mockupConfig.incluirSabonete || false);
        } else if (isBancada1) {
            // Parâmetros específicos da bancada1
            formData.append('flip', state.mockupConfig.flip || false);
        } else {
            // Parâmetros do cavalete (simples)
            formData.append('TipoCavalete', 'simples'); // Sempre simples por enquanto
            formData.append('Fundo', state.mockupConfig.fundo);
        }

        const endpoint = isNicho ? '/api/mockup/nicho1' : (isBancada1 ? '/api/mockup/bancada1' : '/api/mockup/gerar');

        const response = await fetch(`${API_URL}${endpoint}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        const data = await response.json();

        console.log('=== DEBUG MOCKUP RESPONSE ===');
        console.log('Response OK:', response.ok);
        console.log('Data:', data);

        if (!response.ok) {
            throw new Error(data.mensagem || 'Erro ao gerar mockup');
        }

        // Exibe resultado (backend retorna diferente para nicho, bancada1 e cavalete)
        const caminhos = data.caminhosGerados || data.mockups;

        if (caminhos && caminhos.length > 0) {
            const gallery = document.getElementById('mockupsGallery');
            gallery.innerHTML = ''; // Limpa galeria

            // Labels diferentes para cada tipo
            let labels;
            if (isNicho) {
                labels = ['Nicho - Versão Normal', 'Nicho - Rotacionado 180°'];
            } else if (isBancada1) {
                labels = ['Bancada #1 - Normal', 'Bancada #1 - Rotacionado 180°'];
            } else {
                labels = [
                    'Cavalete Duplo - Original/Espelho',
                    'Cavalete Duplo - Espelho/Original',
                    'Cavalete Simples'
                ];
            }

            caminhos.forEach((caminho, index) => {
                // Para nicho e bancada1, caminho já vem completo; para cavalete, precisa montar
                const mockupUrl = (isNicho || isBancada1) ? `${API_URL}${caminho}` : `${API_URL}/uploads/${caminho}`;

                const mockupItem = document.createElement('div');
                mockupItem.className = 'mockup-item';
                mockupItem.innerHTML = `
                    <h3>${labels[index] || `Mockup ${index + 1}`}</h3>
                    <img src="${mockupUrl}" alt="${labels[index]}">
                    <button class="btn btn-secondary btn-download-single" data-url="${mockupUrl}" data-nome="${caminho}">
                        ⬇️ Baixar
                    </button>
                `;
                gallery.appendChild(mockupItem);
            });

            // Salva URLs para download em massa
            state.mockupUrls = (isNicho || isBancada1)
                ? caminhos.map(c => `${API_URL}${c}`)
                : caminhos.map(c => `${API_URL}/uploads/${c}`);

            showScreen(elements.mockupResultScreen);
            showMockupMessage(data.mensagem, 'success');
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

function downloadMockup(url, nome) {
    const link = document.createElement('a');
    link.href = url;
    link.download = nome || `mockup_${Date.now()}.jpg`;
    link.click();
}

function downloadAllMockups() {
    if (!state.mockupUrls || state.mockupUrls.length === 0) {
        showMockupMessage('Nenhum mockup disponível para download', 'error');
        return;
    }

    state.mockupUrls.forEach((url, index) => {
        setTimeout(() => {
            const link = document.createElement('a');
            link.href = url;
            link.download = `mockup_${index + 1}_${Date.now()}.jpg`;
            link.click();
        }, index * 500); // Delay de 500ms entre cada download
    });

    showMockupMessage('Baixando todos os mockups...', 'success');
}

function showMockupMessage(message, type) {
    elements.mockupMessage.textContent = message;
    elements.mockupMessage.className = `message ${type}`;
    elements.mockupMessage.classList.remove('hidden');

    setTimeout(() => {
        elements.mockupMessage.classList.add('hidden');
    }, 5000);
}

// ========== NICHO MOCKUP FLOW ==========
function startNichoFlow() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma foto disponível para mockup de nicho', 'error');
        return;
    }

    // Mostra tela de configuração
    showScreen(elements.nichoConfigScreen);
}

function abrirCropParaNicho() {
    // Captura configurações do nicho
    const fundoSelecionado = document.querySelector('input[name="fundoNicho"]:checked');
    const incluirShampoo = document.getElementById('incluirShampoo').checked;
    const incluirSabonete = document.getElementById('incluirSabonete').checked;

    state.mockupConfig.fundo = fundoSelecionado ? fundoSelecionado.value : 'claro';
    state.mockupConfig.tipo = 'nicho1'; // Identifica que é nicho
    state.mockupConfig.incluirShampoo = incluirShampoo;
    state.mockupConfig.incluirSabonete = incluirSabonete;

    // Ativa modo mockup
    state.mockupMode = true;

    // Carrega imagem original no crop
    state.cropData.image = state.originalPhoto;
    initializeCropCanvas();

    // Mostra tela de crop
    showScreen(elements.cropScreen);
}

// ========== BANCADA1 MOCKUP FLOW ==========
// ========== UNIFIED COUNTERTOP FLOW ==========

/**
 * Passo 1: Inicia o flow de countertop - vai direto para o crop
 */
function startCountertopFlow() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma foto disponível para mockup de bancada', 'error');
        return;
    }

    // Limpa estado anterior de countertop
    state.countertopState.croppedImage = null;
    state.countertopState.selectedType = null;
    state.countertopState.flip = false;

    // Marca que estamos no flow de countertop (para confirmCrop saber)
    state.mockupConfig.tipo = 'countertop';
    state.mockupMode = false; // Não queremos gerar ainda, só fazer crop

    // Carrega imagem original no crop
    state.cropData.image = state.originalPhoto;
    initializeCropCanvas();

    // Vai direto para crop
    showScreen(elements.cropScreen);
}

/**
 * Passo 2: Após crop, salva imagem cortada e mostra tela de seleção
 */
function showCountertopSelection(croppedImageBlob) {
    // Salva crop para reutilização
    state.countertopState.croppedImage = croppedImageBlob;

    // Mostra tela de seleção
    showScreen(elements.countertopSelectionScreen);

    // Reset checkbox de flip
    if (elements.flipCountertop) {
        elements.flipCountertop.checked = false;
    }
}

/**
 * Passo 3: Usuário selecionou tipo de bancada e clicou em gerar
 */
async function selectCountertopAndGenerate(type) {
    if (!state.countertopState.croppedImage) {
        showMessage('Erro: Imagem cortada não encontrada', 'error');
        return;
    }

    // Salva seleção
    state.countertopState.selectedType = type;
    state.countertopState.flip = elements.flipCountertop ? elements.flipCountertop.checked : false;

    // Gera mockup
    await generateCountertopMockup();
}

/**
 * Passo 4: Gera o mockup da bancada selecionada
 */
async function generateCountertopMockup() {
    try {
        elements.uploadProgress.classList.remove('hidden');

        const formData = new FormData();
        formData.append('imagem', state.countertopState.croppedImage, 'cropped.jpg');
        formData.append('flip', state.countertopState.flip);

        const endpoint = state.countertopState.selectedType === 'bancada1'
            ? '/api/mockup/bancada1'
            : '/api/mockup/bancada2';

        const response = await fetch(`${API_URL}${endpoint}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.mensagem || 'Erro ao gerar mockup de bancada');
        }

        // Exibe resultado
        displayCountertopResults(data);

    } catch (error) {
        console.error('Erro ao gerar bancada:', error);
        showMockupMessage(error.message, 'error');
        showMainScreen();
    } finally {
        elements.uploadProgress.classList.add('hidden');
        // Limpa flag de countertop
        state.mockupConfig.tipo = 'simples';
    }
}

/**
 * Passo 5: Exibe resultados com opção de tentar outra bancada
 */
function displayCountertopResults(data) {
    const caminhos = data.mockups;

    if (!caminhos || caminhos.length === 0) {
        showMessage('Nenhum mockup foi gerado', 'error');
        return;
    }

    const gallery = elements.mockupsGallery;
    gallery.innerHTML = '';

    const labels = state.countertopState.selectedType === 'bancada1'
        ? ['Bancada #1 - Normal', 'Bancada #1 - Rotacionado 180°']
        : ['Bancada #2 - Normal', 'Bancada #2 - Rotacionado 180°'];

    caminhos.forEach((caminho, index) => {
        const mockupUrl = `${API_URL}${caminho}`;
        const mockupItem = document.createElement('div');
        mockupItem.className = 'mockup-item';
        mockupItem.innerHTML = `
            <h3>${labels[index]}</h3>
            <img src="${mockupUrl}" alt="${labels[index]}">
            <button class="btn btn-secondary btn-download-single" data-url="${mockupUrl}" data-nome="${caminho}">
                ⬇️ Baixar
            </button>
        `;
        gallery.appendChild(mockupItem);
    });

    // Salva URLs para download em lote
    state.mockupUrls = caminhos.map(c => `${API_URL}${c}`);

    // Modifica botão "Novo Mockup" para permitir tentar outra bancada
    elements.newMockupBtn.textContent = '🔄 Tentar Outra Bancada (Mesmo Recorte)';
    elements.newMockupBtn.onclick = () => {
        // Retorna para seleção com o mesmo crop
        showScreen(elements.countertopSelectionScreen);
    };

    // Mostra tela de resultado
    showScreen(elements.mockupResultScreen);
    showMockupMessage(data.mensagem, 'success');
}

// ========== GERENCIAMENTO DE USUÁRIOS ==========

/**
 * Troca senha do usuário logado
 */
async function handleChangePassword(e) {
    e.preventDefault();

    const senhaAtual = document.getElementById('senhaAtual').value;
    const novaSenha = document.getElementById('novaSenha').value;
    const confirmarSenha = document.getElementById('confirmarSenha').value;

    // Valida se senhas coincidem
    if (novaSenha !== confirmarSenha) {
        elements.changePasswordMessage.textContent = 'As senhas não coincidem';
        elements.changePasswordMessage.className = 'message error';
        elements.changePasswordMessage.classList.remove('hidden');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/api/auth/change-password`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({
                senhaAtual,
                novaSenha
            })
        });

        const data = await response.json();

        if (response.ok) {
            elements.changePasswordMessage.textContent = data.mensagem || 'Senha alterada com sucesso!';
            elements.changePasswordMessage.className = 'message success';
            elements.changePasswordForm.reset();

            // Volta para tela principal após 2 segundos
            setTimeout(() => {
                showMainScreen();
            }, 2000);
        } else {
            elements.changePasswordMessage.textContent = data.mensagem || 'Erro ao trocar senha';
            elements.changePasswordMessage.className = 'message error';
        }

        elements.changePasswordMessage.classList.remove('hidden');
    } catch (error) {
        console.error('Erro ao trocar senha:', error);
        elements.changePasswordMessage.textContent = 'Erro ao trocar senha';
        elements.changePasswordMessage.className = 'message error';
        elements.changePasswordMessage.classList.remove('hidden');
    }
}

/**
 * Carrega lista de usuários (apenas admin)
 */
async function loadUsers() {
    elements.usersList.innerHTML = '<p class="loading">Carregando...</p>';

    try {
        const response = await fetch(`${API_URL}/api/auth/users`, {
            headers: {
                'Authorization': `Bearer ${state.token}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao carregar usuários');
        }

        const usuarios = await response.json();

        if (usuarios.length === 0) {
            elements.usersList.innerHTML = '<p class="empty">Nenhum usuário encontrado</p>';
            return;
        }

        // Renderiza lista de usuários
        elements.usersList.innerHTML = usuarios.map(user => `
            <div class="user-card ${!user.ativo ? 'inactive' : ''}">
                <div class="user-info">
                    <strong>${user.nomeCompleto}</strong>
                    <span class="user-username">@${user.username}</span>
                    <small>Criado em: ${new Date(user.dataCriacao).toLocaleDateString('pt-BR')}</small>
                    <span class="user-status ${user.ativo ? 'active' : 'inactive'}">
                        ${user.ativo ? '● Ativo' : '○ Inativo'}
                    </span>
                </div>
                <div class="user-actions">
                    ${user.username !== 'admin' ? `
                        ${user.ativo ? `
                            <button class="btn btn-secondary btn-deactivate-user" data-user-id="${user.id}">
                                Desativar
                            </button>
                        ` : `
                            <button class="btn btn-primary btn-reactivate-user" data-user-id="${user.id}">
                                Reativar
                            </button>
                        `}
                    ` : '<span class="admin-badge">Administrador</span>'}
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Erro ao carregar usuários:', error);
        elements.usersList.innerHTML = '<p class="error">Erro ao carregar usuários</p>';
    }
}

/**
 * Cria novo usuário (apenas admin)
 */
async function handleAddUser(e) {
    e.preventDefault();

    const username = document.getElementById('newUsername').value.trim();
    const nomeCompleto = document.getElementById('newNomeCompleto').value.trim();

    try {
        const response = await fetch(`${API_URL}/api/auth/users`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.token}`
            },
            body: JSON.stringify({
                username,
                nomeCompleto
            })
        });

        const data = await response.json();

        if (response.ok) {
            elements.addUserMessage.textContent = data.mensagem || 'Usuário criado com sucesso!';
            elements.addUserMessage.className = 'message success';
            elements.addUserForm.reset();

            // Volta para tela de usuários após 2 segundos
            setTimeout(() => {
                showUsersScreen();
            }, 2000);
        } else {
            elements.addUserMessage.textContent = data.mensagem || 'Erro ao criar usuário';
            elements.addUserMessage.className = 'message error';
        }

        elements.addUserMessage.classList.remove('hidden');
    } catch (error) {
        console.error('Erro ao criar usuário:', error);
        elements.addUserMessage.textContent = 'Erro ao criar usuário';
        elements.addUserMessage.className = 'message error';
        elements.addUserMessage.classList.remove('hidden');
    }
}

/**
 * Desativa usuário (apenas admin)
 */
async function deactivateUser(userId) {
    if (!confirm('Tem certeza que deseja desativar este usuário?')) {
        return;
    }

    try {
        const response = await fetch(`${API_URL}/api/auth/users/${userId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${state.token}`
            }
        });

        if (response.ok) {
            await loadUsers(); // Recarrega lista
        } else {
            const data = await response.json();
            alert(data.mensagem || 'Erro ao desativar usuário');
        }
    } catch (error) {
        console.error('Erro ao desativar usuário:', error);
        alert('Erro ao desativar usuário');
    }
}

/**
 * Reativa usuário (apenas admin)
 */
async function reactivateUser(userId) {
    try {
        const response = await fetch(`${API_URL}/api/auth/users/${userId}/reactivate`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${state.token}`
            }
        });

        if (response.ok) {
            await loadUsers(); // Recarrega lista
        } else {
            const data = await response.json();
            alert(data.mensagem || 'Erro ao reativar usuário');
        }
    } catch (error) {
        console.error('Erro ao reativar usuário:', error);
        alert('Erro ao reativar usuário');
    }
}

// ========== SERVICE WORKER (para PWA futuro) ==========
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        // Descomentado quando houver service-worker.js
        // navigator.serviceWorker.register('/service-worker.js');
    });
}
