namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Modelo de usuário para autenticação
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public bool EmailVerificado { get; set; } = false;
        public string? TokenVerificacao { get; set; }
        public StatusUsuario Status { get; set; } = StatusUsuario.Pendente;
        public DateTime? DataExpiracao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? UltimoAcesso { get; set; }  // Último login registrado

        // Navigation properties para histórico
        public ICollection<UserLogin> Logins { get; set; } = new List<UserLogin>();
        public ICollection<GeneratedEnvironment> AmbientesGerados { get; set; } = new List<GeneratedEnvironment>();
    }

    /// <summary>
    /// Status do usuário no sistema
    /// </summary>
    public enum StatusUsuario
    {
        Pendente = 0,              // Aguardando confirmação de email
        AguardandoAprovacao = 1,   // Email confirmado, aguardando admin
        Aprovado = 2,              // Admin aprovou, pode acessar
        Rejeitado = 3,             // Admin rejeitou acesso
        Expirado = 4               // Data de expiração passou
    }
}
