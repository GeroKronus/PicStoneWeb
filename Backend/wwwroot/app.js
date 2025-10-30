// Configuração da API
const API_URL = window.location.origin;

// Estado da aplicação
const state = {
    token: localStorage.getItem('token') || null,
    username: localStorage.getItem('username') || null,
    currentPhoto: null,
    currentPhotoFile: null
};

// Elementos DOM
const elements = {
    loginScreen: document.getElementById('loginScreen'),
    mainScreen: document.getElementById('mainScreen'),
    historyScreen: document.getElementById('historyScreen'),
    loginForm: document.getElementById('loginForm'),
    loginError: document.getElementById('loginError'),
    captureBtn: document.getElementById('captureBtn'),
    fileInput: document.getElementById('fileInput'),
    photoPreview: document.getElementById('photoPreview'),
    previewImage: document.getElementById('previewImage'),
    clearPhotoBtn: document.getElementById('clearPhotoBtn'),
    uploadForm: document.getElementById('uploadForm'),
    submitBtn: document.getElementById('submitBtn'),
    uploadProgress: document.getElementById('uploadProgress'),
    uploadMessage: document.getElementById('uploadMessage'),
    userDisplay: document.getElementById('userDisplay'),
    logoutBtn: document.getElementById('logoutBtn'),
    historyBtn: document.getElementById('historyBtn'),
    backBtn: document.getElementById('backBtn'),
    historyList: document.getElementById('historyList')
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
    elements.captureBtn.addEventListener('click', () => elements.fileInput.click());
    elements.fileInput.addEventListener('change', handleFileSelect);
    elements.clearPhotoBtn.addEventListener('click', clearPhoto);
    elements.uploadForm.addEventListener('submit', handleUpload);
    elements.logoutBtn.addEventListener('click', handleLogout);
    elements.historyBtn.addEventListener('click', showHistoryScreen);
    elements.backBtn.addEventListener('click', showMainScreen);
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

function showMainScreen() {
    showScreen(elements.mainScreen);
    elements.userDisplay.textContent = `Olá, ${state.username}`;
}

async function showHistoryScreen() {
    showScreen(elements.historyScreen);
    await loadHistory();
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

    // Comprime e exibe preview
    compressAndPreviewImage(file);
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
    state.currentPhotoFile = null;
    elements.previewImage.src = '';
    elements.photoPreview.classList.add('hidden');
    elements.fileInput.value = '';
    elements.submitBtn.disabled = true;
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
    formData.append('Lote', document.getElementById('lote').value);
    formData.append('Chapa', document.getElementById('chapa').value);
    formData.append('Processo', document.getElementById('processo').value);

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

        // Limpa formulário após sucesso
        setTimeout(() => {
            clearPhoto();
            elements.uploadForm.reset();
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
                <h3>${foto.nomeArquivo}</h3>
                <p><strong>Lote:</strong> ${foto.lote} | <strong>Chapa:</strong> ${foto.chapa}</p>
                <p><strong>Processo:</strong> ${foto.processo} ${foto.espessura ? `| <strong>Espessura:</strong> ${foto.espessura}mm` : ''}</p>
                <small>Enviado por ${foto.usuario} em ${formatDate(foto.dataUpload)}</small>
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

// ========== SERVICE WORKER (para PWA futuro) ==========
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        // Descomentado quando houver service-worker.js
        // navigator.serviceWorker.register('/service-worker.js');
    });
}
