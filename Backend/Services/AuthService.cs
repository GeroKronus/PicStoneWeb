using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PicStoneFotoAPI.Data;
using PicStoneFotoAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço de autenticação com JWT
    /// </summary>
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Autentica usuário e gera token JWT
        /// </summary>
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                // Busca usuário no banco
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Username == request.Username && u.Ativo);

                if (usuario == null)
                {
                    _logger.LogWarning("Tentativa de login com usuário inexistente: {Username}", request.Username);
                    return null;
                }

                // Verifica senha com BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                {
                    _logger.LogWarning("Senha incorreta para usuário: {Username}", request.Username);
                    return null;
                }

                // Gera token JWT
                var token = GerarTokenJWT(usuario);
                var expiresAt = DateTime.UtcNow.AddHours(8);

                _logger.LogInformation("Login bem-sucedido para usuário: {Username}", request.Username);

                return new LoginResponse
                {
                    Token = token,
                    Username = usuario.Username,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar login para usuário: {Username}", request.Username);
                throw;
            }
        }

        /// <summary>
        /// Renova token JWT (se ainda válido)
        /// </summary>
        public string? RenovarToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSecret = _configuration["JWT_SECRET"] ?? "ChaveSecretaPadraoParaDesenvolvimento123!@#";
                var key = Encoding.UTF8.GetBytes(jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
                var userId = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

                // Cria novo token com mesmas claims
                var usuario = new Usuario { Id = int.Parse(userId), Username = username };
                return GerarTokenJWT(usuario);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gera token JWT válido por 8 horas
        /// </summary>
        private string GerarTokenJWT(Usuario usuario)
        {
            var jwtSecret = _configuration["JWT_SECRET"] ?? "ChaveSecretaPadraoParaDesenvolvimento123!@#";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("NomeCompleto", usuario.NomeCompleto)
            };

            var token = new JwtSecurityToken(
                issuer: "PicStoneFotoAPI",
                audience: "PicStoneFotoApp",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Cria usuário inicial para testes (admin/admin123)
        /// </summary>
        public async Task CriarUsuarioInicialAsync()
        {
            try
            {
                // Verifica se já existe algum usuário
                if (await _context.Usuarios.AnyAsync())
                {
                    return;
                }

                // Cria usuário admin padrão
                var adminUser = new Usuario
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    NomeCompleto = "Administrador",
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Usuarios.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário inicial 'admin' criado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário inicial");
            }
        }

        /// <summary>
        /// Testa conexão com o banco de dados
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão com banco");
                return false;
            }
        }

        /// <summary>
        /// Retorna contagem de usuários
        /// </summary>
        public async Task<int> GetUserCountAsync()
        {
            try
            {
                return await _context.Usuarios.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar usuários");
                return -1;
            }
        }

        /// <summary>
        /// Verifica se o usuário admin existe
        /// </summary>
        public async Task<bool> CheckAdminExistsAsync()
        {
            try
            {
                return await _context.Usuarios.AnyAsync(u => u.Username == "admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se admin existe");
                return false;
            }
        }
    }
}
