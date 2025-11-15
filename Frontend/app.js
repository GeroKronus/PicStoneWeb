// Configuração da API
const API_URL = window.location.origin;

// 🔍 DEBUG SYSTEM - Sistema de debug visual e detalhado
const DEBUG_MODE = false;  // ✅ DESATIVADO - Bug resolvido!
let globalClickLock = false;  // 🔒 Lock para prevenir cliques múltiplos
const debugState = {
    clickCount: 0,
    activationCount: 0,
    dragStartCount: 0,
    dragEndCount: 0,
    lockCount: 0
};

function debugLog(label, data = {}) {
    if (!DEBUG_MODE) return;
    const timestamp = new Date().toISOString().split('T')[1].split('.')[0];
    console.log(`🔍 [${timestamp}] ${label}`, data);
    updateDebugDisplay();
}

function updateDebugDisplay() {
    if (!DEBUG_MODE) return;
    let debugDiv = document.getElementById('debugDisplay');
    if (!debugDiv) {
        debugDiv = document.createElement('div');
        debugDiv.id = 'debugDisplay';
        debugDiv.style.cssText = 'position:fixed;bottom:10px;left:10px;background:rgba(0,0,0,0.9);color:#0f0;padding:10px;font-family:monospace;font-size:11px;z-index:99999;border:2px solid #0f0;border-radius:5px;max-width:300px;pointer-events:auto !important;';
        document.body.appendChild(debugDiv);
    }
    debugDiv.innerHTML = `
        <div style="color:#ff0;font-weight:bold;margin-bottom:5px;">🔍 CROP DEBUG MONITOR</div>
        <div>Cliques: ${debugState.clickCount}</div>
        <div>Ativações: ${debugState.activationCount}</div>
        <div>Drag Start: ${debugState.dragStartCount}</div>
        <div>Drag End: ${debugState.dragEndCount}</div>
        <div>Locks: ${debugState.lockCount}</div>
        <div style="color:${globalClickLock ? '#f00' : '#0f0'};font-weight:bold;margin-top:5px;">
            Status: ${globalClickLock ? '🔒 LOCKED' : '✅ UNLOCKED'}
        </div>
        <button id="emergencyUnlockBtn" style="width:100%;margin-top:10px;padding:8px;background:#f00;color:#fff;border:none;border-radius:5px;font-weight:bold;cursor:pointer;pointer-events:auto !important;font-size:12px;">
            🚨 DESTRAVAR TUDO
        </button>
    `;

    // Adicionar event listener ao botão de emergência
    const emergencyBtn = document.getElementById('emergencyUnlockBtn');
    if (emergencyBtn && !emergencyBtn.hasAttribute('data-listener-added')) {
        emergencyBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            console.error('🚨 BOTÃO DE EMERGÊNCIA ACIONADO! Destravando tudo...');

            // Reseta estado de crop
            state.cropOverlayState.isActive = false;
            state.cropOverlayState.isActivating = false;
            state.cropOverlayState.isDragging = false;

            // Oculta todos os canvas de crop
            document.querySelectorAll('canvas[id*="cropOverlay"]').forEach(canvas => {
                canvas.classList.add('hidden');
            });

            // Mostra todos os botões de crop
            document.querySelectorAll('button[id*="adjustImageBtn"]').forEach(btn => {
                btn.classList.remove('hidden');
            });

            // ✨ RESET COMPLETO: Volta para a tela principal
            document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
            const mainScreen = document.getElementById('mainScreen');
            if (mainScreen) {
                mainScreen.classList.add('active');
            }

            console.log('✅ Sistema resetado - voltou para tela principal');
            alert('✅ Sistema destravado! Voltando para tela principal...');
        });
        emergencyBtn.setAttribute('data-listener-added', 'true');
    }
}

// ✨ REMOVIDO COMPLETAMENTE: Sistema de lockClicks causava mais problemas que soluções
// Proteções suficientes via: botão oculto + isActivating + isDragging + listener cleanup

/**
 * Converte Base64 para Blob
 * @param {string} base64 - String Base64 (com ou sem prefixo data:image/...)
 * @param {string} mimeType - Tipo MIME (padrão: image/jpeg)
 * @returns {Blob} Blob da imagem
 */
function base64ToBlob(base64, mimeType = 'image/jpeg') {
    // Remove prefixo se existir (data:image/jpeg;base64,...)
    const base64Data = base64.includes(',') ? base64.split(',')[1] : base64;

    // Decodifica Base64
    const byteCharacters = atob(base64Data);
    const byteArrays = [];

    for (let offset = 0; offset < byteCharacters.length; offset += 512) {
        const slice = byteCharacters.slice(offset, offset + 512);
        const byteNumbers = new Array(slice.length);

        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }

        byteArrays.push(new Uint8Array(byteNumbers));
    }

    return new Blob(byteArrays, { type: mimeType });
}

// Estado da aplicação
const state = {
    token: localStorage.getItem('token') || null,
    username: localStorage.getItem('username') || null,
    currentPhoto: null,
    currentPhotoFile: null,
    uploadedImageId: null, // ID da imagem armazenada no servidor
    uploadInProgress: false, // ✨ FIX: Flag para indicar upload em andamento
    isGeneratingMockup: false, // ✨ FIX: Flag para prevenir cliques duplos ao gerar mockups
    // ✨ NOVA ARQUITETURA: Coordenadas de crop (enviadas ao servidor ao invés de arquivo)
    cropCoordinates: null, // { x, y, width, height } ou null se não tem crop
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
    // Estado específico para banheiros (bathrooms) - DRY com countertopState
    bathroomState: {
        selectedType: null      // 'banho1' ou 'banho2'
    },
    // Estado para crop overlay na Integração
    cropOverlayState: (() => {
        let _isActive = false;
        const obj = {
            get isActive() {
                return _isActive;
            },
            set isActive(value) {
                const stack = new Error().stack;
                debugLog(`🔄 isActive mudando de ${_isActive} para ${value}`, {
                    stack: stack.split('\n').slice(2, 4).join('\n')  // Mostra caller
                });
                _isActive = value;
            },
            isActivating: false, // ✨ FIX: Previne cliques duplos no botão de crop
            isDragging: false,
            startX: 0,
            startY: 0,
            endX: 0,
            endY: 0,
            originalImageSrc: null,
            canvasRect: null
        };
        return obj;
    })(),
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
    bathroomsBtn: document.getElementById('bathroomsBtn'),
    countertopSelectionScreen: document.getElementById('countertopSelectionScreen'),
    cancelCountertopSelectionBtn: document.getElementById('cancelCountertopSelectionBtn'),
    bathroomSelectionScreen: document.getElementById('bathroomSelectionScreen'),
    cancelBathroomSelectionBtn: document.getElementById('cancelBathroomSelectionBtn'),
    flipCountertop: document.getElementById('flipCountertop'),
    backToMainBtn: document.getElementById('backToMainBtn'),
    downloadAllAmbientesBtn: document.getElementById('downloadAllAmbientesBtn'),
    modifyCropBtn: document.getElementById('modifyCropBtn'),
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
    elements.clearPhotoBtnIntegracao.addEventListener('click', clearPhotoIntegracao);
    elements.adjustImageBtnIntegracao.addEventListener('click', ativarCropOverlayIntegracao);
    elements.resetImageBtnIntegracao.addEventListener('click', resetarParaOriginalIntegracao);

    // Crop Overlay na Integração - mousedown no canvas, move/up no document
    elements.cropOverlayIntegracao.addEventListener('mousedown', iniciarSelecaoCrop);
    elements.cropOverlayIntegracao.addEventListener('touchstart', iniciarSelecaoCropTouch, { passive: false });

    // Ambientes - Captura de foto
    elements.captureBtnAmbientes.addEventListener('click', () => elements.fileInputAmbientes.click());
    elements.fileInputAmbientes.addEventListener('change', handleFileSelectAmbientes);
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
    elements.bathroomsBtn.addEventListener('click', startBathroomsFlow);
    elements.cancelAmbienteBtn.addEventListener('click', () => showMainScreen());
    elements.cancelCountertopSelectionBtn.addEventListener('click', backToAmbientesWithPhoto);
    elements.cancelBathroomSelectionBtn.addEventListener('click', backToAmbientesWithPhoto);
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
            const type = preview.dataset.type;
            // 🔧 FIX: Ignora se for bathroom (será tratado pelo listener específico)
            if (type.startsWith('banho')) {
                return;
            }
            // Verifica se o card pai está desabilitado
            const card = preview.closest('.countertop-card');
            if (card && card.classList.contains('disabled')) {
                return; // Ignora clique em cards desabilitados
            }
            selectCountertopAndGenerate(type);
        }
    });

    // Event delegation para seleção de bathroom via click no thumb
    document.addEventListener('click', (e) => {
        const preview = e.target.closest('.countertop-preview');
        if (preview && preview.dataset.type) {
            const type = preview.dataset.type;
            // Verifica se é um bathroom (banho1, banho2, etc)
            if (type.startsWith('banho')) {
                // Verifica se o card pai está desabilitado
                const card = preview.closest('.countertop-card');
                if (card && card.classList.contains('disabled')) {
                    return; // Ignora clique em cards desabilitados
                }
                selectBathroomAndGenerate(type);
            }
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
 * DRY: Verifica o flow atual e volta para tela correta
 */
function handleBackFromResults() {
    if (state.countertopState.croppedImage) {
        // Está no flow de countertop: volta para seleção de bancadas
        showScreen(elements.countertopSelectionScreen);
    } else if (state.bathroomState.selectedType) {
        // Está no flow de bathroom: volta para seleção de banheiros
        showScreen(elements.bathroomSelectionScreen);
    } else if (state.ambienteConfig.tipo === 'cavalete') {
        // Está no flow de cavalete: volta para ambientes com foto
        backToAmbientesWithPhoto();
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

            // ✅ FIX: Só setar originalImageSrc se há crop ativo (state.cropCoordinates existe)
            // Sem crop: originalImageSrc = null → mostra botão crop
            // Com crop: originalImageSrc = imagem original → mostra botão reverter
            if (state.cropCoordinates) {
                // Tem crop ativo: originalImageSrc recebe a imagem ORIGINAL
                state.cropOverlayState.originalImageSrc = sharedImage.originalImage;
                elements.previewImageAmbientes.src = sharedImage.currentImage; // Exibe cropada
                // Garante que botão reverter está visível
                if (elements.resetImageBtnAmbientes) {
                    elements.resetImageBtnAmbientes.classList.remove('hidden');
                }
                if (elements.adjustImageBtnAmbientes) {
                    elements.adjustImageBtnAmbientes.classList.add('hidden');
                }
            } else {
                // Sem crop: limpa originalImageSrc
                state.cropOverlayState.originalImageSrc = null;
                elements.previewImageAmbientes.src = sharedImage.currentImage; // Exibe original
                // Garante que botão crop está visível
                if (elements.resetImageBtnAmbientes) {
                    elements.resetImageBtnAmbientes.classList.add('hidden');
                }
                if (elements.adjustImageBtnAmbientes) {
                    elements.adjustImageBtnAmbientes.classList.remove('hidden');
                }
            }

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

/**
 * Volta para a tela de Ambientes mantendo a foto e botões visíveis
 * Usado ao clicar "Voltar" na tela de seleção de bancadas
 */
function backToAmbientesWithPhoto() {
    showScreen(elements.ambientesScreen);

    // Garante que a foto e os botões permanecem visíveis
    if (state.currentPhotoFile && elements.previewImageAmbientes.src) {
        elements.photoPreviewAmbientes.classList.remove('hidden');
        elements.ambienteOptions.classList.remove('hidden');
        if (elements.captureSectionAmbientes) {
            elements.captureSectionAmbientes.classList.add('hidden');
        }
    } else {
        // Fallback: se não há foto, limpa tudo
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

// ========== FUNÇÃO ÚNICA DE REDIMENSIONAMENTO (DRY) ==========
function redimensionarImagem(img, fileName) {
    return new Promise((resolve) => {
        const maxWidth = 2000;
        let targetWidth, targetHeight;

        if (img.width > maxWidth) {
            const scale = maxWidth / img.width;
            targetWidth = maxWidth;
            targetHeight = Math.round(img.height * scale);
            console.log(`📐 Redimensionando ${img.width}x${img.height} → ${targetWidth}x${targetHeight} (max ${maxWidth}px)`);
        } else {
            targetWidth = img.width;
            targetHeight = img.height;
            console.log(`📦 Mantendo original: ${img.width}x${img.height} (< ${maxWidth}px)`);
        }

        const canvas = document.createElement('canvas');
        canvas.width = targetWidth;
        canvas.height = targetHeight;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

        const qualidadeJPEG = 0.75;
        canvas.toBlob((blob) => {
            const file = new File([blob], fileName, {
                type: 'image/jpeg',
                lastModified: Date.now()
            });
            const dataUrl = canvas.toDataURL('image/jpeg', 0.85);
            resolve({ file, dataUrl, canvas });
        }, 'image/jpeg', qualidadeJPEG);
    });
}

function compressAndPreviewImage(file) {
    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = async () => {
            const { file: processedFile, dataUrl } = await redimensionarImagem(img, file.name);
            state.currentPhotoFile = processedFile;
            elements.previewImage.src = dataUrl;
            elements.photoPreview.classList.remove('hidden');
            elements.submitBtn.disabled = false;
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
    elements.bathroomsBtn.classList.add('hidden');
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
    elements.bathroomsBtn.classList.add('hidden');
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
    const fileSizeKB = (file.size / 1024).toFixed(0);
    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = async () => {
            const { file: processedFile, dataUrl } = await redimensionarImagem(img, file.name);
            state.currentPhotoFile = processedFile;

            console.log(`📦 Arquivo processado: ${(processedFile.size / 1024).toFixed(0)}KB (original: ${fileSizeKB}KB)`);

            elements.previewImageIntegracao.src = dataUrl;
            elements.photoPreviewIntegracao.classList.remove('hidden');
            elements.submitBtn.disabled = false;

            // ✨ FIX: Garantir que botões estejam no estado inicial ao carregar nova imagem
            // Estado inicial = crop visível, reverter oculto
            if (elements.adjustImageBtnIntegracao) {
                elements.adjustImageBtnIntegracao.classList.remove('hidden');
            }
            if (elements.resetarImagemBtnIntegracao) {
                elements.resetarImagemBtnIntegracao.classList.add('hidden');
            }

            // Salva imagem no estado compartilhado
            const originalImageData = state.originalPhoto ? state.originalPhoto.src : dataUrl;
            saveSharedImage(originalImageData, dataUrl, file.name, processedFile, 'integracao');

            // ✨ NOVO: Faz upload imediato da imagem para o servidor
            await uploadImageToServer(processedFile);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function clearPhotoIntegracao() {
    debugLog('🗑️ clearPhotoIntegracao() CHAMADO', {});

    // ✨ NOVO: Deleta imagem do servidor
    deleteImageFromServer();

    state.currentPhotoFile = null;
    state.originalPhoto = null;
    elements.previewImageIntegracao.src = '';
    elements.photoPreviewIntegracao.classList.add('hidden');
    elements.fileInputIntegracao.value = '';
    elements.submitBtn.disabled = true;
    // Reset crop overlay state
    state.cropOverlayState.isActive = false;
    debugLog('❌ isActive SET TO FALSE (clearPhotoIntegracao)', {});
    state.cropOverlayState.originalImageSrc = null;
    elements.cropOverlayIntegracao.classList.add('hidden');
    elements.resetImageBtnIntegracao.classList.add('hidden');
    // Nota: NÃO limpa estado compartilhado aqui, pois outras telas podem estar usando
}

// ========== INTEGRAÇÃO - CROP OVERLAY ==========

// Função genérica para ativar crop overlay (usada por BookMatch, Ambientes, etc.)
function ativarCropOverlay(imgElement, canvasElement, resetBtnElement, onCropComplete, indicatorElement = null, adjustImageBtn = null) {
    debugState.activationCount++;
    debugLog('🎯 ativarCropOverlay() CHAMADO', {
        isActivating: state.cropOverlayState.isActivating,
        isActive: state.cropOverlayState.isActive,
        hasButton: !!adjustImageBtn
    });

    // ✨ FIX: Previne cliques duplos no botão de crop
    if (state.cropOverlayState.isActivating) {
        debugLog('❌ BLOQUEADO - Já está ativando!', {});
        console.warn('⚠️ Crop overlay já está sendo ativado, ignorando clique duplo');
        return;
    }

    if (!imgElement || !imgElement.src) {
        debugLog('❌ BLOQUEADO - Sem imagem!', {});
        return;
    }

    // ✨ FIX: Marca que está ativando o crop overlay
    state.cropOverlayState.isActivating = true;
    debugLog('✅ Flag isActivating = TRUE', {});

    // Store original image if not already stored
    if (!state.cropOverlayState.originalImageSrc) {
        state.cropOverlayState.originalImageSrc = imgElement.src;
    }

    // Configurar elementos atuais
    state.cropOverlayState.currentCanvas = canvasElement;
    state.cropOverlayState.currentImage = imgElement;
    state.cropOverlayState.currentResetBtn = resetBtnElement;
    state.cropOverlayState.currentIndicator = indicatorElement;
    state.cropOverlayState.currentAdjustBtn = adjustImageBtn; // ✨ FIX: Guardar referência ao botão
    state.cropOverlayState.onCropComplete = onCropComplete;

    // ✨ FIX: Ocultar botão de crop para prevenir cliques múltiplos
    if (adjustImageBtn) {
        adjustImageBtn.classList.add('hidden');
        debugLog('👁️ Botão crop OCULTO', {});
    }

    // ✨ FIX: Ocultar botão reverter (modo original = só crop visível)
    if (resetBtnElement) {
        resetBtnElement.classList.add('hidden');
        debugLog('👁️ Botão reverter OCULTO (modo crop ativo)', {});
    }

    // ✨ REMOVIDO: lockClicks() causava travamento em botões após múltiplos crops
    // Proteção já existe via: botão oculto + isActivating + isDragging + listener cleanup

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
    debugLog('✅ isActive SETADO PARA TRUE', {
        canvasHidden: canvasElement.classList.contains('hidden'),
        canvasWidth: canvasElement.width,
        canvasHeight: canvasElement.height
    });

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

        // ✨ FIX: Libera flag após setup completo do canvas (300ms é suficiente para prevenir cliques rápidos)
        setTimeout(() => {
            state.cropOverlayState.isActivating = false;
        }, 300);
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
            // ✨ NOVA ARQUITETURA: Apenas atualiza preview, mantém arquivo original
            // Coordenadas já foram armazenadas em state.cropCoordinates
            elements.previewImageIntegracao.src = croppedBase64;
        },
        elements.cropIndicatorIntegracao,
        elements.adjustImageBtnIntegracao // ✨ FIX: Passar botão para ocultar
    );
}

// Wrapper para Ambientes
function ativarCropOverlayAmbientes() {
    ativarCropOverlay(
        elements.previewImageAmbientes,
        elements.cropOverlayAmbientes,
        elements.resetImageBtnAmbientes,
        (croppedBase64, croppedFile) => {
            // ✨ NOVA ARQUITETURA: Apenas atualiza preview, mantém arquivo original
            // Coordenadas já foram armazenadas em state.cropCoordinates
            elements.previewImageAmbientes.src = croppedBase64;

            // ✅ FIX: Atualiza sharedImage com imagem cropada para manter sincronia
            if (state.sharedImageState) {
                state.sharedImageState.currentImage = croppedBase64;
                console.log('📸 sharedImage.currentImage atualizado com imagem cropada');
            }
        },
        elements.cropIndicatorAmbientes,
        elements.adjustImageBtnAmbientes // ✨ FIX: Passar botão para ocultar
    );
}

async function resetarParaOriginalAmbientes() {
    if (state.cropOverlayState.originalImageSrc) {
        // ✨ NOVA ARQUITETURA: Apenas limpa coordenadas (original sempre preservada no servidor)
        console.log('🔄 Resetando para imagem original (limpando coordenadas de crop)');
        state.cropCoordinates = null;

        elements.previewImageAmbientes.src = state.cropOverlayState.originalImageSrc;

        // ✅ FIX CRÍTICO: NÃO resetar currentPhotoFile!
        // O arquivo original ainda é válido após reverter o crop (crop só altera coordenadas, não o arquivo)
        // Resetar para null causava validações a falharem em startCountertopFlow() e startBathroomsFlow()
        // state.currentPhotoFile = null; // ❌ REMOVIDO - causava bug após múltiplos crops

        elements.resetImageBtnAmbientes.classList.add('hidden');
        elements.cropOverlayAmbientes.classList.add('hidden');
        elements.cropIndicatorAmbientes.classList.add('hidden');
        state.cropOverlayState.originalImageSrc = null;

        // ✨ FIX: Resetar TODOS os estados do crop overlay para evitar conflitos
        state.cropOverlayState.isActive = false;
        state.cropOverlayState.isActivating = false;

        // ✨ FIX: Mostrar botão de crop novamente ao resetar
        if (elements.adjustImageBtnAmbientes) {
            elements.adjustImageBtnAmbientes.classList.remove('hidden');
        }
    }
}

function iniciarSelecaoCrop(e) {
    debugState.clickCount++;
    debugLog('🖱️ iniciarSelecaoCrop() - Clique no canvas', {
        isActive: state.cropOverlayState.isActive,
        isDragging: state.cropOverlayState.isDragging,
        globalLocked: globalClickLock
    });

    if (!state.cropOverlayState.isActive) {
        debugLog('❌ IGNORADO - Crop overlay não está ativo', {});
        return;
    }

    // ✨ FIX: Previne múltiplas seleções simultâneas
    if (state.cropOverlayState.isDragging) {
        debugLog('❌ BLOQUEADO - Já está dragging!', {});
        console.warn('⚠️ Seleção de crop já em andamento, ignorando clique');
        return;
    }

    e.preventDefault();
    state.cropOverlayState.isDragging = true;
    debugState.dragStartCount++;
    debugLog('✅ Drag iniciado - isDragging = TRUE', {});

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

    // ✨ FIX: Remove listeners antigos antes de adicionar novos (previne duplicação)
    document.removeEventListener('mousemove', atualizarSelecaoCrop);
    document.removeEventListener('mouseup', finalizarEAplicarCrop);

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
    debugLog('🏁 finalizarEAplicarCrop() - Drag FINALIZADO', {
        isDragging: state.cropOverlayState.isDragging
    });

    if (!state.cropOverlayState.isDragging) {
        debugLog('❌ IGNORADO - Não estava dragging', {});
        return;
    }

    e.preventDefault();
    state.cropOverlayState.isDragging = false;
    debugState.dragEndCount++;
    debugLog('✅ isDragging = FALSE', {});

    // Remove document listeners
    debugLog('🗑️ Removendo event listeners', {});
    document.removeEventListener('mousemove', atualizarSelecaoCrop);
    document.removeEventListener('mouseup', finalizarEAplicarCrop);

    const x = Math.min(state.cropOverlayState.startX, state.cropOverlayState.endX);
    const y = Math.min(state.cropOverlayState.startY, state.cropOverlayState.endY);
    const width = Math.abs(state.cropOverlayState.endX - state.cropOverlayState.startX);
    const height = Math.abs(state.cropOverlayState.endY - state.cropOverlayState.startY);

    // Check if selection is valid (minimum 30x30 pixels)
    if (width < 30 || height < 30) {
        debugLog('⚠️ Seleção muito pequena - CANCELANDO', { width, height });
        // Selection too small, just hide overlay
        const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
        canvas.classList.add('hidden');
        state.cropOverlayState.isActive = false;
        debugLog('❌ isActive SET TO FALSE (seleção pequena)', {});

        // ✨ FIX: MUTUAMENTE EXCLUSIVO - Volta ao estado original (apenas crop visível)
        if (state.cropOverlayState.currentAdjustBtn) {
            state.cropOverlayState.currentAdjustBtn.classList.remove('hidden');
            debugLog('👁️ Botão CROP revelado (cancelamento)', {});
        }
        if (state.cropOverlayState.currentResetBtn) {
            state.cropOverlayState.currentResetBtn.classList.add('hidden');
            debugLog('🙈 Botão REVERTER oculto (cancelamento)', {});
        }
        return;
    }

    // Apply crop automatically
    aplicarCropGenerico(x, y, width, height);
}

// Função genérica para aplicar crop (usada por todas as features)
async function aplicarCropGenerico(x, y, width, height) {
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = width;
    tempCanvas.height = height;
    const ctx = tempCanvas.getContext('2d');

    const img = new Image();
    img.onload = () => {
        ctx.drawImage(img, x, y, width, height, 0, 0, width, height);

        const croppedBase64 = tempCanvas.toDataURL('image/jpeg', 0.95);

        // ✨ NOVA ARQUITETURA: Armazena COORDENADAS ao invés de arquivo
        // Servidor fará o crop sob demanda usando a imagem original
        state.cropCoordinates = {
            x: Math.round(x),
            y: Math.round(y),
            width: Math.round(width),
            height: Math.round(height)
        };
        console.log('✂️ Crop aplicado! Coordenadas armazenadas:', state.cropCoordinates);

        // Hide overlay
        const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
        canvas.classList.add('hidden');
        state.cropOverlayState.isActive = false;
        debugLog('❌ isActive SET TO FALSE (aplicarCropGenerico - sucesso)', {});

        // ✨ FIX: MUTUAMENTE EXCLUSIVO - Apenas botão REVERTER visível (crop aplicado)
        // Botão CROP permanece oculto (só volta ao resetar)
        if (state.cropOverlayState.currentResetBtn) {
            state.cropOverlayState.currentResetBtn.classList.remove('hidden');
            debugLog('👁️ Botão REVERTER revelado (crop aplicado)', {});
        }

        // Call callback com preview visual (base64) - SEM arquivo
        if (state.cropOverlayState.onCropComplete) {
            state.cropOverlayState.onCropComplete(croppedBase64, null);
        }
    };
    const imgSrc = state.cropOverlayState.currentImage ? state.cropOverlayState.currentImage.src : elements.previewImageIntegracao.src;
    img.src = imgSrc;
}

function iniciarSelecaoCropTouch(e) {
    if (!state.cropOverlayState.isActive) return;

    // ✨ FIX: Previne múltiplas seleções simultâneas
    if (state.cropOverlayState.isDragging) {
        console.warn('⚠️ Seleção de crop já em andamento, ignorando toque');
        return;
    }

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

    // ✨ FIX: Remove listeners antigos antes de adicionar novos (previne duplicação)
    document.removeEventListener('touchmove', atualizarSelecaoCropTouch);
    document.removeEventListener('touchend', finalizarEAplicarCropTouch);

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

    // Check if selection is valid (minimum 30x30 pixels)
    if (width < 30 || height < 30) {
        debugLog('⚠️ Seleção TOUCH muito pequena - CANCELANDO', { width, height });
        // Selection too small, just hide overlay
        const canvas = state.cropOverlayState.currentCanvas || elements.cropOverlayIntegracao;
        canvas.classList.add('hidden');
        debugLog('🙈 Canvas OCULTO após cancelamento TOUCH', { canvasId: canvas.id });

        state.cropOverlayState.isActive = false;
        debugLog('❌ isActive SET TO FALSE (finalizarEAplicarCropTouch - seleção pequena)', {});

        // ✨ FIX: MUTUAMENTE EXCLUSIVO - Volta ao estado original (apenas crop visível)
        if (state.cropOverlayState.currentAdjustBtn) {
            state.cropOverlayState.currentAdjustBtn.classList.remove('hidden');
            debugLog('👁️ Botão CROP revelado (cancelamento TOUCH)', {});
        }
        if (state.cropOverlayState.currentResetBtn) {
            state.cropOverlayState.currentResetBtn.classList.add('hidden');
            debugLog('🙈 Botão REVERTER oculto (cancelamento TOUCH)', {});
        }
        return;
    }

    // Apply crop automatically
    aplicarCropGenerico(x, y, width, height);
}

async function resetarParaOriginalIntegracao() {
    if (!state.cropOverlayState.originalImageSrc) return;

    // ✨ NOVA ARQUITETURA: Apenas limpa coordenadas (original sempre preservada no servidor)
    console.log('🔄 Resetando para imagem original (limpando coordenadas de crop)');
    state.cropCoordinates = null;

    // Restore original image
    elements.previewImageIntegracao.src = state.cropOverlayState.originalImageSrc;

    // ✅ FIX CRÍTICO: NÃO resetar currentPhotoFile!
    // O arquivo original ainda é válido após reverter o crop (crop só altera coordenadas, não o arquivo)
    // Não é necessário recriar o arquivo via fetch - o original em state.currentPhotoFile permanece válido
    // fetch(state.cropOverlayState.originalImageSrc)
    //     .then(res => res.blob())
    //     .then(blob => {
    //         state.currentPhotoFile = new File([blob], 'original.jpg', {
    //             type: 'image/jpeg',
    //             lastModified: Date.now()
    //         });
    //     });

    // Hide reset button and clear stored original since we're back to original
    elements.resetImageBtnIntegracao.classList.add('hidden');
    state.cropOverlayState.originalImageSrc = null;

    // Also hide the crop overlay if it was visible
    elements.cropOverlayIntegracao.classList.add('hidden');
    state.cropOverlayState.isActive = false;
    state.cropOverlayState.isActivating = false;
    debugLog('❌ isActive e isActivating SET TO FALSE (resetarParaOriginalIntegracao)', {});

    // ✨ FIX: Mostrar botão de crop novamente ao resetar
    if (elements.adjustImageBtnIntegracao) {
        elements.adjustImageBtnIntegracao.classList.remove('hidden');
    }
}

// ========== UPLOAD DE IMAGEM PARA SERVIDOR ==========

// ✨ REMOVIDO: uploadCroppedIfNeeded() - Nova arquitetura envia coordenadas ao invés de arquivo

async function uploadImageToServer(imageFile) {
    try {
        console.log('📤 Fazendo upload da imagem para o servidor...');

        // ✨ FIX: Desabilita cards enquanto upload está em andamento
        state.uploadInProgress = true;
        disableCountertopCards();
        showUploadToast(); // ✨ NOVO: Exibe mensagem de upload em andamento

        const formData = new FormData();
        formData.append('imagem', imageFile);

        const response = await fetch(`${API_URL}/api/image/upload`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        if (!response.ok) {
            throw new Error('Erro ao fazer upload da imagem');
        }

        const result = await response.json();

        if (result.sucesso && result.imageId) {
            state.uploadedImageId = result.imageId;
            console.log(`✅ Imagem enviada para servidor: ${result.imageId}`);
            console.log(`📐 Dimensões: ${result.largura}x${result.altura}`);
        } else {
            console.warn('⚠️ Upload retornou sem imageId');
        }
    } catch (error) {
        console.error('❌ Erro ao fazer upload da imagem:', error);
        // Não bloqueia a UX - o sistema vai usar o fallback (enviar arquivo diretamente)
        state.uploadedImageId = null;
    } finally {
        // ✨ FIX: Reabilita cards após upload (sucesso ou erro)
        state.uploadInProgress = false;
        enableCountertopCards();
        hideUploadToast(); // ✨ NOVO: Esconde mensagem de upload
    }
}

async function deleteImageFromServer() {
    if (!state.uploadedImageId) {
        console.log('ℹ️ Nenhuma imagem para deletar no servidor');
        return;
    }

    try {
        console.log(`🗑️ Deletando imagem do servidor: ${state.uploadedImageId}`);

        const response = await fetch(`${API_URL}/api/image/${state.uploadedImageId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${state.token}`
            }
        });

        if (response.ok) {
            console.log('✅ Imagem deletada do servidor');
        } else {
            console.warn('⚠️ Erro ao deletar imagem do servidor');
        }
    } catch (error) {
        console.error('❌ Erro ao deletar imagem do servidor:', error);
    } finally {
        // Limpa o imageId independente do resultado
        state.uploadedImageId = null;
    }
}

// ✨ FIX: Funções para desabilitar/habilitar cards durante upload
function disableCountertopCards() {
    const cards = document.querySelectorAll('.countertop-card');
    cards.forEach(card => {
        card.classList.add('disabled');
        card.style.opacity = '0.5';
        card.style.pointerEvents = 'none';
    });
    console.log('🔒 Cards desabilitados durante upload');
}

function enableCountertopCards() {
    const cards = document.querySelectorAll('.countertop-card');
    cards.forEach(card => {
        card.classList.remove('disabled');
        card.style.opacity = '1';
        card.style.pointerEvents = 'auto';
    });
    console.log('🔓 Cards habilitados após upload');
}

// ✨ NOVO: Funções para exibir/esconder toast de upload
function showUploadToast() {
    // Cria elemento toast se não existir
    let toast = document.getElementById('upload-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'upload-toast';
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: #2196F3;
            color: white;
            padding: 16px 24px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
            font-size: 16px;
            font-weight: 500;
            z-index: 10000;
            display: flex;
            align-items: center;
            gap: 12px;
            animation: slideDown 0.3s ease-out;
        `;

        // Adiciona ícone de loading
        const spinner = document.createElement('div');
        spinner.style.cssText = `
            width: 20px;
            height: 20px;
            border: 3px solid rgba(255, 255, 255, 0.3);
            border-top-color: white;
            border-radius: 50%;
            animation: spin 1s linear infinite;
        `;

        const text = document.createElement('span');
        text.textContent = 'Enviando imagem. Aguarde';

        toast.appendChild(spinner);
        toast.appendChild(text);
        document.body.appendChild(toast);

        // Adiciona CSS de animações se não existir
        if (!document.getElementById('toast-animations')) {
            const style = document.createElement('style');
            style.id = 'toast-animations';
            style.textContent = `
                @keyframes slideDown {
                    from {
                        transform: translate(-50%, -100%);
                        opacity: 0;
                    }
                    to {
                        transform: translate(-50%, 0);
                        opacity: 1;
                    }
                }
                @keyframes spin {
                    to { transform: rotate(360deg); }
                }
                @keyframes slideUp {
                    from {
                        transform: translate(-50%, 0);
                        opacity: 1;
                    }
                    to {
                        transform: translate(-50%, -100%);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }
    } else {
        toast.style.display = 'flex';
    }

    console.log('🔔 Toast de upload exibido');
}

function hideUploadToast() {
    const toast = document.getElementById('upload-toast');
    if (toast) {
        toast.style.animation = 'slideUp 0.3s ease-out';
        setTimeout(() => {
            toast.style.display = 'none';
            toast.style.animation = 'slideDown 0.3s ease-out';
        }, 300);
    }
    console.log('🔕 Toast de upload escondido');
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
        img.onload = async () => {
            // Salva imagem original para uso nos ambientes
            state.originalPhoto = img;

            const { file: processedFile, dataUrl } = await redimensionarImagem(img, file.name);
            state.currentPhotoFile = processedFile;

            elements.previewImageAmbientes.src = dataUrl;

            // Salva imagem original para crop
            state.cropOverlayState.originalImageSrc = dataUrl;

            elements.photoPreviewAmbientes.classList.remove('hidden');

            // Esconde botão "Escolher/Tirar Foto"
            if (elements.captureSectionAmbientes) {
                elements.captureSectionAmbientes.classList.add('hidden');
            }

            // Mostra opções de ambiente
            elements.ambienteOptions.classList.remove('hidden');

            // ✨ FIX: Garantir que botões estejam no estado inicial ao carregar nova imagem
            // Estado inicial = crop visível, reverter oculto
            if (elements.adjustImageBtnAmbientes) {
                elements.adjustImageBtnAmbientes.classList.remove('hidden');
            }
            if (elements.resetarImagemBtnAmbientes) {
                elements.resetarImagemBtnAmbientes.classList.add('hidden');
            }

            // Salva imagem no estado compartilhado
            const originalImageData = state.originalPhoto ? state.originalPhoto.src : dataUrl;
            saveSharedImage(originalImageData, dataUrl, file.name, processedFile, 'ambientes');

            // ✨ NOVO: Faz upload imediato da imagem para o servidor
            await uploadImageToServer(processedFile);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

function clearPhotoAmbientes() {
    // ✨ NOVO: Deleta imagem do servidor
    deleteImageFromServer();

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
        elements.bathroomsBtn.classList.remove('hidden');

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
    cropCanvas.toBlob(async (blob) => {
        const file = new File([blob], 'cropped.jpg', { type: 'image/jpeg' });

        // ✅ OTIMIZAÇÃO: Faz upload da imagem cropada para substituir a original no servidor
        // Isso permite reutilizar imageId sem reenvios múltiplos
        const fileSizeKB = (file.size / 1024).toFixed(0);
        const reader = new FileReader();
        reader.onload = async (e) => {
            const img = new Image();
            img.onload = async () => {
                const { file: processedFile, dataUrl } = await redimensionarImagem(img, file.name);
                state.currentPhotoFile = processedFile;

                console.log(`📦 Imagem cropada processada: ${(processedFile.size / 1024).toFixed(0)}KB (original: ${fileSizeKB}KB)`);

                elements.previewImageIntegracao.src = dataUrl;
                elements.photoPreviewIntegracao.classList.remove('hidden');
                elements.submitBtn.disabled = false;

                // Salva imagem no estado compartilhado
                const originalImageData = state.originalPhoto ? state.originalPhoto.src : dataUrl;
                saveSharedImage(originalImageData, dataUrl, file.name, processedFile, 'integracao');

                // ✨ NOVA ARQUITETURA: Coordenadas já armazenadas em aplicarCropGenerico()

                // Esconde crop, mostra preview
                elements.cropSectionIntegracao.classList.add('hidden');
                elements.photoPreviewIntegracao.classList.remove('hidden');
                elements.cropInfoIntegracao.classList.add('hidden');

                showMessage('Imagem cortada! Clique em "Gerar Ambiente" para criar o mockup.', 'success');
            };
            img.src = e.target.result;
        };
        reader.readAsDataURL(file);
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
async function startAmbienteFlow() {
    // 🔧 EMERGENCY FIX: Reseta flag travada se usuário voltar ao menu principal
    state.isGeneratingMockup = false;

    // 🔧 FIX: Limpa estado de countertop e bathroom para evitar interferência entre flows
    state.countertopState.selectedType = null;
    state.countertopState.croppedImage = null;
    state.bathroomState.selectedType = null;

    if (!state.originalPhoto) {
        showMessage('Nenhuma foto disponível para ambiente', 'error');
        return;
    }

    // ✨ NOVA ARQUITETURA: Upload já foi feito, coordenadas armazenadas localmente
    // Mostra tela de configuração
    showScreen(elements.ambienteConfigScreen);
}

function abrirCropParaAmbiente() {
    // ✅ FIX: Check de isGeneratingMockup REMOVIDO
    // Motivo: Se flag ficar travada após erro, usuário fica bloqueado permanentemente
    // A função gerarAmbiente() já tem proteção no try/finally que reseta a flag (linha 2830)
    // Múltiplas chamadas são controladas pelo loading overlay que bloqueia a UI

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

    // ✨ FIX: Marca que está gerando mockup
    state.isGeneratingMockup = true;

    // Gera ambiente direto com a imagem atual (cropada ou original)
    gerarAmbiente(state.currentPhotoFile);
}

async function gerarAmbiente(imagemCropada) {
    try {
        console.log('🎬 gerarAmbiente() chamado');
        // ✅ Upload já foi feito em startAmbienteFlow()

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

            // ✨ NOVA ARQUITETURA: Sempre usa imageId + coordenadas de crop opcionais
            console.log(`📎 Usando imagem do servidor: ${state.uploadedImageId}`);
            formData.append('imageId', state.uploadedImageId);

            // Adiciona coordenadas de crop se existirem
            if (state.cropCoordinates) {
                console.log('✂️ Enviando coordenadas de crop:', state.cropCoordinates);
                formData.append('cropX', state.cropCoordinates.x);
                formData.append('cropY', state.cropCoordinates.y);
                formData.append('cropWidth', state.cropCoordinates.width);
                formData.append('cropHeight', state.cropCoordinates.height);
            }

            formData.append('flip', state.ambienteConfig.flip || false);
        } else {
            // Cavalete: /api/mockup/gerar/progressive
            endpoint = '/api/mockup/gerar/progressive';
            formData = new FormData();

            // ✨ NOVA ARQUITETURA: Sempre usa imageId + coordenadas de crop opcionais
            console.log(`📎 Usando imagem do servidor: ${state.uploadedImageId}`);
            formData.append('imageId', state.uploadedImageId);

            // Adiciona coordenadas de crop se existirem
            if (state.cropCoordinates) {
                console.log('✂️ Enviando coordenadas de crop:', state.cropCoordinates);
                formData.append('cropX', state.cropCoordinates.x);
                formData.append('cropY', state.cropCoordinates.y);
                formData.append('cropWidth', state.cropCoordinates.width);
                formData.append('cropHeight', state.cropCoordinates.height);
            }

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
        const getLabels = (tipoAmbiente, count) => {
            if (tipoAmbiente.startsWith('bancada')) {
                const num = tipoAmbiente.replace('bancada', '');
                // Gera labels dinamicamente: "Bancada #X - View 1", "View 2", etc.
                return Array.from({ length: count }, (_, i) => `Bancada #${num} - View ${i + 1}`);
            } else {
                return [
                    'Cavalete Duplo - Original/Espelho',
                    'Cavalete Duplo - Espelho/Original',
                    'Cavalete Simples'
                ];
            }
        };

        // Gera labels suficientes (máximo 10 views) - serão ajustados conforme mockups chegam
        const labels = getLabels(tipo, 10);

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

        // ✨ FIX: Libera flag de geração para permitir novas gerações
        state.isGeneratingMockup = false;
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
async function startCountertopFlow() {
    // 🔧 EMERGENCY FIX: Reseta flag travada se usuário voltar ao menu principal
    state.isGeneratingMockup = false;

    // 🔧 FIX: Garantir que crop overlay esteja oculto (pode estar bloqueando cliques)
    if (elements.cropOverlayAmbientes) {
        elements.cropOverlayAmbientes.classList.add('hidden');
    }
    state.cropOverlayState.isActive = false;

    if (!state.currentPhotoFile) {
        showMessage('Por favor, selecione uma foto primeiro', 'error');
        return;
    }

    // ✨ NOVA ARQUITETURA: Upload já foi feito, coordenadas armazenadas localmente

    // Limpa estado anterior de countertop
    state.countertopState.croppedImage = null;
    state.countertopState.selectedType = null;
    state.countertopState.flip = false;

    // 🔧 FIX: Limpa estado de bathroom para evitar interferência entre flows
    state.bathroomState.selectedType = null;

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
    // ✅ FIX: Check de isGeneratingMockup REMOVIDO
    // Motivo: Se flag ficar travada após erro, usuário fica bloqueado permanentemente
    // A função generateCountertopAmbiente() já tem proteção no try/finally que reseta a flag (linha 3081)
    // Múltiplas chamadas são controladas pelo loading overlay que bloqueia a UI

    if (!state.countertopState.croppedImage) {
        showMessage('Erro: Imagem cortada não encontrada', 'error');
        return;
    }

    // Salva seleção
    state.countertopState.selectedType = type;
    state.countertopState.flip = elements.flipCountertop ? elements.flipCountertop.checked : false;

    // ✨ FIX: Marca que está gerando mockup
    state.isGeneratingMockup = true;

    // Gera ambiente
    await generateCountertopAmbiente();
}

/**
 * Passo 4: Gera o ambiente da bancada selecionada
 */
async function generateCountertopAmbiente() {
    try {
        console.log('🎬 generateCountertopAmbiente() chamado');

        // Mostra loading overlay global
        elements.loadingOverlay.classList.remove('hidden');

        const formData = new FormData();

        // ✨ NOVA ARQUITETURA: Usa endpoint progressive com imageId + coordenadas
        console.log(`📎 Usando imagem do servidor: ${state.uploadedImageId}`);
        formData.append('imageId', state.uploadedImageId);
        formData.append('flip', state.countertopState.flip);

        // Adiciona coordenadas de crop se existirem
        if (state.cropCoordinates) {
            console.log('✂️ Enviando coordenadas de crop:', state.cropCoordinates);
            formData.append('cropX', state.cropCoordinates.x);
            formData.append('cropY', state.cropCoordinates.y);
            formData.append('cropWidth', state.cropCoordinates.width);
            formData.append('cropHeight', state.cropCoordinates.height);
        }

        // Usa endpoint progressive que suporta imageId e crop
        const endpoint = `/api/mockup/${state.countertopState.selectedType}/progressive`;

        const response = await fetch(`${API_URL}${endpoint}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${state.token}`
            },
            body: formData
        });

        // ✨ Endpoint progressive retorna SSE (Server-Sent Events)
        if (!response.ok) {
            throw new Error(`Erro ${response.status}: ${response.statusText || 'Falha ao gerar ambiente'}`);
        }

        // Processa SSE progressive com buffer (técnica robusta)
        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';
        const ambientes = [];

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
                    console.log('📦 SSE event recebido:', event);

                    if (event.type === 'mockup' && event.data?.url) {
                        ambientes.push(event.data.url);
                        console.log(`✅ Mockup ${ambientes.length} recebido:`, event.data.url);
                    }
                } catch (e) {
                    console.warn('⚠️ Erro ao parsear SSE:', e, 'Line:', jsonStr);
                }
            }
        }

        console.log('🎉 Todos os mockups recebidos:', ambientes);

        // Exibe resultado
        displayCountertopResults({ ambientes });

    } catch (error) {
        console.error('Erro ao gerar bancada:', error);
        showAmbienteMessage(error.message, 'error');
        showMainScreen();
    } finally {
        // Esconde loading overlay
        elements.loadingOverlay.classList.add('hidden');
        // Limpa flag de countertop
        state.ambienteConfig.tipo = 'simples';
        // ✨ FIX: Libera flag de geração para permitir novas gerações
        state.isGeneratingMockup = false;
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
        'bancada1': ['Bancada #1 - View 1', 'Bancada #1 - View 2'],
        'bancada2': ['Bancada #2 - View 1', 'Bancada #2 - View 2'],
        'bancada3': ['Bancada #3 - View 1', 'Bancada #3 - View 2'],
        'bancada4': ['Bancada #4 - View 1', 'Bancada #4 - View 2'],
        'bancada5': ['Bancada #5 - View 1', 'Bancada #5 - View 2', 'Bancada #5 - View 3', 'Bancada #5 - View 4'],
        'bancada6': ['Bancada #6 - View 1', 'Bancada #6 - View 2'],
        'bancada7': ['Bancada #7 - View 1', 'Bancada #7 - View 2'],
        'bancada8': ['Bancada #8 - View 1', 'Bancada #8 - View 2']
    };

    const labels = bancadaLabels[state.countertopState.selectedType] ||
                   ['Bancada - View 1', 'Bancada - View 2'];

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

    // Configura botão "Modificar Crop" para voltar à tela de crop (somente se o botão existir)
    if (elements.modifyCropBtn) {
        elements.modifyCropBtn.onclick = () => {
            // Verifica se há imagem compartilhada
            if (!state.sharedImageState.originalImage) {
                console.error('Nenhuma imagem original disponível para modificar crop');
                return;
            }

            // ✨ FIX: Preserva contexto de ambiente (cavalete, nicho, etc)
            // Se ambienteConfig.tipo não for 'simples' ou 'countertop', reativa modo ambiente
            if (state.ambienteConfig.tipo !== 'simples' && state.ambienteConfig.tipo !== 'countertop') {
                state.ambienteMode = true;
                console.log(`[Modificar Crop] Reativando modo ambiente: ${state.ambienteConfig.tipo}`);
            }

            // Volta para tela de crop com a imagem original
            const img = new Image();
            img.onload = () => {
                state.cropData.image = img;
                initializeCropCanvas();
                showCropScreen();
            };
            img.src = state.sharedImageState.originalImage;
        };
    }

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

// ========== BATHROOMS MOCKUP FLOW ==========

/**
 * Inicia o flow de Bathroom - gera ambos bathroom1 e bathroom2
 */
async function startBathroomsFlow() {
    // 🔧 EMERGENCY FIX: Reseta flag travada se usuário voltar ao menu principal
    state.isGeneratingMockup = false;

    // 🔧 FIX: Limpa estado de countertop para evitar interferência entre flows
    state.countertopState.selectedType = null;
    state.countertopState.croppedImage = null; // ← FIX: Limpa croppedImage para evitar bug de navegação

    if (!state.currentPhotoFile) {
        showMessage('Por favor, selecione uma foto primeiro', 'error');
        return;
    }

    // Mostra tela de seleção de banheiro
    showScreen(elements.bathroomSelectionScreen);
}

/**
 * Seleciona banheiro e inicia geração
 */
async function selectBathroomAndGenerate(type) {
    if (!state.currentPhotoFile) {
        showMessage('Erro: Foto não encontrada', 'error');
        return;
    }

    // ✨ FIX: Marca que está gerando mockup
    state.isGeneratingMockup = true;

    try {
        // Prepara para receber os mockups
        state.ambienteUrls = [];
        state.ambienteMode = true;

        // Mostra tela de resultados
        showScreen(elements.ambienteResultScreen);
        elements.ambientesGallery.innerHTML = '<div class="loading">Gerando Bathroom...</div>';

        // Extrai número do tipo (banho1 → 1)
        const bathroomNumber = parseInt(type.replace('banho', ''));

        // Salva tipo selecionado no estado (para navegação do botão Voltar)
        state.bathroomState.selectedType = type;

        // Gera Bathroom selecionado
        await gerarBathroomProgressivo(bathroomNumber);

        // Atualiza mensagem de sucesso
        const totalGerados = state.ambienteUrls.length;
        showAmbienteMessage(`${totalGerados} quadrante${totalGerados > 1 ? 's' : ''} gerado${totalGerados > 1 ? 's' : ''} com sucesso!`, 'success');

    } catch (error) {
        console.error('Erro ao gerar bathroom:', error);
        showAmbienteMessage('Erro ao gerar bathroom: ' + error.message, 'error');
    } finally {
        // ✨ FIX: Libera flag de geração para permitir novas gerações
        state.isGeneratingMockup = false;
    }
}

/**
 * Gera um Bathroom específico via SSE Progressive
 */
async function gerarBathroomProgressivo(numero) {
    const formData = new FormData();

    // ✅ FIX: Usar imagem CROPADA ao invés da original
    // Se existe imagem cropada em Base64, converte para Blob e usa
    if (state.sharedImageState && state.sharedImageState.currentImage) {
        const croppedBlob = base64ToBlob(state.sharedImageState.currentImage);
        formData.append('imagemCropada', croppedBlob, 'cropped.jpg');
        console.log('✅ Usando imagem CROPADA para bathroom');
    } else {
        // Fallback: usa arquivo original se não houver crop
        formData.append('imagemCropada', state.currentPhotoFile);
        console.log('⚠️ Usando imagem ORIGINAL para bathroom (sem crop)');
    }

    formData.append('fundo', 'claro'); // Pode ser parametrizado depois

    const endpoint = `${API_URL}/api/mockup/bathroom${numero}/progressive`;

    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: formData
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        while (true) {
            const { value, done } = await reader.read();
            if (done) break;

            const chunk = decoder.decode(value);
            const lines = chunk.split('\n');

            for (const line of lines) {
                if (line.startsWith('data: ')) {
                    const jsonStr = line.slice(6);
                    try {
                        const event = JSON.parse(jsonStr);

                        if (event.type === 'inicio') {
                            console.log(`Bathroom ${numero}: ${event.data.mensagem}`);
                        } else if (event.type === 'progresso') {
                            console.log(`Bathroom ${numero}: ${event.data.etapa}`);
                            elements.ambientesGallery.innerHTML = `<div class="loading">${event.data.etapa}</div>`;
                        } else if (event.type === 'sucesso') {
                            // Adiciona os 4 quadrantes à galeria
                            event.data.caminhos.forEach(caminho => {
                                const imageUrl = `${API_URL}/uploads/mockups/${caminho}`;
                                state.ambienteUrls.push(imageUrl);
                                adicionarImagemAGaleria(imageUrl, `Bathroom #${numero}`);
                            });
                        } else if (event.type === 'erro') {
                            throw new Error(event.data.mensagem);
                        }
                    } catch (parseError) {
                        console.warn('Erro ao parsear evento SSE:', parseError);
                    }
                }
            }
        }
    } catch (error) {
        console.error(`Erro ao gerar Bathroom ${numero}:`, error);
        throw error;
    }
}

/**
 * Adiciona uma imagem à galeria de resultados
 */
function adicionarImagemAGaleria(imageUrl, label) {
    if (elements.ambientesGallery.querySelector('.loading')) {
        elements.ambientesGallery.innerHTML = '';
    }

    const ambienteItem = document.createElement('div');
    ambienteItem.className = 'ambiente-item';

    ambienteItem.innerHTML = `
        <img src="${imageUrl}" alt="${label}">
        <div class="ambiente-actions">
            <button class="btn btn-secondary btn-download-single"
                    data-url="${imageUrl}"
                    data-nome="${label}">
                ⬇️ Baixar
            </button>
            <button class="btn btn-primary btn-share-single"
                    data-url="${imageUrl}"
                    data-nome="${label}">
                📤 Compartilhar
            </button>
        </div>
    `;

    elements.ambientesGallery.appendChild(ambienteItem);
}

// ========== SERVICE WORKER (para PWA futuro) ==========
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        // Descomentado quando houver service-worker.js
        // navigator.serviceWorker.register('/service-worker.js');
    });
}
