using Microsoft.EntityFrameworkCore;
using PicStoneFotoAPI.Data;
using PicStoneFotoAPI.Models;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para tracking de histórico de usuários
    /// </summary>
    public class HistoryService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HistoryService> _logger;

        public HistoryService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<HistoryService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Registra login do usuário
        /// </summary>
        public async Task RegistrarLoginAsync(int usuarioId)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var login = new UserLogin
                {
                    UsuarioId = usuarioId,
                    DataHora = DateTime.UtcNow,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString()
                };

                _context.UserLogins.Add(login);

                // Atualiza último acesso do usuário
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario != null)
                {
                    usuario.UltimoAcesso = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Login registrado para usuário {usuarioId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar login do usuário {usuarioId}");
                // Não lança exceção para não bloquear o login
            }
        }

        /// <summary>
        /// Registra geração de ambiente
        /// </summary>
        public async Task RegistrarAmbienteAsync(
            int usuarioId,
            string tipoAmbiente,
            string? material = null,
            string? bloco = null,
            string? chapa = null,
            string? detalhes = null,
            int quantidadeImagens = 1)
        {
            try
            {
                var ambiente = new GeneratedEnvironment
                {
                    UsuarioId = usuarioId,
                    DataHora = DateTime.UtcNow,
                    TipoAmbiente = tipoAmbiente,
                    Material = material,
                    Bloco = bloco,
                    Chapa = chapa,
                    Detalhes = detalhes,
                    QuantidadeImagens = quantidadeImagens
                };

                _context.GeneratedEnvironments.Add(ambiente);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Ambiente '{tipoAmbiente}' registrado para usuário {usuarioId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar ambiente do usuário {usuarioId}");
                // Não lança exceção para não bloquear a geração
            }
        }

        /// <summary>
        /// Busca últimos logins de um usuário
        /// </summary>
        public async Task<List<UserLogin>> GetLoginsUsuarioAsync(int usuarioId, int limite = 50)
        {
            return await _context.UserLogins
                .Where(l => l.UsuarioId == usuarioId)
                .OrderByDescending(l => l.DataHora)
                .Take(limite)
                .ToListAsync();
        }

        /// <summary>
        /// Busca últimos ambientes gerados por um usuário
        /// </summary>
        public async Task<List<GeneratedEnvironment>> GetAmbientesUsuarioAsync(int usuarioId, int limite = 50)
        {
            return await _context.GeneratedEnvironments
                .Where(e => e.UsuarioId == usuarioId)
                .OrderByDescending(e => e.DataHora)
                .Take(limite)
                .ToListAsync();
        }

        /// <summary>
        /// Estatísticas simples de uso do usuário
        /// </summary>
        public async Task<UserStats> GetUserStatsAsync(int usuarioId)
        {
            var totalLogins = await _context.UserLogins
                .Where(l => l.UsuarioId == usuarioId)
                .CountAsync();

            var totalAmbientes = await _context.GeneratedEnvironments
                .Where(e => e.UsuarioId == usuarioId)
                .CountAsync();

            var primeiroAcesso = await _context.UserLogins
                .Where(l => l.UsuarioId == usuarioId)
                .OrderBy(l => l.DataHora)
                .Select(l => l.DataHora)
                .FirstOrDefaultAsync();

            var ultimoAcesso = await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.UltimoAcesso)
                .FirstOrDefaultAsync();

            return new UserStats
            {
                TotalLogins = totalLogins,
                TotalAmbientesGerados = totalAmbientes,
                PrimeiroAcesso = primeiroAcesso,
                UltimoAcesso = ultimoAcesso
            };
        }

        /// <summary>
        /// [OTIMIZADO] Retorna TODOS os usuários com suas estatísticas em 1 única query SQL
        /// Performance: 1 query vs N*4 queries (onde N = número de usuários)
        /// </summary>
        public async Task<List<UserWithStatsDto>> GetAllUsersWithStatsAsync()
        {
            var result = await _context.Usuarios
                .Select(u => new UserWithStatsDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    NomeCompleto = u.NomeCompleto,
                    Ativo = u.Ativo,
                    Status = u.Status,
                    DataExpiracao = u.DataExpiracao,
                    Stats = new UserStats
                    {
                        TotalLogins = u.Logins.Count(),
                        TotalAmbientesGerados = u.AmbientesGerados.Count(),
                        PrimeiroAcesso = u.Logins.OrderBy(l => l.DataHora).Select(l => l.DataHora).FirstOrDefault(),
                        UltimoAcesso = u.UltimoAcesso
                    }
                })
                .OrderByDescending(u => u.Stats.UltimoAcesso ?? DateTime.MinValue)
                .ToListAsync();

            return result;
        }
    }

    /// <summary>
    /// DTO para retornar usuário com estatísticas
    /// </summary>
    public class UserWithStatsDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public StatusUsuario Status { get; set; }
        public DateTime? DataExpiracao { get; set; }
        public UserStats Stats { get; set; } = new UserStats();
    }
}
