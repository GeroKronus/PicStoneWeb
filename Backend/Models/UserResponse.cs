namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Response com dados do usuário (sem senha)
    /// </summary>
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
