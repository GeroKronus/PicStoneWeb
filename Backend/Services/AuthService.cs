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
        private readonly EmailService _emailService;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger, EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
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

                // ===== VERIFICAÇÃO DE EXPIRAÇÃO =====
                DateTime? dataExpiracao = null;
                int? diasRestantes = null;
                bool expiracaoProxima = false;

                if (usuario.DataExpiracao.HasValue)
                {
                    dataExpiracao = usuario.DataExpiracao.Value;
                    var hoje = DateTime.Now;

                    // Verifica se JÁ EXPIROU
                    if (dataExpiracao.Value <= hoje)
                    {
                        _logger.LogWarning("Tentativa de login com acesso expirado: {Username} (expirou em {DataExpiracao})",
                            request.Username, dataExpiracao.Value);

                        // Marca usuário como expirado se ainda não estiver
                        if (usuario.Status != StatusUsuario.Expirado)
                        {
                            usuario.Status = StatusUsuario.Expirado;
                            usuario.Ativo = false;
                            await _context.SaveChangesAsync();
                        }

                        return null; // BLOQUEIA LOGIN
                    }

                    // Calcula dias restantes
                    var timeSpan = dataExpiracao.Value - hoje;
                    diasRestantes = (int)Math.Ceiling(timeSpan.TotalDays);

                    // Verifica se está PRÓXIMO de expirar (5 dias ou menos)
                    if (diasRestantes <= 5)
                    {
                        expiracaoProxima = true;

                        // 🚀 OTIMIZAÇÃO: Envia email em background (não bloqueia login)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendExpirationWarningEmailAsync(
                                    usuario.Email,
                                    usuario.NomeCompleto,
                                    dataExpiracao.Value,
                                    diasRestantes.Value
                                );
                                _logger.LogInformation("Email de aviso de expiração enviado para: {Username} ({Dias} dias restantes)",
                                    request.Username, diasRestantes);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Erro ao enviar email de aviso de expiração para: {Username}", request.Username);
                                // Não bloqueia login se email falhar
                            }
                        });
                    }
                }

                // Gera token JWT
                var token = GerarTokenJWT(usuario);
                var expiresAt = DateTime.UtcNow.AddYears(100);
                var isAdmin = usuario.Username == "rogerio@picstone.com.br";

                _logger.LogInformation("Login bem-sucedido para usuário: {Username}", request.Username);

                return new LoginResponse
                {
                    Token = token,
                    Username = usuario.Username,
                    ExpiresAt = expiresAt,
                    IsAdmin = isAdmin,
                    DataExpiracao = dataExpiracao,
                    DiasRestantes = diasRestantes,
                    ExpiracaoProxima = expiracaoProxima
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
        /// Gera token JWT válido por 100 anos (praticamente sem expiração)
        /// </summary>
        private string GerarTokenJWT(Usuario usuario)
        {
            var jwtSecret = _configuration["JWT_SECRET"] ?? "ChaveSecretaPadraoParaDesenvolvimento123!@#";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var isAdmin = usuario.Username == "rogerio@picstone.com.br";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("NomeCompleto", usuario.NomeCompleto),
                new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
            };

            var token = new JwtSecurityToken(
                issuer: "PicStoneFotoAPI",
                audience: "PicStoneFotoApp",
                claims: claims,
                expires: DateTime.UtcNow.AddYears(100),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Cria usuário admin se não existir (rogerio@picstone.com.br/123456)
        /// Sempre garante que o admin existe, independente de outros usuários
        /// </summary>
        public async Task CriarUsuarioInicialAsync()
        {
            try
            {
                // Verifica se o admin já existe
                var adminExistente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Username == "rogerio@picstone.com.br");

                if (adminExistente != null)
                {
                    _logger.LogInformation("Usuário admin já existe");
                    return;
                }

                // Cria usuário admin padrão
                var adminUser = new Usuario
                {
                    Username = "rogerio@picstone.com.br",
                    Email = "rogerio@picstone.com.br",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    NomeCompleto = "Rogério Isidorio",
                    Ativo = true,
                    EmailVerificado = true,
                    Status = StatusUsuario.Aprovado,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Usuarios.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário admin 'rogerio@picstone.com.br' criado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário admin");
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
                return await _context.Usuarios.AnyAsync(u => u.Username == "rogerio@picstone.com.br");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se admin existe");
                return false;
            }
        }

        /// <summary>
        /// Troca senha do usuário (usuário logado)
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string username, string senhaAtual, string novaSenha)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Username == username && u.Ativo);

                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado: {Username}", username);
                    return false;
                }

                // Verifica senha atual
                if (!BCrypt.Net.BCrypt.Verify(senhaAtual, usuario.PasswordHash))
                {
                    _logger.LogWarning("Senha atual incorreta para usuário: {Username}", username);
                    return false;
                }

                // Atualiza senha
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Senha alterada com sucesso para usuário: {Username}", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao trocar senha para usuário: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Cria novo usuário (apenas admin pode criar)
        /// Senha padrão: 123456
        /// </summary>
        public async Task<UserResponse?> CreateUserAsync(string username, string nomeCompleto)
        {
            try
            {
                // Verifica se username já existe
                if (await _context.Usuarios.AnyAsync(u => u.Username == username))
                {
                    _logger.LogWarning("Username já existe: {Username}", username);
                    return null;
                }

                var novoUsuario = new Usuario
                {
                    Username = username,
                    Email = username, // Usa o username como email
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), // Senha padrão
                    NomeCompleto = nomeCompleto,
                    Ativo = true,
                    EmailVerificado = true, // Admin-created users are pre-verified
                    Status = StatusUsuario.Aprovado, // Admin-created users are pre-approved
                    DataCriacao = DateTime.UtcNow
                };

                _context.Usuarios.Add(novoUsuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário criado com sucesso: {Username}", username);

                return new UserResponse
                {
                    Id = novoUsuario.Id,
                    Username = novoUsuario.Username,
                    NomeCompleto = novoUsuario.NomeCompleto,
                    Ativo = novoUsuario.Ativo,
                    DataCriacao = novoUsuario.DataCriacao
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário: {Username}", username);
                return null;
            }
        }

        /// <summary>
        /// Lista todos os usuários (apenas admin)
        /// </summary>
        public async Task<List<UserResponse>> ListUsersAsync()
        {
            try
            {
                var usuarios = await _context.Usuarios
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                return usuarios.Select(u => new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    NomeCompleto = u.NomeCompleto,
                    Ativo = u.Ativo,
                    DataCriacao = u.DataCriacao
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários");
                return new List<UserResponse>();
            }
        }

        /// <summary>
        /// Desativa usuário (soft delete - apenas admin)
        /// </summary>
        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(userId);

                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado: ID {UserId}", userId);
                    return false;
                }

                // Não permite desativar o admin
                if (usuario.Username == "rogerio@picstone.com.br")
                {
                    _logger.LogWarning("Tentativa de desativar usuário admin");
                    return false;
                }

                usuario.Ativo = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário desativado: {Username}", usuario.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar usuário: ID {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Reativa usuário (apenas admin)
        /// Envia email de aprovação
        /// </summary>
        public async Task<(bool Success, string Message)> ReactivateUserAsync(int userId, DateTime? dataExpiracao, EmailService emailService)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(userId);

                if (usuario == null)
                {
                    return (false, "Usuário não encontrado");
                }

                // Reativa usuário
                usuario.Ativo = true;
                usuario.Status = StatusUsuario.Aprovado;
                usuario.DataExpiracao = dataExpiracao;

                await _context.SaveChangesAsync();

                // 🚀 OTIMIZAÇÃO: Envia email em background (não bloqueia)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendApprovalEmailAsync(usuario.Email, usuario.NomeCompleto, dataExpiracao);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email de reativação para: {Email}", usuario.Email);
                    }
                });

                _logger.LogInformation($"Usuário reativado: {usuario.Email}");

                return (true, "Usuário reativado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao reativar usuário ID: {userId}");
                return (false, "Erro ao reativar usuário. Tente novamente.");
            }
        }

        /// <summary>
        /// Registra novo usuário (cadastro público)
        /// Envia email de verificação
        /// </summary>
        public async Task<(bool Success, string Message, Usuario? User)> RegisterUserAsync(RegisterRequest request, EmailService emailService, string? baseUrl = null)
        {
            try
            {
                // MODO TESTE: Remove usuário anterior com mesmo email se existir
                var usuarioExistente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (usuarioExistente != null)
                {
                    _logger.LogInformation($"[MODO TESTE] Removendo usuário existente com email: {request.Email}");
                    _context.Usuarios.Remove(usuarioExistente);
                    await _context.SaveChangesAsync();
                }

                // Usa o email completo como username
                var username = request.Email;

                // Gera token de verificação
                var token = Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString();

                // Cria usuário
                var usuario = new Usuario
                {
                    Username = username,  // Email completo
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                    NomeCompleto = request.NomeCompleto,
                    Ativo = false,  // Inativo até aprovação
                    EmailVerificado = false,
                    TokenVerificacao = token,
                    Status = StatusUsuario.Pendente,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // 🚀 OTIMIZAÇÃO: Envia email em background (não bloqueia cadastro)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendVerificationEmailAsync(usuario.Email, usuario.NomeCompleto, token, baseUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email de verificação para: {Email}", usuario.Email);
                    }
                });

                _logger.LogInformation($"Novo usuário registrado: {usuario.Email} (username: {usuario.Username})");

                return (true, "Cadastro realizado! Verifique seu email para confirmar o cadastro.", usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuário");
                return (false, "Erro ao realizar cadastro. Tente novamente.", null);
            }
        }

        /// <summary>
        /// Verifica email com token
        /// Muda status para AguardandoAprovacao e notifica admin
        /// </summary>
        public async Task<(bool Success, string Message)> VerifyEmailAsync(string token, EmailService emailService)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.TokenVerificacao == token);

                if (usuario == null)
                {
                    return (false, "Token de verificação inválido");
                }

                if (usuario.EmailVerificado)
                {
                    return (false, "Email já verificado");
                }

                // Marca email como verificado
                usuario.EmailVerificado = true;
                usuario.TokenVerificacao = null;
                usuario.Status = StatusUsuario.AguardandoAprovacao;

                await _context.SaveChangesAsync();

                // 🚀 OTIMIZAÇÃO: Notifica admin em background (não bloqueia verificação)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendAdminNotificationAsync(usuario.NomeCompleto, usuario.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar notificação para admin sobre: {Email}", usuario.Email);
                    }
                });

                _logger.LogInformation($"Email verificado: {usuario.Email}");

                return (true, "Email verificado com sucesso! Aguarde a aprovação do administrador.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar email");
                return (false, "Erro ao verificar email. Tente novamente.");
            }
        }

        /// <summary>
        /// Lista usuários aguardando aprovação (apenas admin)
        /// </summary>
        public async Task<List<UserResponse>> ListPendingUsersAsync()
        {
            try
            {
                var usuarios = await _context.Usuarios
                    .Where(u => u.Status == StatusUsuario.AguardandoAprovacao)
                    .OrderBy(u => u.DataCriacao)
                    .ToListAsync();

                return usuarios.Select(u => new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    NomeCompleto = u.NomeCompleto,
                    Ativo = u.Ativo,
                    DataCriacao = u.DataCriacao,
                    Email = u.Email,
                    EmailVerificado = u.EmailVerificado,
                    Status = u.Status.ToString(),
                    DataExpiracao = u.DataExpiracao
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários pendentes");
                return new List<UserResponse>();
            }
        }

        /// <summary>
        /// Aprova usuário (apenas admin)
        /// Envia email de aprovação
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveUserAsync(int userId, DateTime? dataExpiracao, EmailService emailService)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(userId);

                if (usuario == null)
                {
                    return (false, "Usuário não encontrado");
                }

                if (usuario.Status != StatusUsuario.AguardandoAprovacao)
                {
                    return (false, "Usuário não está aguardando aprovação");
                }

                // Aprova usuário
                usuario.Status = StatusUsuario.Aprovado;
                usuario.Ativo = true;
                usuario.DataExpiracao = dataExpiracao;

                await _context.SaveChangesAsync();

                // 🚀 OTIMIZAÇÃO: Envia email em background (não bloqueia aprovação)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendApprovalEmailAsync(usuario.Email, usuario.NomeCompleto, dataExpiracao);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email de aprovação para: {Email}", usuario.Email);
                    }
                });

                _logger.LogInformation($"Usuário aprovado: {usuario.Email}");

                return (true, "Usuário aprovado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao aprovar usuário ID: {userId}");
                return (false, "Erro ao aprovar usuário. Tente novamente.");
            }
        }

        /// <summary>
        /// Rejeita usuário (apenas admin)
        /// Envia email de rejeição
        /// </summary>
        public async Task<(bool Success, string Message)> RejectUserAsync(int userId, string? motivo, EmailService emailService)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(userId);

                if (usuario == null)
                {
                    return (false, "Usuário não encontrado");
                }

                if (usuario.Status != StatusUsuario.AguardandoAprovacao)
                {
                    return (false, "Usuário não está aguardando aprovação");
                }

                // Rejeita usuário
                usuario.Status = StatusUsuario.Rejeitado;
                usuario.Ativo = false;

                await _context.SaveChangesAsync();

                // 🚀 OTIMIZAÇÃO: Envia email em background (não bloqueia rejeição)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendRejectionEmailAsync(usuario.Email, usuario.NomeCompleto, motivo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email de rejeição para: {Email}", usuario.Email);
                    }
                });

                _logger.LogInformation($"Usuário rejeitado: {usuario.Email}");

                return (true, "Usuário rejeitado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao rejeitar usuário ID: {userId}");
                return (false, "Erro ao rejeitar usuário. Tente novamente.");
            }
        }
    }
}
