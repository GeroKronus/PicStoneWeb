// ==================== HISTÓRICO DE USO ====================

// Elementos da tela de histórico
let historyScreen, backFromHistoryBtn;
let allUsersTab, myHistoryTab;
let allUsersView, myHistoryView;
let usersStatsList, searchUsersInput;
let myStatsCard, myLoginsList, myAmbientesList;
let myLoginsTabBtn, myAmbientesTabBtn;

// Elementos de visualização
let cardViewBtn, tableViewBtn;
let usersStatsTable, usersTableBody;

// Armazena lista completa de usuários para filtro
let allUsersData = [];
let currentViewMode = 'cards'; // 'cards' ou 'table'

// Helper para obter token
function getToken() {
    return localStorage.getItem('token');
}

// Inicialização da tela de histórico
function initHistory() {
    // Elementos da tela
    historyScreen = document.getElementById('historyScreen');
    backFromHistoryBtn = document.getElementById('backFromHistoryBtn');

    // Abas principais
    allUsersTab = document.getElementById('allUsersTab');
    myHistoryTab = document.getElementById('myHistoryTab');

    // Views
    allUsersView = document.getElementById('allUsersView');
    myHistoryView = document.getElementById('myHistoryView');

    // Listas
    usersStatsList = document.getElementById('usersStatsList');
    myLoginsList = document.getElementById('myLoginsList');
    myAmbientesList = document.getElementById('myAmbientesList');

    // Campo de busca
    searchUsersInput = document.getElementById('searchUsersInput');

    // Elementos de visualização
    cardViewBtn = document.getElementById('cardViewBtn');
    tableViewBtn = document.getElementById('tableViewBtn');
    usersStatsTable = document.getElementById('usersStatsTable');
    usersTableBody = document.getElementById('usersTableBody');

    // Sub-abas
    myLoginsTabBtn = document.getElementById('myLoginsTabBtn');
    myAmbientesTabBtn = document.getElementById('myAmbientesTabBtn');

    // Event Listeners
    if (backFromHistoryBtn) {
        backFromHistoryBtn.addEventListener('click', () => {
            const mainScreen = document.getElementById('mainScreen');
            if (mainScreen && typeof showScreen === 'function') {
                showScreen(mainScreen);
            }
        });
    }

    if (allUsersTab) {
        allUsersTab.addEventListener('click', () => switchHistoryTab('allUsers'));
    }

    if (myHistoryTab) {
        myHistoryTab.addEventListener('click', () => switchHistoryTab('myHistory'));
    }

    if (myLoginsTabBtn) {
        myLoginsTabBtn.addEventListener('click', () => switchMyHistorySubTab('logins'));
    }

    if (myAmbientesTabBtn) {
        myAmbientesTabBtn.addEventListener('click', () => switchMyHistorySubTab('ambientes'));
    }

    // Event listener para busca de usuários
    if (searchUsersInput) {
        searchUsersInput.addEventListener('input', (e) => {
            filterUsers(e.target.value);
        });
    }

    // Event listeners para botões de visualização
    if (cardViewBtn) {
        cardViewBtn.addEventListener('click', () => switchViewMode('cards'));
    }

    if (tableViewBtn) {
        tableViewBtn.addEventListener('click', () => switchViewMode('table'));
    }
}

// Mostra a tela de histórico
async function showHistoryScreen() {
    const isAdmin = localStorage.getItem('isAdmin') === 'true';

    // Esconde/mostra abas baseado no papel do usuário
    if (isAdmin) {
        allUsersTab.style.display = 'block';
        allUsersView.style.display = 'block';
        allUsersTab.classList.add('active');
        myHistoryTab.classList.remove('active');
        allUsersView.classList.add('active');
        myHistoryView.classList.remove('active');

        // Carrega lista de usuários
        await loadAllUsersStats();
    } else {
        allUsersTab.style.display = 'none';
        allUsersView.style.display = 'none';
        myHistoryTab.classList.add('active');
        myHistoryView.classList.add('active');

        // Carrega histórico do usuário
        await loadMyHistory();
    }

    if (typeof showScreen === 'function') {
        showScreen(historyScreen);
    }
}

// Alterna entre abas (Todos os Usuários / Meu Histórico)
function switchHistoryTab(tab) {
    if (tab === 'allUsers') {
        allUsersTab.classList.add('active');
        myHistoryTab.classList.remove('active');
        allUsersView.classList.add('active');
        myHistoryView.classList.remove('active');

        loadAllUsersStats();
    } else {
        myHistoryTab.classList.add('active');
        allUsersTab.classList.remove('active');
        myHistoryView.classList.add('active');
        allUsersView.classList.remove('active');

        loadMyHistory();
    }
}

// Alterna entre sub-abas do histórico do usuário (Logins / Ambientes)
function switchMyHistorySubTab(tab) {
    if (tab === 'logins') {
        myLoginsTabBtn.classList.add('active');
        myAmbientesTabBtn.classList.remove('active');
        myLoginsList.classList.add('active');
        myAmbientesList.classList.remove('active');
    } else {
        myAmbientesTabBtn.classList.add('active');
        myLoginsTabBtn.classList.remove('active');
        myAmbientesList.classList.add('active');
        myLoginsList.classList.remove('active');
    }
}

// ==================== ADMIN: TODOS OS USUÁRIOS ====================

async function loadAllUsersStats() {
    try {
        usersStatsList.innerHTML = '<p class="loading">Carregando estatísticas...</p>';

        // Busca lista de usuários
        const usersResponse = await fetch('/api/auth/users', {
            headers: { 'Authorization': `Bearer ${getToken()}` }
        });

        if (!usersResponse.ok) throw new Error('Erro ao carregar usuários');

        const users = await usersResponse.json();

        // Carrega estatísticas de cada usuário
        const usersWithStats = await Promise.all(
            users.map(async (user) => {
                try {
                    const statsResponse = await fetch(`/api/history/admin/user/${user.id}/stats`, {
                        headers: { 'Authorization': `Bearer ${getToken()}` }
                    });

                    if (statsResponse.ok) {
                        const stats = await statsResponse.json();
                        return { ...user, stats };
                    }
                } catch (err) {
                    console.error(`Erro ao carregar stats do usuário ${user.id}:`, err);
                }

                return { ...user, stats: { totalLogins: 0, totalAmbientesGerados: 0 } };
            })
        );

        // Armazena dados para filtro e renderiza cards de usuários
        allUsersData = usersWithStats;
        renderUsersStatsCards(usersWithStats);

        // Também renderiza a tabela para que esteja pronta quando o usuário trocar de visualização
        renderUsersStatsTable(usersWithStats);

    } catch (error) {
        console.error('Erro ao carregar estatísticas:', error);
        usersStatsList.innerHTML = '<p class="loading">Erro ao carregar estatísticas</p>';
    }
}

function renderUsersStatsCards(users) {
    if (!users || users.length === 0) {
        usersStatsList.innerHTML = '<p class="loading">Nenhum usuário encontrado</p>';
        return;
    }

    usersStatsList.innerHTML = users.map(user => `
        <div class="user-stats-card" onclick="showUserDetails(${user.id}, '${escapeHtml(user.nomeCompleto)}')">
            <div class="user-stats-header">
                <div class="user-info">
                    <div class="user-avatar-small">
                        ${getInitials(user.nomeCompleto)}
                    </div>
                    <div class="user-details">
                        <h4>${escapeHtml(user.nomeCompleto)}</h4>
                        <p>${escapeHtml(user.username)}</p>
                    </div>
                </div>
                <span class="user-status-badge ${user.ativo ? 'active' : 'inactive'}">
                    ${user.ativo ? 'Ativo' : 'Inativo'}
                </span>
            </div>

            <div class="user-stats-mini">
                <div class="stat-mini">
                    <div class="stat-mini-value">${user.stats.totalLogins || 0}</div>
                    <div class="stat-mini-label">Logins</div>
                </div>
                <div class="stat-mini">
                    <div class="stat-mini-value">${user.stats.totalAmbientesGerados || 0}</div>
                    <div class="stat-mini-label">Ambientes</div>
                </div>
                <div class="stat-mini">
                    <div class="stat-mini-value">${user.stats.ultimoAcesso ? formatDateShort(user.stats.ultimoAcesso) : 'Nunca'}</div>
                    <div class="stat-mini-label">Último Acesso</div>
                </div>
            </div>
        </div>
    `).join('');
}

// Mostra detalhes de um usuário específico (modal ou nova tela)
async function showUserDetails(userId, userName) {
    try {
        const [logins, ambientes, stats] = await Promise.all([
            fetch(`/api/history/admin/user/${userId}/logins?limite=20`, {
                headers: { 'Authorization': `Bearer ${getToken()}` }
            }).then(r => r.json()),

            fetch(`/api/history/admin/user/${userId}/ambientes?limite=20`, {
                headers: { 'Authorization': `Bearer ${getToken()}` }
            }).then(r => r.json()),

            fetch(`/api/history/admin/user/${userId}/stats`, {
                headers: { 'Authorization': `Bearer ${getToken()}` }
            }).then(r => r.json())
        ]);

        // Mostra modal com detalhes
        showUserDetailsModal(userName, stats, logins, ambientes);

    } catch (error) {
        console.error('Erro ao carregar detalhes do usuário:', error);
        showMessage('Erro ao carregar detalhes do usuário', 'error');
    }
}

function showUserDetailsModal(userName, stats, logins, ambientes) {
    const modal = document.createElement('div');
    modal.className = 'modal active';
    modal.innerHTML = `
        <div class="modal-content" style="max-width: 600px; max-height: 80vh; overflow-y: auto;">
            <h2>${escapeHtml(userName)} - Histórico</h2>

            <div class="stats-grid" style="margin: 20px 0;">
                <div class="stat-mini">
                    <div class="stat-mini-value">${stats.totalLogins || 0}</div>
                    <div class="stat-mini-label">Total Logins</div>
                </div>
                <div class="stat-mini">
                    <div class="stat-mini-value">${stats.totalAmbientesGerados || 0}</div>
                    <div class="stat-mini-label">Ambientes</div>
                </div>
            </div>

            <h3 style="margin-top: 24px;">Últimos Logins (${logins.length})</h3>
            <div class="history-list active" style="margin-bottom: 20px;">
                ${logins.slice(0, 5).map(login => `
                    <div class="history-item">
                        <div class="history-item-header">
                            <span class="history-item-title">Login</span>
                            <span class="history-item-time">${formatDateTime(login.dataHora)}</span>
                        </div>
                        <div class="history-item-details">
                            <span class="history-item-detail">📍 ${login.ipAddress || 'N/A'}</span>
                            <span class="history-item-detail">💻 ${parseUserAgent(login.userAgent)}</span>
                        </div>
                    </div>
                `).join('') || '<p class="loading">Nenhum login registrado</p>'}
            </div>

            <h3>Últimos Ambientes (${ambientes.length})</h3>
            <div class="history-list active" style="margin-bottom: 20px;">
                ${ambientes.slice(0, 5).map(amb => `
                    <div class="history-item">
                        <div class="history-item-header">
                            <span class="history-item-title">${amb.tipoAmbiente}</span>
                            <span class="history-item-time">${formatDateTime(amb.dataHora)}</span>
                        </div>
                        <div class="history-item-details">
                            ${amb.material ? `<span class="history-item-detail">🎨 ${amb.material}</span>` : ''}
                            ${amb.bloco ? `<span class="history-item-detail">📦 ${amb.bloco}</span>` : ''}
                            ${amb.chapa ? `<span class="history-item-detail">🔢 ${amb.chapa}</span>` : ''}
                            <span class="history-item-detail">📊 ${amb.quantidadeImagens} imagens</span>
                        </div>
                    </div>
                `).join('') || '<p class="loading">Nenhum ambiente registrado</p>'}
            </div>

            <button class="btn btn-primary btn-full" onclick="this.closest('.modal').remove()">Fechar</button>
        </div>
    `;

    document.body.appendChild(modal);
}

// ==================== USUÁRIO: MEU HISTÓRICO ====================

async function loadMyHistory() {
    try {
        // Carrega estatísticas
        const statsResponse = await fetch('/api/history/stats', {
            headers: { 'Authorization': `Bearer ${getToken()}` }
        });

        if (statsResponse.ok) {
            const stats = await statsResponse.json();
            renderMyStats(stats);
        }

        // Carrega logins
        await loadMyLogins();

        // Carrega ambientes
        await loadMyAmbientes();

    } catch (error) {
        console.error('Erro ao carregar meu histórico:', error);
    }
}

function renderMyStats(stats) {
    document.getElementById('myTotalLogins').textContent = stats.totalLogins || 0;
    document.getElementById('myTotalAmbientes').textContent = stats.totalAmbientesGerados || 0;
    document.getElementById('myFirstAccess').textContent = stats.primeiroAcesso ? formatDateShort(stats.primeiroAcesso) : 'N/A';
    document.getElementById('myLastAccess').textContent = stats.ultimoAcesso ? formatDateShort(stats.ultimoAcesso) : 'N/A';
}

async function loadMyLogins() {
    try {
        myLoginsList.innerHTML = '<p class="loading">Carregando logins...</p>';

        const response = await fetch('/api/history/logins?limite=50', {
            headers: { 'Authorization': `Bearer ${getToken()}` }
        });

        if (!response.ok) throw new Error('Erro ao carregar logins');

        const data = await response.json();

        if (!data.logins || data.logins.length === 0) {
            myLoginsList.innerHTML = '<p class="loading">Nenhum login registrado</p>';
            return;
        }

        myLoginsList.innerHTML = data.logins.map(login => `
            <div class="history-item">
                <div class="history-item-header">
                    <span class="history-item-title">Login</span>
                    <span class="history-item-time">${formatDateTime(login.dataHora)}</span>
                </div>
                <div class="history-item-details">
                    <span class="history-item-detail">📍 ${login.ipAddress || 'N/A'}</span>
                    <span class="history-item-detail">💻 ${parseUserAgent(login.userAgent)}</span>
                </div>
            </div>
        `).join('');

    } catch (error) {
        console.error('Erro ao carregar logins:', error);
        myLoginsList.innerHTML = '<p class="loading">Erro ao carregar logins</p>';
    }
}

async function loadMyAmbientes() {
    try {
        myAmbientesList.innerHTML = '<p class="loading">Carregando ambientes...</p>';

        const response = await fetch('/api/history/ambientes?limite=50', {
            headers: { 'Authorization': `Bearer ${getToken()}` }
        });

        if (!response.ok) throw new Error('Erro ao carregar ambientes');

        const data = await response.json();

        if (!data.ambientes || data.ambientes.length === 0) {
            myAmbientesList.innerHTML = '<p class="loading">Nenhum ambiente gerado</p>';
            return;
        }

        myAmbientesList.innerHTML = data.ambientes.map(amb => `
            <div class="history-item">
                <div class="history-item-header">
                    <span class="history-item-title">${amb.tipoAmbiente}</span>
                    <span class="history-item-time">${formatDateTime(amb.dataHora)}</span>
                </div>
                <div class="history-item-details">
                    ${amb.material ? `<span class="history-item-detail">🎨 ${amb.material}</span>` : ''}
                    ${amb.bloco ? `<span class="history-item-detail">📦 Bloco ${amb.bloco}</span>` : ''}
                    ${amb.chapa ? `<span class="history-item-detail">🔢 Chapa ${amb.chapa}</span>` : ''}
                    <span class="history-item-detail">📊 ${amb.quantidadeImagens} ${amb.quantidadeImagens === 1 ? 'imagem' : 'imagens'}</span>
                </div>
            </div>
        `).join('');

    } catch (error) {
        console.error('Erro ao carregar ambientes:', error);
        myAmbientesList.innerHTML = '<p class="loading">Erro ao carregar ambientes</p>';
    }
}

// ==================== UTILITÁRIOS ====================

function formatDateTime(dateString) {
    if (!dateString) return 'N/A';

    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Agora';
    if (diffMins < 60) return `${diffMins} min atrás`;
    if (diffHours < 24) return `${diffHours}h atrás`;
    if (diffDays < 7) return `${diffDays}d atrás`;

    return date.toLocaleString('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function formatDateShort(dateString) {
    if (!dateString) return 'N/A';

    const date = new Date(dateString);
    const now = new Date();
    const diffDays = Math.floor((now - date) / 86400000);

    if (diffDays === 0) return 'Hoje';
    if (diffDays === 1) return 'Ontem';
    if (diffDays < 7) return `${diffDays}d`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)}sem`;

    return date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
}

function parseUserAgent(userAgent) {
    if (!userAgent) return 'Desconhecido';

    // Simplifica o User-Agent
    if (userAgent.includes('iPhone')) return 'iPhone';
    if (userAgent.includes('iPad')) return 'iPad';
    if (userAgent.includes('Android')) return 'Android';
    if (userAgent.includes('Windows')) return 'Windows';
    if (userAgent.includes('Mac')) return 'Mac';
    if (userAgent.includes('Linux')) return 'Linux';
    if (userAgent.includes('curl')) return 'API/curl';

    return 'Navegador';
}

// Alterna entre visualização em cards e tabela
function switchViewMode(mode) {
    currentViewMode = mode;

    // Atualiza estado dos botões
    if (mode === 'cards') {
        cardViewBtn.classList.add('active');
        tableViewBtn.classList.remove('active');
        usersStatsList.classList.remove('hidden');
        usersStatsTable.classList.remove('active');
    } else {
        tableViewBtn.classList.add('active');
        cardViewBtn.classList.remove('active');
        usersStatsList.classList.add('hidden');
        usersStatsTable.classList.add('active');
    }

    // Re-renderiza com os dados atuais
    if (allUsersData && allUsersData.length > 0) {
        if (mode === 'cards') {
            renderUsersStatsCards(allUsersData);
        } else {
            renderUsersStatsTable(allUsersData);
        }
    }
}

// Renderiza tabela com estatísticas dos usuários
function renderUsersStatsTable(users) {
    if (!usersTableBody) return;

    if (!users || users.length === 0) {
        usersTableBody.innerHTML = '<tr><td colspan="7" class="loading">Nenhum usuário encontrado</td></tr>';
        return;
    }

    usersTableBody.innerHTML = users.map(user => {
        const nome = escapeHtml(user.nomeCompleto || 'Sem nome');
        const email = escapeHtml(user.username || 'Sem email');
        const totalLogins = (user.stats && user.stats.totalLogins) || 0;
        const totalAmbientes = (user.stats && user.stats.totalAmbientesGerados) || 0;
        const primeiroAcesso = (user.stats && user.stats.primeiroAcesso) ? formatDateTime(user.stats.primeiroAcesso, 'short') : '-';
        const ultimoAcesso = (user.stats && user.stats.ultimoAcesso) ? formatDateTime(user.stats.ultimoAcesso, 'short') : '-';

        return `
            <tr>
                <td><strong>${nome}</strong></td>
                <td>${email}</td>
                <td>${totalLogins}</td>
                <td>${totalAmbientes}</td>
                <td>${primeiroAcesso}</td>
                <td>${ultimoAcesso}</td>
                <td>
                    <button class="btn-small" onclick="showUserDetails(${user.id}, '${escapeHtml(user.nomeCompleto)}')">
                        Ver Detalhes
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

// Filtra usuários baseado no texto de busca
function filterUsers(searchText) {
    if (!allUsersData || allUsersData.length === 0) {
        return;
    }

    const search = searchText.toLowerCase().trim();

    // Se não há texto de busca, mostra todos
    if (!search) {
        if (currentViewMode === 'cards') {
            renderUsersStatsCards(allUsersData);
        } else {
            renderUsersStatsTable(allUsersData);
        }
        return;
    }

    // Filtra usuários por nome ou email
    const filtered = allUsersData.filter(user => {
        const nome = (user.nomeCompleto || '').toLowerCase();
        const email = (user.username || '').toLowerCase();

        return nome.includes(search) || email.includes(search);
    });

    // Renderiza conforme modo de visualização atual
    if (currentViewMode === 'cards') {
        renderUsersStatsCards(filtered);
    } else {
        renderUsersStatsTable(filtered);
    }
}

function getInitials(name) {
    if (!name) return '?';

    const parts = name.trim().split(' ');
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();

    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Inicializa quando o DOM estiver pronto
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initHistory);
} else {
    initHistory();
}
