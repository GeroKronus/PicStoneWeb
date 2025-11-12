// Configuração da API
const API_URL = window.location.origin;

// Estado da aplicação
const state = {
    token: localStorage.getItem('token') || null,
    username: localStorage.getItem('username') || null,
    currentPhoto: null,
    currentPhotoFile: null,
    originalPhoto: null, // Foto original para ambiente
    ambienteMode: false, // Indica se está em modo ambiente
    cropData: {
        image: null,
        startX: 0,
        startY: 0,
        endX: 0,
        endY: 0,
        isDragging: false,
        scale: 1
    },
    ambienteConfig: {
        tipo: 'simples',
        fundo: 'claro'
    },
    // Estado específico para bancadas (countertops)
    countertopState: {
        croppedImage: null,    // Imagem cortada (reutilizável)
        selectedType: null,     // 'bancada1' ou 'bancada2'
        flip: false            // Opção global de flip
    },
    // Estado para crop overlay na Integração
    cropOverlayState: {
        isActive: false,
        isDragging: false,
        startX: 0,
        startY: 0,
        endX: 0,
        endY: 0,
        originalImageSrc: null,
        canvasRect: null
    },
    // Estado compartilhado de imagem entre todos os cards
    sharedImageState: {
        originalImage: null,      // Base64 da imagem original
        currentImage: null,       // Base64 da imagem atual (pode ter crop)
        fileName: null,           // Nome do arquivo
        file: null,               // File object
        lastUpdated: null,        // Timestamp
        source: null              // 'integracao', 'ambientes', ou 'bookmatch'
    }
};

// Modo de visualização para gerenciar usuários
let currentUsersViewMode = 'cards';
let allUsersManagementData = [];

// Elementos DOM
const elements = {
    loginScreen: document.getElementById('loginScreen'),
    mainScreen: document.getElementById('mainScreen'),
    integracaoScreen: document.getElementById('integracaoScreen'),
    ambientesScreen: document.getElementById('ambientesScreen'),
    cropScreen: document.getElementById('cropScreen'),
    historyScreen: document.getElementById('historyScreen'),
    ambienteConfigScreen: document.getElementById('ambienteConfigScreen'),
    ambienteResultScreen: document.getElementById('ambienteResultScreen'),
    loginForm: document.getElementById('loginForm'),
    loginError: document.getElementById('loginError'),
    loginTab: document.getElementById('loginTab'),
    registerTab: document.getElementById('registerTab'),
    registerForm: document.getElementById('registerForm'),
    registerMessage: document.getElementById('registerMessage'),
    // Botões principais
    integracaoCard: document.getElementById('integracaoCard'),
    ambientesCard: document.getElementById('ambientesCard'),
    // Integração
    captureBtnIntegracao: document.getElementById('captureBtnIntegracao'),
    fileInputIntegracao: document.getElementById('fileInputIntegracao'),
    photoPreviewIntegracao: document.getElementById('photoPreviewIntegracao'),
    previewImageIntegracao: document.getElementById('previewImageIntegracao'),
    clearPhotoBtnIntegracao: document.getElementById('clearPhotoBtnIntegracao'),
    adjustImageBtnIntegracao: document.getElementById('adjustImageBtnIntegracao'),
    resetImageBtnIntegracao: document.getElementById('resetImageBtnIntegracao'),
    cropOverlayIntegracao: document.getElementById('cropOverlayIntegracao'),
    cropIndicatorIntegracao: document.getElementById('cropIndicatorIntegracao'),
    backToMainFromIntegracaoBtn: document.getElementById('backToMainFromIntegracaoBtn'),
    // Ambientes
    captureBtnAmbientes: document.getElementById('captureBtnAmbientes'),
    fileInputAmbientes: document.getElementById('fileInputAmbientes'),
    photoPreviewAmbientes: document.getElementById('photoPreviewAmbientes'),
    previewImageAmbientes: document.getElementById('previewImageAmbientes'),
    clearPhotoBtnAmbientes: document.getElementById('clearPhotoBtnAmbientes'),
    adjustImageBtnAmbientes: document.getElementById('adjustImageBtnAmbientes'),
    resetImageBtnAmbientes: document.getElementById('resetImageBtnAmbientes'),
    cropOverlayAmbientes: document.getElementById('cropOverlayAmbientes'),
    cropIndicatorAmbientes: document.getElementById('cropIndicatorAmbientes'),
    captureSectionAmbientes: document.getElementById('captureSectionAmbientes'),
    backToMainFromAmbientesBtn: document.getElementById('backToMainFromAmbientesBtn'),
    ambienteOptions: document.getElementById('ambienteOptions'),
    // Formulário (apenas na Integração)
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
    ambienteBtn: document.getElementById('ambienteBtn'),
    photoIndicator: document.getElementById('photoIndicator'),
    cancelAmbienteBtn: document.getElementById('cancelAmbienteBtn'),
    continuarCropAmbienteBtn: document.getElementById('continuarCropAmbienteBtn'),
    countertopsBtn: document.getElementById('countertopsBtn'),
    countertopSelectionScreen: document.getElementById('countertopSelectionScreen'),
    cancelCountertopSelectionBtn: document.getElementById('cancelCountertopSelectionBtn'),
    flipCountertop: document.getElementById('flipCountertop'),
    backToMainBtn: document.getElementById('backToMainBtn'),
    downloadAllAmbientesBtn: document.getElementById('downloadAllAmbientesBtn'),
    newAmbienteBtn: document.getElementById('newAmbienteBtn'),
    ambientesGallery: document.getElementById('ambientesGallery'),
    ambienteMessage: document.getElementById('ambienteMessage'),
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
    backFromAddUserBtn: document.getElementById('backFromAddUserBtn'),
    loadingOverlay: document.getElementById('loadingOverlay'),
    loadingMessage: document.getElementById('loadingMessage'),
    loadingSubmessage: document.getElementById('loadingSubmessage'),
    progressContainer: document.getElementById('progressContainer'),
    progressBar: document.getElementById('progressBar'),
    progressText: document.getElementById('progressText'),
    // Visualização Cards/Tabela em Gerenciar Usuários
    usersCardViewBtn: document.getElementById('usersCardViewBtn'),
    usersTableViewBtn: document.getElementById('usersTableViewBtn'),
    usersTable: document.getElementById('usersTable'),
    usersManagementTableBody: document.getElementById('usersManagementTableBody')
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
        } else if (response.status === 401) {
            // Token expirado ou inválido - redireciona para login
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
            renovarTokenAutomaticamente();
        }
    }, 30 * 60 * 1000); // 30 minutos

    // Também verifica imediatamente ao iniciar
    if (state.token && tokenExpiresInLessThanOneHour(state.token)) {
        renovarTokenAutomaticamente();
    }
}

// ========== GERENCIAMENTO DE IMAGEM COMPARTILHADA ==========

/**
 * Salva imagem no estado compartilhado
 * @param {string} originalImage - Base64 da imagem original
 * @param {string} currentImage - Base64 da imagem atual (pode ter crop)
 * @param {string} fileName - Nome do arquivo
 * @param {File} file - Objeto File
 * @param {string} source - Origem ('integracao', 'ambientes', 'bookmatch')
 */
function saveSharedImage(originalImage, currentImage, fileName, file, source) {
    state.sharedImageState = {
        originalImage: originalImage,
        currentImage: currentImage,
        fileName: fileName,
        file: file,
        lastUpdated: Date.now(),
        source: source
    };
}

/**
 * Carrega imagem do estado compartilhado para o card atual
 * @param {string} targetCard - Card de destino ('integracao', 'ambientes', 'bookmatch')
 * @returns {object|null} Objeto com os dados da imagem ou null se não houver
 */
function loadSharedImage(targetCard) {
    if (!state.sharedImageState.originalImage) {
        return null;
    }

    return {
        originalImage: state.sharedImageState.originalImage,
        currentImage: state.sharedImageState.currentImage,
        fileName: state.sharedImageState.fileName,
        file: state.sharedImageState.file
    };
}

/**
 * Verifica se existe imagem compartilhada disponível
 * @returns {boolean}
 */
function hasSharedImage() {
    return state.sharedImageState.originalImage !== null;
}

/**
 * Limpa o estado compartilhado de imagem
 */
function clearSharedImage() {
    state.sharedImageState = {
        originalImage: null,
        currentImage: null,
        fileName: null,
        file: null,
        lastUpdated: null,
        source: null
    };
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
    elements.registerForm.addEventListener('submit', handleRegister);

    // Tab switching
    elements.loginTab.addEventListener('click', () => {
        elements.loginTab.classList.add('active');
        elements.registerTab.classList.remove('active');
        elements.loginForm.classList.add('active');
        elements.registerForm.classList.remove('active');
    });

    elements.registerTab.addEventListener('click', () => {
        elements.registerTab.classList.add('active');
        elements.loginTab.classList.remove('active');
        elements.registerForm.classList.add('active');
        elements.loginForm.classList.remove('active');
    });

    // Modal de verificação de email
    const closeEmailModalBtn = document.getElementById('closeEmailModalBtn');
    const emailVerificationModal = document.getElementById('emailVerificationModal');
    if (closeEmailModalBtn && emailVerificationModal) {
        closeEmailModalBtn.addEventListener('click', () => {
            emailVerificationModal.classList.add('hidden');
            // Volta para a aba de login
            elements.loginTab.click();
        });

        // Fechar modal clicando fora do conteúdo
        emailVerificationModal.addEventListener('click', (e) => {
            if (e.target === emailVerificationModal) {
                emailVerificationModal.classList.add('hidden');
                elements.loginTab.click();
            }
        });
    }

    // Navegação principal
    elements.integracaoCard.addEventListener('click', showIntegracaoScreen);
    elements.ambientesCard.addEventListener('click', showAmbientesScreen);
    elements.backToMainFromIntegracaoBtn.addEventListener('click', showMainScreen);
    elements.backToMainFromAmbientesBtn.addEventListener('click', showMainScreen);

    // Integração - Captura de foto
    elements.captureBtnIntegracao.addEventListener('click', () => elements.fileInputIntegracao.click());
    elements.fileInputIntegracao.addEventListener('change', handleFileSelectIntegracao);
    elements.fileInputIntegracao.addEventListener('input', handleFileSelectIntegracao);
    elements.clearPhotoBtnIntegracao.addEventListener('click', clearPhotoIntegracao);
    elements.adjustImageBtnIntegracao.addEventListener('click', ativarCropOverlayIntegracao);
    elements.resetImageBtnIntegracao.addEventListener('click', resetarParaOriginalIntegracao);

    // Crop Overlay na Integração - mousedown no canvas, move/up no document
    elements.cropOverlayIntegracao.addEventListener('mousedown', iniciarSelecaoCrop);
    elements.cropOverlayIntegracao.addEventListener('touchstart', iniciarSelecaoCropTouch, { passive: false });

    // Ambientes - Captura de foto
    elements.captureBtnAmbientes.addEventListener('click', () => elements.fileInputAmbientes.click());
    elements.fileInputAmbientes.addEventListener('change', handleFileSelectAmbientes);
    elements.fileInputAmbientes.addEventListener('input', handleFileSelectAmbientes);
    elements.clearPhotoBtnAmbientes.addEventListener('click', clearPhotoAmbientes);

    // Crop de Ambientes
    if (elements.adjustImageBtnAmbientes) {
        elements.adjustImageBtnAmbientes.addEventListener('click', ativarCropOverlayAmbientes);
    }
    if (elements.resetImageBtnAmbientes) {
        elements.resetImageBtnAmbientes.addEventListener('click', resetarParaOriginalAmbientes);
    }
    // Event listeners para crop de Ambientes (no canvas)
    if (elements.cropOverlayAmbientes) {
        elements.cropOverlayAmbientes.addEventListener('mousedown', iniciarSelecaoCrop);
        elements.cropOverlayAmbientes.addEventListener('touchstart', iniciarSelecaoCropTouch, { passive: false });
    }

    // Formulário de upload (só na Integração)
    elements.uploadForm.addEventListener('submit', handleUpload);
    if (elements.logoutBtn) {
        elements.logoutBtn.addEventListener('click', handleLogout);
    }
    if (elements.historyBtn) {
        elements.historyBtn.addEventListener('click', showHistoryScreen);
    }
    if (elements.backBtn) {
        elements.backBtn.addEventListener('click', showMainScreen);
    }

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

    // Ambiente event listeners
    elements.ambienteBtn.addEventListener('click', startAmbienteFlow);
    elements.countertopsBtn.addEventListener('click', startCountertopFlow);
    elements.cancelAmbienteBtn.addEventListener('click', () => showMainScreen());
    elements.cancelCountertopSelectionBtn.addEventListener('click', () => showMainScreen());
    elements.continuarCropAmbienteBtn.addEventListener('click', abrirCropParaAmbiente);
    elements.backToMainBtn.addEventListener('click', handleBackFromResults);
    elements.newAmbienteBtn.addEventListener('click', startAmbienteFlow);
    elements.downloadAllAmbientesBtn.addEventListener('click', downloadAllAmbientes);

    // Event delegation para botões de download e compartilhar
    elements.ambientesGallery.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-download-single')) {
            const url = e.target.dataset.url;
            const nome = e.target.dataset.nome;
            downloadAmbiente(url, nome);
        } else if (e.target.classList.contains('btn-share-single')) {
            const url = e.target.dataset.url;
            const nome = e.target.dataset.nome;
            shareAmbiente(url, nome);
        }
    });

    // Event delegation para seleção de countertop via click no thumb
    document.addEventListener('click', (e) => {
        const preview = e.target.closest('.countertop-preview');
        if (preview && preview.dataset.type) {
            // Verifica se o card pai está desabilitado
            const card = preview.closest('.countertop-card');
            if (card && card.classList.contains('disabled')) {
                return; // Ignora clique em cards desabilitados
            }
            const type = preview.dataset.type;
            selectCountertopAndGenerate(type);
        }
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

    // Event listeners para alternância de visualização (Cards/Tabela)
    elements.usersCardViewBtn.addEventListener('click', () => switchUsersViewMode('cards'));
    elements.usersTableViewBtn.addEventListener('click', () => switchUsersViewMode('table'));

    // Event listener para busca de usuários
    const searchUsersManagementInput = document.getElementById('searchUsersManagementInput');
    if (searchUsersManagementInput) {
        searchUsersManagementInput.addEventListener('input', (e) => {
            filterUsersManagement(e.target.value);
        });
    }

    // Event delegation para botões de gerenciar usuários (Cards)
    elements.usersList.addEventListener('click', async (e) => {
        if (e.target.classList.contains('btn-deactivate-user')) {
            const userId = e.target.dataset.userId;
            await deactivateUser(userId);
        } else if (e.target.classList.contains('btn-reactivate-user')) {
            const userId = e.target.dataset.userId;
            const userName = e.target.dataset.userName;
            await reactivateUser(userId, userName);
        }
    });

    // Event delegation para botões de gerenciar usuários (Tabela)
    elements.usersTable.addEventListener('click', async (e) => {
        if (e.target.classList.contains('btn-deactivate-user')) {
            const userId = e.target.dataset.userId;
            await deactivateUser(userId);
        } else if (e.target.classList.contains('btn-reactivate-user')) {
            const userId = e.target.dataset.userId;
            const userName = e.target.dataset.userName;
            await reactivateUser(userId, userName);
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

        // Verificar aviso de expiração
        if (data.expiracaoProxima && data.diasRestantes) {
            mostrarBannerExpiracao(data.diasRestantes, data.dataExpiracao);
        } else {
            esconderBannerExpiracao();
        }

        showMainScreen();
        iniciarVerificacaoToken(); // Inicia verificação automática do token
        elements.loginError.classList.add('hidden');
    } catch (error) {
        elements.loginError.textContent = error.message;
        elements.loginError.classList.remove('hidden');
    }
}

async function handleRegister(e) {
    e.preventDefault();

    const nomeCompleto = document.getElementById('registerNomeCompleto').value;
    const email = document.getElementById('registerEmail').value;
    const senha = document.getElementById('registerSenha').value;
    const confirmarSenha = document.getElementById('registerConfirmarSenha').value;

    // Validação de senhas
    if (senha !== confirmarSenha) {
        elements.registerMessage.textContent = 'As senhas não coincidem';
        elements.registerMessage.classList.remove('hidden', 'success');
        elements.registerMessage.classList.add('error');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/api/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nomeCompleto, email, senha })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao criar conta');
        }

        const data = await response.json();

        // Limpar formulário
        elements.registerForm.reset();

        // Esconder mensagem antiga (se estiver visível)
        elements.registerMessage.classList.add('hidden');

        // Mostrar modal de sucesso
        const modal = document.getElementById('emailVerificationModal');
        modal.classList.remove('hidden');

    } catch (error) {
        elements.registerMessage.textContent = error.message;
        elements.registerMessage.classList.remove('hidden', 'success');
        elements.registerMessage.classList.add('error');
    }
}

function handleLogout() {
    state.token = null;
    state.username = null;
    localStorage.removeItem('token');
    localStorage.removeItem('username');

    // Limpa estados
    state.currentPhotoFile = null;
    state.originalPhoto = null;

    // Reset formulário se existir
    if (elements.uploadForm) {
        elements.uploadForm.reset();
    }

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

/**
 * Gerencia o botão "Voltar" da tela de resultados
 * Se há crop de countertop salvo, volta para seleção
 * Caso contrário, volta para tela principal
 */
function handleBackFromResults() {
    if (state.countertopState.croppedImage) {
        // Está no flow de countertop: volta para seleção
        showScreen(elements.countertopSelectionScreen);
    } else {
        // Flow normal: volta para tela principal
        showMainScreen();
    }
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
    if (state.username === 'rogerio@picstone.com.br') {
        if (elements.manageUsersBtn) {
            elements.manageUsersBtn.classList.remove('hidden');
        }
        if (elements.pendingUsersBtn) {
            elements.pendingUsersBtn.classList.remove('hidden');
        }
        // Salva no localStorage para uso no history.js
        localStorage.setItem('isAdmin', 'true');
    } else {
        if (elements.manageUsersBtn) elements.manageUsersBtn.classList.add('hidden');
        if (elements.pendingUsersBtn) elements.pendingUsersBtn.classList.add('hidden');
        localStorage.setItem('isAdmin', 'false');
    }

    // Mostra botão de histórico para todos os usuários
    if (elements.historyBtn) {
        elements.historyBtn.classList.remove('hidden');
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
    if (state.username !== 'rogerio@picstone.com.br') {
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

function showIntegracaoScreen() {
    showScreen(elements.integracaoScreen);

    // Carrega automaticamente imagem compartilhada se existir
    if (hasSharedImage()) {
        const sharedImage = loadSharedImage('integracao');
        if (sharedImage) {
            state.originalPhoto = new Image();
            state.originalPhoto.src = sharedImage.originalImage;
            state.currentPhotoFile = sharedImage.file;
            elements.previewImageIntegracao.src = sharedImage.currentImage;
            elements.photoPreviewIntegracao.classList.remove('hidden');
            elements.submitBtn.disabled = false;
        }
    } else {
        clearPhotoIntegracao();
    }
}

function showAmbientesScreen() {
    showScreen(elements.ambientesScreen);

    // Carrega automaticamente imagem compartilhada se existir
    if (hasSharedImage()) {
        const sharedImage = loadSharedImage('ambientes');
        if (sharedImage) {
            state.originalPhoto = new Image();
            state.originalPhoto.src = sharedImage.originalImage;
            state.currentPhotoFile = sharedImage.file;
            state.cropOverlayState.originalImageSrc = sharedImage.currentImage;
            elements.previewImageAmbientes.src = sharedImage.currentImage;
            elements.photoPreviewAmbientes.classList.remove('hidden');
            if (elements.captureSectionAmbientes) {
                elements.captureSectionAmbientes.classList.add('hidden');
            }
            elements.ambienteOptions.classList.remove('hidden');
        }
    } else {
        clearPhotoAmbientes();
    }
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
    elements.ambienteBtn.classList.add('hidden');
    elements.countertopsBtn.classList.add('hidden');
    elements.photoIndicator.classList.add('hidden');
}

function clearPhotoState() {
    // Limpa apenas estado atual (para preparar nova foto)
    state.currentPhotoFile = null;
    elements.previewImage.src = '';
    elements.photoPreview.classList.add('hidden');
    elements.submitBtn.disabled = true;
    elements.ambienteBtn.classList.add('hidden');
    elements.countertopsBtn.classList.add('hidden');
    elements.photoIndicator.classList.add('hidden');
}

// ========== INTEGRAÇÃO - CAPTURA DE FOTO ==========
function handleFileSelectIntegracao(e) {
    const file = e.target.files[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
        showMessage('Por favor, selecione uma imagem válida', 'error');
        return;
    }

    if (file.size > 10 * 1024 * 1024) {
        showMessage('Arquivo muito grande. Máximo 10MB', 'error');
        return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            state.originalPhoto = img;
            compressAndPreviewImageIntegracao(file);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function compressAndPreviewImageIntegracao(file) {
    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            const canvas = document.createElement('canvas');
            canvas.width = img.width;
            canvas.height = img.height;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);

            canvas.toBlob((blob) => {
                state.currentPhotoFile = new File([blob], file.name, {
                    type: 'image/jpeg',
                    lastModified: Date.now()
                });

                const currentImageData = canvas.toDataURL('image/jpeg', 0.85);
                elements.previewImageIntegracao.src = currentImageData;
                elements.photoPreviewIntegracao.classList.remove('hidden');
                elements.submitBtn.disabled = false;

                // Salva imagem no estado compartilhado
                const originalImageData = state.originalPhoto ? state.originalPhoto.src : currentImageData;
                saveSharedImage(originalImageData, currentImageData, file.name, state.currentPhotoFile, 'integracao');
            }, 'image/jpeg', 0.95);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function clearPhotoIntegracao() {
    state.currentPhotoFile = null;
    state.originalPhoto = null;
    elements.previewImageIntegracao.src = '';
    elements.photoPreviewIntegracao.classList.add('hidden');
    elements.fileInputIntegracao.value = '';
    elements.submitBtn.disabled = true;
    // Reset crop overlay state
    state.cropOverlayState.isActive = false;
    state.cropOverlayState.originalImageSrc = null;
    elements.cropOverlayIntegracao.classList.add('hidden');
    elements.resetImageBtnIntegracao.classList.add('hidden');
    // Nota: NÃO limpa estado compartilhado aqui, pois outras telas podem estar usando
}

// ========== INTEGRAÇÃO - CROP OVERLAY ==========

// Função genérica para ativar crop overlay (usada por BookMatch, Ambientes, etc.)
function ativarCropOverlay(imgElement, canvasElement, resetBtnElement, onCropComplete, indicatorElement = null) {
    if (!imgElement || !imgElement.src) return;

    // Store original image if not already stored
    if (!state.cropOverlayState.originalImageSrc) {
        state.cropOverlayState.originalImageSrc = imgElement.src;
    }

    // Configurar elementos atuais
    state.cropOverlayState.currentCanvas = canvasElement;
    state.cropOverlayState.currentImage = imgElement;
    state.cropOverlayState.currentResetBtn = resetBtnElement;
    state.cropOverlayState.currentIndicator = indicatorElement;
    state.cropOverlayState.onCropComplete = onCropComplete;

    // Setup canvas to match image EXACTLY
    canvasElement.width = imgElement.naturalWidth;
    canvasElement.height = imgElement.naturalHeight;

    // Style to match displayed size
    canvasElement.style.width = imgElement.offsetWidth + 'px';
    canvasElement.style.height = imgElement.offsetHeight + 'px';
    canvasElement.style.top = '0';
    canvasElement.style.left = '0';

    // Show canvas overlay
    canvasElement.classList.remove('hidden');
    state.cropOverlayState.isActive = true;

    // Mostrar indicador visual APENAS em mobile (se fornecido)
    if (indicatorElement) {
        const isMobile = window.matchMedia('(hover: none) and (pointer: coarse)').matches;
        if (isMobile) {
            indicatorElement.classList.remove('hidden');
        }
    }

    // Reset state
    state.cropOverlayState.isDragging = false;
    state.cropOverlayState.startX = 0;
    state.cropOverlayState.startY = 0;
    state.cropOverlayState.endX = 0;
    state.cropOverlayState.endY = 0;

    // Update canvas rect AFTER showing it
    setTimeout(() => {
        state.cropOverlayState.canvasRect = canvasElement.getBoundingClientRect();
    }, 10);

    // Clear canvas
    const ctx = canvasElement.getContext('2d');
    ctx.clearRect(0, 0, canvasElement.width, canvasElement.height);
}

// Wrapper para compatibilidade com código legado da Integração
function ativarCropOverlayIntegracao() {
    ativarCropOverlay(
        elements.previewImageIntegracao,
        elements.cropOverlayIntegracao,
        elements.resetImageBtnIntegracao,
        (croppedBase64, croppedFile) => {
            state.currentPhotoFile = croppedFile;
            elements.previewImageIntegracao.src = croppedBase64;
        },
        elements.cropIndicatorIntegracao
    );
}

// Wrapper para Ambientes
function ativarCropOverlayAmbientes() {
    ativarCropOverlay(
        elements.previewImageAmbientes,
        elements.cropOverlayAmbientes,
        elements.resetImageBtnAmbientes,
        (croppedBase64, croppedFile) => {
            state.currentPhotoFile = croppedFile;
            elements.previewImageAmbientes.src = croppedBase64;
        },
        elements.cropIndicatorAmbientes
    );
}

function resetarParaOriginalAmbientes() {
    if (state.cropOverlayState.originalImageSrc) {
        elements.previewImageAmbientes.src = state.cropOverlayState.originalImageSrc;
        state.currentPhotoFile = null; // Reset to original file
        elements.resetImageBtnAmbientes.classList.add('hidden');
        elements.cropOverlayAmbientes.classList.add('hidden');
        elements.cropIndicatorAmbientes.classList.add('hidden');
        state.cropOverlayState.originalImageSrc = null;
    }
}

function iniciarSelecaoCrop(e) {
    if (!state.cropOverlayState.isActive) return;

    e.preventDefault();
    state.cropOverlayState.isDragging = true;

    // Esconde o indicador visual quando o usuário começa a selecionar
    if (state.cropOverlayState.currentIndicator) {
        state.cropOverlayState.currentIndicator.classList.add('hidden');
    }

    const rect = state.cropOverlayState.canvasRect;
    const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;

    // Get click position relative to canvas
    const scaleX = canvas.width / canvas.offsetWidth;
    const scaleY = canvas.height / canvas.offsetHeight;

    state.cropOverlayState.startX = (e.clientX - rect.left) * scaleX;
    state.cropOverlayState.startY = (e.clientY - rect.top) * scaleY;
    state.cropOverlayState.endX = state.cropOverlayState.startX;
    state.cropOverlayState.endY = state.cropOverlayState.startY;

    // Add document listeners for move and up
    document.addEventListener('mousemove', atualizarSelecaoCrop);
    document.addEventListener('mouseup', finalizarEAplicarCrop);
}

function atualizarSelecaoCrop(e) {
    if (!state.cropOverlayState.isDragging) return;

    e.preventDefault();

    const rect = state.cropOverlayState.canvasRect;
    const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
    const ctx = canvas.getContext('2d');

    // Get current position with scaling
    const scaleX = canvas.width / canvas.offsetWidth;
    const scaleY = canvas.height / canvas.offsetHeight;

    state.cropOverlayState.endX = Math.max(0, Math.min(canvas.width, (e.clientX - rect.left) * scaleX));
    state.cropOverlayState.endY = Math.max(0, Math.min(canvas.height, (e.clientY - rect.top) * scaleY));

    // Clear and redraw selection
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw semi-transparent dark overlay
    ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Calculate selection rectangle
    const x = Math.min(state.cropOverlayState.startX, state.cropOverlayState.endX);
    const y = Math.min(state.cropOverlayState.startY, state.cropOverlayState.endY);
    const width = Math.abs(state.cropOverlayState.endX - state.cropOverlayState.startX);
    const height = Math.abs(state.cropOverlayState.endY - state.cropOverlayState.startY);

    // Clear selected area (removes dark overlay)
    ctx.clearRect(x, y, width, height);

    // Draw selection border (green)
    ctx.strokeStyle = '#00ff00';
    ctx.lineWidth = 3;
    ctx.strokeRect(x, y, width, height);
}

function finalizarEAplicarCrop(e) {
    if (!state.cropOverlayState.isDragging) return;

    e.preventDefault();
    state.cropOverlayState.isDragging = false;

    // Remove document listeners
    document.removeEventListener('mousemove', atualizarSelecaoCrop);
    document.removeEventListener('mouseup', finalizarEAplicarCrop);

    const x = Math.min(state.cropOverlayState.startX, state.cropOverlayState.endX);
    const y = Math.min(state.cropOverlayState.startY, state.cropOverlayState.endY);
    const width = Math.abs(state.cropOverlayState.endX - state.cropOverlayState.startX);
    const height = Math.abs(state.cropOverlayState.endY - state.cropOverlayState.startY);

    // Check if selection is valid (minimum 10x10 pixels)
    if (width < 10 || height < 10) {
        // Selection too small, just hide overlay
        const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
        canvas.classList.add('hidden');
        state.cropOverlayState.isActive = false;
        return;
    }

    // Apply crop automatically
    aplicarCropGenerico(x, y, width, height);
}

// Função genérica para aplicar crop (usada por todas as features)
function aplicarCropGenerico(x, y, width, height) {
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = width;
    tempCanvas.height = height;
    const ctx = tempCanvas.getContext('2d');

    const img = new Image();
    img.onload = () => {
        ctx.drawImage(img, x, y, width, height, 0, 0, width, height);

        tempCanvas.toBlob((blob) => {
            const croppedFile = new File([blob], 'cropped.jpg', {
                type: 'image/jpeg',
                lastModified: Date.now()
            });
            const croppedBase64 = tempCanvas.toDataURL('image/jpeg', 0.95);

            // Hide overlay
            const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
            canvas.classList.add('hidden');
            state.cropOverlayState.isActive = false;

            // Show reset button (se fornecido)
            if (state.cropOverlayState.currentResetBtn) {
                state.cropOverlayState.currentResetBtn.classList.remove('hidden');
            }

            // Call callback (se fornecido)
            if (state.cropOverlayState.onCropComplete) {
                state.cropOverlayState.onCropComplete(croppedBase64, croppedFile);
            }

        }, 'image/jpeg', 0.95);
    };
    const imgSrc = state.cropOverlayState.currentImage ? state.cropOverlayState.currentImage.src : elements.previewImageIntegracao.src;
    img.src = imgSrc;
}

function iniciarSelecaoCropTouch(e) {
    if (!state.cropOverlayState.isActive) return;

    e.preventDefault();
    const touch = e.touches[0];
    state.cropOverlayState.isDragging = true;

    // Esconde o indicador visual quando o usuário começa a selecionar
    if (state.cropOverlayState.currentIndicator) {
        state.cropOverlayState.currentIndicator.classList.add('hidden');
    }

    const rect = state.cropOverlayState.canvasRect;
    const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;

    const scaleX = canvas.width / canvas.offsetWidth;
    const scaleY = canvas.height / canvas.offsetHeight;

    state.cropOverlayState.startX = (touch.clientX - rect.left) * scaleX;
    state.cropOverlayState.startY = (touch.clientY - rect.top) * scaleY;
    state.cropOverlayState.endX = state.cropOverlayState.startX;
    state.cropOverlayState.endY = state.cropOverlayState.startY;

    // Add document listeners for touch move and end
    document.addEventListener('touchmove', atualizarSelecaoCropTouch, { passive: false });
    document.addEventListener('touchend', finalizarEAplicarCropTouch);
}

function atualizarSelecaoCropTouch(e) {
    if (!state.cropOverlayState.isDragging) return;

    e.preventDefault();
    const touch = e.touches[0];

    const rect = state.cropOverlayState.canvasRect;
    const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
    const ctx = canvas.getContext('2d');

    const scaleX = canvas.width / canvas.offsetWidth;
    const scaleY = canvas.height / canvas.offsetHeight;

    state.cropOverlayState.endX = Math.max(0, Math.min(canvas.width, (touch.clientX - rect.left) * scaleX));
    state.cropOverlayState.endY = Math.max(0, Math.min(canvas.height, (touch.clientY - rect.top) * scaleY));

    // Clear and redraw selection (same as mouse)
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    const x = Math.min(state.cropOverlayState.startX, state.cropOverlayState.endX);
    const y = Math.min(state.cropOverlayState.startY, state.cropOverlayState.endY);
    const width = Math.abs(state.cropOverlayState.endX - state.cropOverlayState.startX);
    const height = Math.abs(state.cropOverlayState.endY - state.cropOverlayState.startY);

    ctx.clearRect(x, y, width, height);
    ctx.strokeStyle = '#00ff00';
    ctx.lineWidth = 3;
    ctx.strokeRect(x, y, width, height);
}

function finalizarEAplicarCropTouch(e) {
    if (!state.cropOverlayState.isDragging) return;

    e.preventDefault();
    state.cropOverlayState.isDragging = false;

    // Remove document listeners
    document.removeEventListener('touchmove', atualizarSelecaoCropTouch);
    document.removeEventListener('touchend', finalizarEAplicarCropTouch);

    const x = Math.min(state.cropOverlayState.startX, state.cropOverlayState.endX);
    const y = Math.min(state.cropOverlayState.startY, state.cropOverlayState.endY);
    const width = Math.abs(state.cropOverlayState.endX - state.cropOverlayState.startX);
    const height = Math.abs(state.cropOverlayState.endY - state.cropOverlayState.startY);

    // Check if selection is valid (minimum 10x10 pixels)
    if (width < 10 || height < 10) {
        // Selection too small, just hide overlay
        elements.cropOverlayIntegracao.classList.add('hidden');
        state.cropOverlayState.isActive = false;
        return;
    }

    // Apply crop automatically
    aplicarCropGenerico(x, y, width, height);
}

function resetarParaOriginalIntegracao() {
    if (!state.cropOverlayState.originalImageSrc) return;

    // Restore original image
    elements.previewImageIntegracao.src = state.cropOverlayState.originalImageSrc;

    // Recreate the file from the original image
    fetch(state.cropOverlayState.originalImageSrc)
        .then(res => res.blob())
        .then(blob => {
            state.currentPhotoFile = new File([blob], 'original.jpg', {
                type: 'image/jpeg',
                lastModified: Date.now()
            });
        });

    // Hide reset button and clear stored original since we're back to original
    elements.resetImageBtnIntegracao.classList.add('hidden');
    state.cropOverlayState.originalImageSrc = null;

    // Also hide the crop overlay if it was visible
    elements.cropOverlayIntegracao.classList.add('hidden');
    state.cropOverlayState.isActive = false;
}

// ========== AMBIENTES - CAPTURA DE FOTO ==========
function handleFileSelectAmbientes(e) {
    const file = e.target.files[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
        alert('Por favor, selecione uma imagem válida');
        return;
    }

    if (file.size > 10 * 1024 * 1024) {
        alert('Arquivo muito grande. Máximo 10MB');
        return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            state.originalPhoto = img;
            compressAndPreviewImageAmbientes(file);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function compressAndPreviewImageAmbientes(file) {
    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            // Salva imagem original para uso nos ambientes
            state.originalPhoto = img;

            const canvas = document.createElement('canvas');
            canvas.width = img.width;
            canvas.height = img.height;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);

            canvas.toBlob((blob) => {
                state.currentPhotoFile = new File([blob], file.name, {
                    type: 'image/jpeg',
                    lastModified: Date.now()
                });

                const imageDataUrl = canvas.toDataURL('image/jpeg', 0.85);
                elements.previewImageAmbientes.src = imageDataUrl;

                // Salva imagem original para crop
                state.cropOverlayState.originalImageSrc = imageDataUrl;

                elements.photoPreviewAmbientes.classList.remove('hidden');

                // Esconde botão "Escolher/Tirar Foto"
                if (elements.captureSectionAmbientes) {
                    elements.captureSectionAmbientes.classList.add('hidden');
                }

                // Mostra opções de ambiente
                elements.ambienteOptions.classList.remove('hidden');

                // Salva imagem no estado compartilhado
                const originalImageData = state.originalPhoto ? state.originalPhoto.src : imageDataUrl;
                saveSharedImage(originalImageData, imageDataUrl, file.name, state.currentPhotoFile, 'ambientes');
            }, 'image/jpeg', 0.95);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function clearPhotoAmbientes() {
    state.currentPhotoFile = null;
    state.originalPhoto = null;
    elements.previewImageAmbientes.src = '';
    elements.photoPreviewAmbientes.classList.add('hidden');
    elements.fileInputAmbientes.value = '';
    elements.ambienteOptions.classList.add('hidden');

    // Reset crop state
    state.cropOverlayState.originalImageSrc = null;
    if (elements.resetImageBtnAmbientes) {
        elements.resetImageBtnAmbientes.classList.add('hidden');
    }
    if (elements.cropOverlayAmbientes) {
        elements.cropOverlayAmbientes.classList.add('hidden');
    }
    if (elements.cropIndicatorAmbientes) {
        elements.cropIndicatorAmbientes.classList.add('hidden');
    }

    // Mostra botão "Escolher/Tirar Foto" novamente
    if (elements.captureSectionAmbientes) {
        elements.captureSectionAmbientes.classList.remove('hidden');
    }

    // Limpa estado compartilhado
    clearSharedImage();
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

        // Mostra botões de ambiente (permanecem visíveis)
        elements.ambienteBtn.classList.remove('hidden');
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
            // NÃO limpa state.originalPhoto - fica disponível para ambiente/ajuste
            // NÃO oculta ambienteBtn - fica acessível
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
    if (elements.cropInfo) {
        elements.cropInfo.classList.add('hidden'); // Esconde até haver uma seleção
    }
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

    // Calcula coordenadas na imagem original usando escalas separadas para X e Y
    const x = Math.min(state.cropData.startX, state.cropData.endX) * state.cropData.scaleX;
    const y = Math.min(state.cropData.startY, state.cropData.endY) * state.cropData.scaleY;
    const width = Math.abs(state.cropData.endX - state.cropData.startX) * state.cropData.scaleX;
    const height = Math.abs(state.cropData.endY - state.cropData.startY) * state.cropData.scaleY;

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
        if (state.countertopState.croppedImage !== null || state.ambienteConfig.tipo === 'countertop') {
            // Flow de countertop: salva crop e mostra tela de seleção
            showCountertopSelection(file);
        } else if (state.ambienteMode) {
            // Outros ambientes (cavalete, nicho): gera diretamente
            gerarAmbiente(file);
        } else {
            // Modo ajuste normal - INTEGRACAO only
            // Since this flow should only happen from Integracao screen
            compressAndPreviewImageIntegracao(file);
            showScreen(elements.integracaoScreen);
        }
    }, 'image/jpeg', 0.95);
}

function cancelCrop() {
    // Limpa input file para permitir selecionar a mesma imagem novamente
    if (elements.fileInputIntegracao) {
        elements.fileInputIntegracao.value = '';
    }
    if (elements.fileInputAmbientes) {
        elements.fileInputAmbientes.value = '';
    }
    state.ambienteMode = false;
    state.ambienteConfig.tipo = 'simples';  // Reset tipo
    state.countertopState.croppedImage = null;  // Clear countertop state
    if (elements.cropInfo) {
        elements.cropInfo.classList.add('hidden');
    }
    showMainScreen();
}

function abrirCropParaAjuste() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma imagem disponível para ajustar', 'error');
        return;
    }

    // Carrega imagem original no crop (modo ajuste normal)
    state.ambienteMode = false;
    state.cropData.image = state.originalPhoto;
    initializeCropCanvas();
    showCropScreen();
}

// ========== CROP INLINE NA INTEGRAÇÃO ==========
function abrirCropIntegracaoInline() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma imagem disponível para ajustar', 'error');
        return;
    }

    // Esconde preview, mostra crop
    elements.photoPreviewIntegracao.classList.add('hidden');
    elements.cropSectionIntegracao.classList.remove('hidden');

    // Inicializa crop com imagem original
    state.cropData.image = state.originalPhoto;
    state.cropData.startX = 0;
    state.cropData.startY = 0;
    state.cropData.endX = 0;
    state.cropData.endY = 0;
    state.cropData.isDragging = false;

    initializeCropCanvasIntegracao();
}

function initializeCropCanvasIntegracao() {
    const canvas = elements.cropCanvasIntegracao;
    const ctx = canvas.getContext('2d');
    const img = state.cropData.image;

    // Calcula dimensões mantendo proporção
    const maxWidth = Math.min(window.innerWidth - 40, 800);
    const maxHeight = window.innerHeight - 300;

    let canvasWidth = img.width;
    let canvasHeight = img.height;

    if (canvasWidth > maxWidth || canvasHeight > maxHeight) {
        const scale = Math.min(maxWidth / canvasWidth, maxHeight / canvasHeight);
        canvasWidth = canvasWidth * scale;
        canvasHeight = canvasHeight * scale;
    }

    canvas.width = canvasWidth;
    canvas.height = canvasHeight;

    state.cropData.scale = canvasWidth / img.width;

    // Desenha imagem
    ctx.drawImage(img, 0, 0, canvasWidth, canvasHeight);
}

function startCropIntegracao(e) {
    const rect = elements.cropCanvasIntegracao.getBoundingClientRect();
    state.cropData.startX = e.clientX - rect.left;
    state.cropData.startY = e.clientY - rect.top;
    state.cropData.isDragging = true;
}

function updateCropIntegracao(e) {
    if (!state.cropData.isDragging) return;

    const rect = elements.cropCanvasIntegracao.getBoundingClientRect();
    state.cropData.endX = e.clientX - rect.left;
    state.cropData.endY = e.clientY - rect.top;

    redrawCropIntegracao();
    updateCropInfoIntegracao();
}

function endCropIntegracao() {
    state.cropData.isDragging = false;
}

function handleTouchStartIntegracao(e) {
    e.preventDefault();
    const touch = e.touches[0];
    const rect = elements.cropCanvasIntegracao.getBoundingClientRect();
    state.cropData.startX = touch.clientX - rect.left;
    state.cropData.startY = touch.clientY - rect.top;
    state.cropData.isDragging = true;
}

function handleTouchMoveIntegracao(e) {
    e.preventDefault();
    if (!state.cropData.isDragging) return;

    const touch = e.touches[0];
    const rect = elements.cropCanvasIntegracao.getBoundingClientRect();
    state.cropData.endX = touch.clientX - rect.left;
    state.cropData.endY = touch.clientY - rect.top;

    redrawCropIntegracao();
    updateCropInfoIntegracao();
}

function redrawCropIntegracao() {
    const canvas = elements.cropCanvasIntegracao;
    const ctx = canvas.getContext('2d');
    const img = state.cropData.image;

    // Limpa e redesenha imagem
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

    // Desenha seleção
    if (state.cropData.endX && state.cropData.endY) {
        const x = Math.min(state.cropData.startX, state.cropData.endX);
        const y = Math.min(state.cropData.startY, state.cropData.endY);
        const width = Math.abs(state.cropData.endX - state.cropData.startX);
        const height = Math.abs(state.cropData.endY - state.cropData.startY);

        // Overlay escuro
        ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Área selecionada clara
        ctx.clearRect(x, y, width, height);
        ctx.drawImage(img,
            x / state.cropData.scale, y / state.cropData.scale,
            width / state.cropData.scale, height / state.cropData.scale,
            x, y, width, height);

        // Borda da seleção
        ctx.strokeStyle = '#007bff';
        ctx.lineWidth = 2;
        ctx.strokeRect(x, y, width, height);
    }
}

function updateCropInfoIntegracao() {
    if (!state.cropData.endX || !state.cropData.endY) return;

    const width = Math.abs(state.cropData.endX - state.cropData.startX) / state.cropData.scale;
    const height = Math.abs(state.cropData.endY - state.cropData.startY) / state.cropData.scale;

    const widthCm = width * 0.026458333;
    const heightCm = height * 0.026458333;
    const areaCm2 = widthCm * heightCm;
    const megapixels = (width * height) / 1000000;

    elements.cropInfoAreaIntegracao.textContent = areaCm2.toFixed(2);
    elements.cropInfoMPIntegracao.textContent = megapixels.toFixed(2);
    elements.cropInfoSizeIntegracao.textContent = `${Math.round(width)} x ${Math.round(height)}`;
    elements.cropInfoIntegracao.classList.remove('hidden');
}

function resetarSelecaoCropIntegracao() {
    state.cropData.startX = 0;
    state.cropData.startY = 0;
    state.cropData.endX = 0;
    state.cropData.endY = 0;
    state.cropData.isDragging = false;
    elements.cropInfoIntegracao.classList.add('hidden');
    initializeCropCanvasIntegracao();
}

function cancelarCropIntegracao() {
    // Volta para o preview sem salvar
    elements.cropSectionIntegracao.classList.add('hidden');
    elements.photoPreviewIntegracao.classList.remove('hidden');
    elements.cropInfoIntegracao.classList.add('hidden');
}

function confirmarCropIntegracao() {
    if (!state.cropData.endX || !state.cropData.endY) {
        showMessage('Selecione uma área para cortar', 'error');
        return;
    }

    const canvas = elements.cropCanvasIntegracao;
    const img = state.cropData.image;

    const x = Math.min(state.cropData.startX, state.cropData.endX) / state.cropData.scale;
    const y = Math.min(state.cropData.startY, state.cropData.endY) / state.cropData.scale;
    const width = Math.abs(state.cropData.endX - state.cropData.startX) / state.cropData.scale;
    const height = Math.abs(state.cropData.endY - state.cropData.startY) / state.cropData.scale;

    // Cria canvas com imagem cortada
    const cropCanvas = document.createElement('canvas');
    cropCanvas.width = width;
    cropCanvas.height = height;
    const cropCtx = cropCanvas.getContext('2d');
    cropCtx.drawImage(img, x, y, width, height, 0, 0, width, height);

    // Converte para File e atualiza preview
    cropCanvas.toBlob((blob) => {
        const file = new File([blob], 'cropped.jpg', { type: 'image/jpeg' });
        compressAndPreviewImageIntegracao(file);

        // Esconde crop, mostra preview
        elements.cropSectionIntegracao.classList.add('hidden');
        elements.photoPreviewIntegracao.classList.remove('hidden');
        elements.cropInfoIntegracao.classList.add('hidden');

        showMessage('Imagem cortada com sucesso!', 'success');
    }, 'image/jpeg', 0.95);
}

function compressAndPreviewImageIntegracao(file) {
    const reader = new FileReader();
    reader.onload = function (e) {
        const img = new Image();
        img.onload = function () {
            // Atualiza originalPhoto para futuras edições
            state.originalPhoto = img;

            // Preview da imagem
            elements.previewImageIntegracao.src = e.target.result;
            elements.photoPreviewIntegracao.classList.remove('hidden');
            state.currentPhotoFile = file;
            elements.submitBtn.disabled = false;
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
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
function startAmbienteFlow() {
    if (!state.originalPhoto) {
        showMessage('Nenhuma foto disponível para ambiente', 'error');
        return;
    }

    // Mostra tela de configuração
    showScreen(elements.ambienteConfigScreen);
}

function abrirCropParaAmbiente() {
    // Verifica se tem imagem disponível
    if (!state.currentPhotoFile) {
        showMessage('Por favor, selecione uma foto primeiro', 'error');
        return;
    }

    // Captura configuração de fundo
    const fundoSelecionado = document.querySelector('input[name="fundoCavalete"]:checked');
    state.ambienteConfig.fundo = fundoSelecionado ? fundoSelecionado.value : 'claro';
    state.ambienteConfig.tipo = 'cavalete'; // Define tipo como cavalete

    // Ativa modo ambiente
    state.ambienteMode = true;

    // CRITICAL FIX: Reset countertop state to prevent incorrect routing
    state.countertopState.croppedImage = null;
    state.countertopState.selectedType = null;
    state.countertopState.flip = false;

    // Gera ambiente direto com a imagem atual (cropada ou original)
    gerarAmbiente(state.currentPhotoFile);
}

async function gerarAmbiente(imagemCropada) {
    try {
        // Mostra loading overlay e prepara elementos de progresso
        elements.loadingOverlay.classList.remove('hidden');
        elements.loadingMessage.textContent = 'Gerando mockups...';
        elements.loadingSubmessage.textContent = 'Você verá cada imagem assim que ficar pronta';
        elements.progressContainer.classList.remove('hidden');
        elements.progressBar.style.width = '0%';
        elements.progressText.textContent = '0 de 0 mockups prontos';

        // Detecta tipo de ambiente e monta endpoint
        const tipo = state.ambienteConfig.tipo; // 'simples', 'bancada1', 'bancada2', etc.
        let endpoint, formData;

        if (tipo.startsWith('bancada')) {
            // Bancadas 1-8: /api/mockup/bancada1/progressive ... /api/mockup/bancada8/progressive
            const bancadaNum = tipo.replace('bancada', ''); // '1', '2', etc.
            endpoint = `/api/mockup/bancada${bancadaNum}/progressive`;
            formData = new FormData();
            formData.append('imagem', imagemCropada);
            formData.append('flip', state.ambienteConfig.flip || false);
        } else {
            // Cavalete: /api/mockup/gerar/progressive
            endpoint = '/api/mockup/gerar/progressive';
            formData = new FormData();
            formData.append('ImagemCropada', imagemCropada);
            formData.append('TipoCavalete', 'simples');
            formData.append('Fundo', state.ambienteConfig.fundo || 'claro');
        }

        // Inicia requisição SSE com fetch()
        const response = await fetch(`${API_URL}${endpoint}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        if (!response.ok) {
            throw new Error('Erro ao iniciar geração de mockups');
        }

        // Prepara galeria
        const gallery = document.getElementById('ambientesGallery');
        gallery.innerHTML = '';
        state.ambienteUrls = [];

        // Labels para cada tipo
        const getLabels = (tipoAmbiente) => {
            if (tipoAmbiente === 'bancada1') {
                return ['Bancada #1 - Normal', 'Bancada #1 - Rotacionado 180°'];
            } else if (tipoAmbiente.startsWith('bancada')) {
                const num = tipoAmbiente.replace('bancada', '');
                return [`Bancada #${num} - Variação 1`, `Bancada #${num} - Variação 2`, `Bancada #${num} - Variação 3`, `Bancada #${num} - Variação 4`];
            } else {
                return [
                    'Cavalete Duplo - Original/Espelho',
                    'Cavalete Duplo - Espelho/Original',
                    'Cavalete Simples'
                ];
            }
        };

        const labels = getLabels(tipo);

        // Lê stream SSE usando ReadableStream
        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';
        let mockupCount = 0;
        let totalMockups = 0;

        while (true) {
            const { done, value } = await reader.read();

            if (done) break;

            buffer += decoder.decode(value, { stream: true });

            // Processa linhas completas (eventos SSE)
            const lines = buffer.split('\n');
            buffer = lines.pop() || ''; // Última linha pode estar incompleta

            for (const line of lines) {
                if (!line.startsWith('data: ')) continue;

                const jsonStr = line.substring(6); // Remove "data: "
                if (!jsonStr.trim()) continue;

                try {
                    const event = JSON.parse(jsonStr);

                    if (event.type === 'start') {
                        console.log('📌 SSE: Iniciando geração...', event.data);
                        elements.loadingMessage.textContent = event.data.mensagem || 'Gerando mockups...';
                    }
                    else if (event.type === 'progress') {
                        totalMockups = event.data.total;
                        const currentIndex = event.data.index;
                        console.log(`🔄 SSE: Progresso ${currentIndex + 1}/${totalMockups}`);
                        elements.loadingMessage.textContent = event.data.mensagem || `Gerando mockup ${currentIndex + 1}/${totalMockups}...`;
                    }
                    else if (event.type === 'mockup') {
                        mockupCount++;
                        totalMockups = event.data.total;
                        const percentage = Math.round((mockupCount / totalMockups) * 100);
                        elements.progressBar.style.width = `${percentage}%`;
                        elements.progressText.textContent = `${mockupCount} de ${totalMockups} mockups prontos`;

                        console.log(`✅ SSE: Mockup ${mockupCount}/${totalMockups} pronto!`, event.data.url);

                        // Adiciona mockup na galeria IMEDIATAMENTE
                        const ambienteUrl = `${API_URL}${event.data.url}`;
                        state.ambienteUrls.push(ambienteUrl);

                        const ambienteItem = document.createElement('div');
                        ambienteItem.className = 'ambiente-item';
                        ambienteItem.innerHTML = `
                            <h3>${labels[event.data.index] || `Mockup ${event.data.index + 1}`}</h3>
                            <img src="${ambienteUrl}" alt="${labels[event.data.index]}">
                            <div class="ambiente-actions">
                                <button class="btn btn-secondary btn-download-single" data-url="${ambienteUrl}" data-nome="${labels[event.data.index] || `Mockup ${event.data.index + 1}`}">
                                    ⬇️ Baixar
                                </button>
                                <button class="btn btn-primary btn-share-single" data-url="${ambienteUrl}" data-nome="${labels[event.data.index] || `Mockup ${event.data.index + 1}`}">
                                    📤 Compartilhar
                                </button>
                            </div>
                        `;
                        gallery.appendChild(ambienteItem);

                        // Mostra tela de resultado após primeiro mockup
                        if (mockupCount === 1) {
                            showScreen(elements.ambienteResultScreen);
                        }
                    }
                    else if (event.type === 'done') {
                        console.log('🎉 SSE: Todos os mockups foram gerados!', event.data);
                        elements.loadingMessage.textContent = event.data.mensagem || 'Mockups gerados com sucesso!';
                        elements.progressBar.style.width = '100%';
                        elements.progressText.textContent = `${totalMockups} de ${totalMockups} mockups prontos`;
                        showAmbienteMessage(event.data.mensagem || 'Mockups gerados!', 'success');
                    }
                    else if (event.type === 'error') {
                        console.error('❌ SSE: Erro!', event.data);
                        throw new Error(event.data.mensagem || 'Erro ao gerar mockup');
                    }
                } catch (parseError) {
                    console.warn('⚠️ Erro ao parsear evento SSE:', jsonStr, parseError);
                }
            }
        }

        // Reseta modo ambiente
        state.ambienteMode = false;

    } catch (error) {
        console.error('❌ Erro ao gerar ambiente:', error);
        showAmbienteMessage(error.message || 'Erro ao gerar mockups', 'error');
        state.ambienteMode = false;
        showMainScreen();
    } finally {
        // Esconde loading overlay e reseta progresso
        elements.loadingOverlay.classList.add('hidden');
        elements.progressContainer.classList.add('hidden');
        elements.progressBar.style.width = '0%';
    }
}

function downloadAmbiente(url, nome) {
    const link = document.createElement('a');
    link.href = url;
    link.download = nome || `ambiente_${Date.now()}.jpg`;
    link.click();
}

/**
 * Compartilha ambiente via Web Share API nativa
 */
async function shareAmbiente(url, nome) {
    try {
        // Tenta usar Web Share API (funciona em mobile e alguns browsers desktop)
        if (navigator.share) {
            // Faz fetch da imagem e converte para blob para compartilhar
            const response = await fetch(url);
            const blob = await response.blob();
            const file = new File([blob], `${nome}.jpg`, { type: 'image/jpeg' });

            await navigator.share({
                title: 'PicStone Mobile',
                text: 'Make with PicStone® mobile',
                files: [file]
            });

            showAmbienteMessage('Compartilhado com sucesso!', 'success');
        } else {
            // Fallback: Compartilhar via WhatsApp Web
            const texto = encodeURIComponent('Make with PicStone® mobile');
            const whatsappUrl = `https://wa.me/?text=${texto}`;
            window.open(whatsappUrl, '_blank');
            showAmbienteMessage('Abrindo WhatsApp...', 'success');
        }
    } catch (error) {
        // Se usuário cancelar ou der erro
        if (error.name !== 'AbortError') {
            console.error('Erro ao compartilhar:', error);

            // Último fallback: copiar link para clipboard
            try {
                await navigator.clipboard.writeText(url);
                showAmbienteMessage('Link copiado! Cole no WhatsApp', 'success');
            } catch (clipError) {
                showAmbienteMessage('Erro ao compartilhar', 'error');
            }
        }
    }
}

function downloadAllAmbientes() {
    if (!state.ambienteUrls || state.ambienteUrls.length === 0) {
        showAmbienteMessage('Nenhum ambiente disponível para download', 'error');
        return;
    }

    state.ambienteUrls.forEach((url, index) => {
        setTimeout(() => {
            const link = document.createElement('a');
            link.href = url;
            link.download = `ambiente_${index + 1}_${Date.now()}.jpg`;
            link.click();
        }, index * 500); // Delay de 500ms entre cada download
    });

    showAmbienteMessage('Baixando todos os ambientes...', 'success');
}

function showAmbienteMessage(message, type) {
    elements.ambienteMessage.textContent = message;
    elements.ambienteMessage.className = `message ${type}`;
    elements.ambienteMessage.classList.remove('hidden');

    setTimeout(() => {
        elements.ambienteMessage.classList.add('hidden');
    }, 5000);
}

// ========== BANCADA1 MOCKUP FLOW ==========
// ========== UNIFIED COUNTERTOP FLOW ==========

/**
 * Passo 1: Inicia o flow de countertop - mostra tela de seleção de tipo
 */
function startCountertopFlow() {
    if (!state.currentPhotoFile) {
        showMessage('Por favor, selecione uma foto primeiro', 'error');
        return;
    }

    // Limpa estado anterior de countertop
    state.countertopState.croppedImage = null;
    state.countertopState.selectedType = null;
    state.countertopState.flip = false;

    // Marca que estamos no flow de countertop
    state.ambienteConfig.tipo = 'countertop';
    state.ambienteMode = false;

    // Salva a imagem atual (cropada ou original) no state de countertop
    state.countertopState.croppedImage = state.currentPhotoFile;

    // Vai direto para seleção de tipo de bancada
    showScreen(elements.countertopSelectionScreen);

    // Reset checkbox de flip
    if (elements.flipCountertop) {
        elements.flipCountertop.checked = false;
    }
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

    // Gera ambiente
    await generateCountertopAmbiente();
}

/**
 * Passo 4: Gera o ambiente da bancada selecionada
 */
async function generateCountertopAmbiente() {
    try {
        // Mostra loading overlay global
        elements.loadingOverlay.classList.remove('hidden');

        const formData = new FormData();
        formData.append('imagem', state.countertopState.croppedImage, 'cropped.jpg');
        formData.append('flip', state.countertopState.flip);

        // Suporta bancada1 até bancada8
        const endpoint = `/api/mockup/${state.countertopState.selectedType}`;

        const response = await fetch(`${API_URL}${endpoint}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        // Verifica se há conteúdo antes de parsear JSON
        const contentType = response.headers.get('content-type');
        let data = null;

        if (contentType && contentType.includes('application/json')) {
            const text = await response.text();
            if (text) {
                data = JSON.parse(text);
            }
        }

        if (!response.ok) {
            const errorMsg = data?.mensagem ||
                           `Erro ${response.status}: ${response.statusText || 'Falha ao gerar ambiente'}`;
            throw new Error(errorMsg);
        }

        // Exibe resultado
        displayCountertopResults(data);

    } catch (error) {
        console.error('Erro ao gerar bancada:', error);
        showAmbienteMessage(error.message, 'error');
        showMainScreen();
    } finally {
        // Esconde loading overlay
        elements.loadingOverlay.classList.add('hidden');
        // Limpa flag de countertop
        state.ambienteConfig.tipo = 'simples';
    }
}

/**
 * Passo 5: Exibe resultados com opção de tentar outra bancada
 */
function displayCountertopResults(data) {
    const caminhos = data.ambientes;

    if (!caminhos || caminhos.length === 0) {
        showMessage('Nenhum ambiente foi gerado', 'error');
        return;
    }

    const gallery = elements.ambientesGallery;
    gallery.innerHTML = '';

    // Mapeia o tipo de bancada para o número correto
    const bancadaLabels = {
        'bancada1': ['Bancada #1 - Normal', 'Bancada #1 - Rotacionado 180°'],
        'bancada2': ['Bancada #2 - Normal', 'Bancada #2 - Rotacionado 180°'],
        'bancada3': ['Bancada #3 - Normal', 'Bancada #3 - Rotacionado 180°'],
        'bancada4': ['Bancada #4 - Normal', 'Bancada #4 - Rotacionado 180°'],
        'bancada5': ['Bancada #5 - Normal', 'Bancada #5 - Rotacionado 180°'],
        'bancada6': ['Bancada #6 - Normal', 'Bancada #6 - Rotacionado 180°'],
        'bancada7': ['Bancada #7 - Normal', 'Bancada #7 - Rotacionado 180°'],
        'bancada8': ['Bancada #8 - Normal', 'Bancada #8 - Rotacionado 180°']
    };

    const labels = bancadaLabels[state.countertopState.selectedType] ||
                   ['Bancada - Normal', 'Bancada - Rotacionado 180°'];

    caminhos.forEach((caminho, index) => {
        const ambienteUrl = `${API_URL}${caminho}`;
        const ambienteItem = document.createElement('div');
        ambienteItem.className = 'ambiente-item';
        ambienteItem.innerHTML = `
            <h3>${labels[index]}</h3>
            <img src="${ambienteUrl}" alt="${labels[index]}">
            <div class="ambiente-actions">
                <button class="btn btn-secondary btn-download-single" data-url="${ambienteUrl}" data-nome="${caminho}">
                    ⬇️ Baixar
                </button>
                <button class="btn btn-primary btn-share-single" data-url="${ambienteUrl}" data-nome="${labels[index]}">
                    📤 Compartilhar
                </button>
            </div>
        `;
        gallery.appendChild(ambienteItem);
    });

    // Salva URLs para download em lote
    state.ambienteUrls = caminhos.map(c => `${API_URL}${c}`);

    // Modifica botão "Novo Ambiente" para permitir tentar outra bancada
    elements.newAmbienteBtn.textContent = '🔄 Tentar Outra Bancada (Mesmo Crop)';
    elements.newAmbienteBtn.onclick = () => {
        // Retorna para seleção com o mesmo crop
        showScreen(elements.countertopSelectionScreen);
    };

    // Mostra tela de resultado
    showScreen(elements.ambienteResultScreen);
    showAmbienteMessage(data.mensagem, 'success');
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
    elements.usersManagementTableBody.innerHTML = '<tr><td colspan="6" class="loading">Carregando...</td></tr>';

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
        allUsersManagementData = usuarios; // Armazena dados para alternância

        if (usuarios.length === 0) {
            elements.usersList.innerHTML = '<p class="empty">Nenhum usuário encontrado</p>';
            elements.usersManagementTableBody.innerHTML = '<tr><td colspan="6" class="empty">Nenhum usuário encontrado</td></tr>';
            return;
        }

        // Renderiza na visualização atual
        if (currentUsersViewMode === 'cards') {
            renderUsersCards(usuarios);
        } else {
            renderUsersTable(usuarios);
        }
    } catch (error) {
        console.error('Erro ao carregar usuários:', error);
        elements.usersList.innerHTML = '<p class="error">Erro ao carregar usuários</p>';
        elements.usersManagementTableBody.innerHTML = '<tr><td colspan="6" class="error">Erro ao carregar usuários</td></tr>';
    }
}

/**
 * Renderiza usuários em cards
 */
function renderUsersCards(users) {
    elements.usersList.innerHTML = users.map(user => `
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
                        <button class="btn btn-primary btn-reactivate-user" data-user-id="${user.id}" data-user-name="${user.nomeCompleto}">
                            Reativar
                        </button>
                    `}
                ` : '<span class="admin-badge">Administrador</span>'}
            </div>
        </div>
    `).join('');
}

/**
 * Renderiza usuários em tabela
 */
function renderUsersTable(users) {
    if (!elements.usersManagementTableBody) {
        return;
    }

    const html = users.map(user => {
        const dataCriacao = new Date(user.dataCriacao).toLocaleDateString('pt-BR');
        const dataExpiracao = user.dataExpiracao
            ? new Date(user.dataExpiracao).toLocaleDateString('pt-BR')
            : 'Sem expiração';
        const status = user.ativo ? 'Ativo' : 'Inativo';
        const statusClass = user.ativo ? 'active' : 'inactive';

        return `
            <tr class="${!user.ativo ? 'inactive' : ''}">
                <td data-label="Nome"><strong>${user.nomeCompleto}</strong></td>
                <td data-label="Email">@${user.username}</td>
                <td data-label="Data Criação">${dataCriacao}</td>
                <td data-label="Data Expiração">${dataExpiracao}</td>
                <td data-label="Status"><span class="user-status ${statusClass}">${user.ativo ? '●' : '○'} ${status}</span></td>
                <td data-label="Ações">
                    ${user.username !== 'admin' ? `
                        ${user.ativo ? `
                            <button class="btn btn-small btn-secondary btn-deactivate-user" data-user-id="${user.id}">
                                Desativar
                            </button>
                        ` : `
                            <button class="btn btn-small btn-primary btn-reactivate-user" data-user-id="${user.id}" data-user-name="${user.nomeCompleto}">
                                Reativar
                            </button>
                        `}
                    ` : '<span class="admin-badge">Admin</span>'}
                </td>
            </tr>
        `;
    }).join('');

    elements.usersManagementTableBody.innerHTML = html;
}

/**
 * Alterna entre visualização em cards e tabela
 */
function switchUsersViewMode(mode) {
    currentUsersViewMode = mode;

    // Atualiza estado dos botões e visualizações
    if (mode === 'cards') {
        elements.usersCardViewBtn.classList.add('active');
        elements.usersTableViewBtn.classList.remove('active');
        elements.usersList.classList.remove('hidden');
        elements.usersTable.classList.remove('active');
    } else {
        elements.usersTableViewBtn.classList.add('active');
        elements.usersCardViewBtn.classList.remove('active');
        elements.usersList.classList.add('hidden');
        elements.usersTable.classList.add('active');
    }

    // Re-renderiza com os dados atuais
    if (allUsersManagementData && allUsersManagementData.length > 0) {
        if (mode === 'cards') {
            renderUsersCards(allUsersManagementData);
        } else {
            renderUsersTable(allUsersManagementData);
        }
    } else {
        // Se não há dados, recarrega
        loadUsers();
    }
}

/**
 * Filtra usuários baseado no texto de busca
 */
function filterUsersManagement(searchText) {
    if (!allUsersManagementData || allUsersManagementData.length === 0) {
        return;
    }

    const search = searchText.toLowerCase().trim();

    // Se não há texto de busca, mostra todos
    if (!search) {
        if (currentUsersViewMode === 'cards') {
            renderUsersCards(allUsersManagementData);
        } else {
            renderUsersTable(allUsersManagementData);
        }
        return;
    }

    // Filtra usuários por nome ou email
    const filtered = allUsersManagementData.filter(user => {
        const nome = (user.nomeCompleto || '').toLowerCase();
        const email = (user.username || '').toLowerCase();
        return nome.includes(search) || email.includes(search);
    });

    // Renderiza lista filtrada
    if (currentUsersViewMode === 'cards') {
        renderUsersCards(filtered);
    } else {
        renderUsersTable(filtered);
    }

    // Se não encontrou nenhum usuário
    if (filtered.length === 0) {
        if (currentUsersViewMode === 'cards') {
            elements.usersList.innerHTML = '<p class="empty">Nenhum usuário encontrado</p>';
        } else {
            elements.usersManagementTableBody.innerHTML = '<tr><td colspan="6" class="empty">Nenhum usuário encontrado</td></tr>';
        }
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
 * Reativa usuário (apenas admin) - Mostra modal
 */
async function reactivateUser(userId, userName) {
    // Referência aos elementos do modal
    const modal = document.getElementById('reactivateModal');
    const userNameDisplay = document.getElementById('reactivateUserName');
    const dataExpiracaoInput = document.getElementById('dataExpiracaoReativar');
    const confirmBtn = document.getElementById('confirmReactivateBtn');
    const cancelBtn = document.getElementById('cancelReactivateBtn');

    // Define nome do usuário
    userNameDisplay.textContent = `Usuário: ${userName}`;

    // Limpa campo de data
    dataExpiracaoInput.value = '';

    // Mostra modal
    modal.classList.remove('hidden');

    // Remove event listeners anteriores (evita duplicação)
    const newConfirmBtn = confirmBtn.cloneNode(true);
    const newCancelBtn = cancelBtn.cloneNode(true);
    confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);
    cancelBtn.parentNode.replaceChild(newCancelBtn, cancelBtn);

    // Handler para confirmar reativação
    newConfirmBtn.addEventListener('click', async () => {
        try {
            // Converte data para ISO format completo ou null
            let dataExpiracao = null;
            if (dataExpiracaoInput.value) {
                const data = new Date(dataExpiracaoInput.value + 'T00:00:00');
                if (!isNaN(data.getTime())) {
                    dataExpiracao = data.toISOString();
                }
            }

            const response = await fetch(`${API_URL}/api/auth/users/${userId}/reactivate`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${state.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ dataExpiracao })
            });

            if (response.ok) {
                const data = await response.json();
                alert(data.mensagem || 'Usuário reativado com sucesso!');
                modal.classList.add('hidden');
                await loadUsers(); // Recarrega lista
            } else {
                const data = await response.json();
                alert(data.mensagem || 'Erro ao reativar usuário');
            }
        } catch (error) {
            console.error('Erro ao reativar usuário:', error);
            alert('Erro ao reativar usuário');
        }
    });

    // Handler para cancelar
    newCancelBtn.addEventListener('click', () => {
        modal.classList.add('hidden');
    });

    // Fecha modal ao clicar fora
    modal.addEventListener('click', (e) => {
        if (e.target === modal) {
            modal.classList.add('hidden');
        }
    });
}

// ========== BANNER DE EXPIRAÇÃO ==========
function mostrarBannerExpiracao(diasRestantes, dataExpiracao) {
    const banner = document.getElementById('expirationBanner');
    if (!banner) return;

    const mensagemElement = document.getElementById('expirationMessage');
    const diasElement = document.getElementById('expirationDays');

    // Formata a data de expiração
    let dataFormatada = '';
    if (dataExpiracao) {
        const data = new Date(dataExpiracao);
        dataFormatada = data.toLocaleDateString('pt-BR');
    }

    // Define a mensagem baseada nos dias restantes
    let mensagem = '';
    if (diasRestantes === 1) {
        mensagem = `Seu acesso expira AMANHÃ (${dataFormatada}). Entre em contato com o administrador para renovar.`;
    } else {
        mensagem = `Seu acesso expira em ${diasRestantes} dias (${dataFormatada}). Entre em contato com o administrador para renovar.`;
    }

    mensagemElement.textContent = mensagem;
    diasElement.textContent = diasRestantes;

    // Altera cor do banner conforme urgência
    if (diasRestantes <= 2) {
        banner.classList.add('urgent'); // Vermelho
    } else {
        banner.classList.remove('urgent'); // Laranja
    }

    banner.classList.remove('hidden');
}

function esconderBannerExpiracao() {
    const banner = document.getElementById('expirationBanner');
    if (banner) {
        banner.classList.add('hidden');
    }
}

function fecharBannerExpiracao() {
    esconderBannerExpiracao();
}

// ========== VERSÃO DA APLICAÇÃO ==========
async function loadAppVersion() {
    try {
        const response = await fetch('/api/version');
        if (response.ok) {
            const data = await response.json();
            const versionElement = document.getElementById('appVersion');
            if (versionElement) {
                versionElement.textContent = `v${data.version}`;
            }
        }
    } catch (error) {
        console.error('Erro ao carregar versão:', error);
        const versionElement = document.getElementById('appVersion');
        if (versionElement) {
            versionElement.textContent = 'v1.0000';
        }
    }
}

// Carrega versão quando página termina de carregar
window.addEventListener('DOMContentLoaded', () => {
    loadAppVersion();
});

// ========== SERVICE WORKER (para PWA futuro) ==========
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        // Descomentado quando houver service-worker.js
        // navigator.serviceWorker.register('/service-worker.js');
    });
}
