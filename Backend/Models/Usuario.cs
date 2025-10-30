namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Modelo de usuário para autenticação
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}
