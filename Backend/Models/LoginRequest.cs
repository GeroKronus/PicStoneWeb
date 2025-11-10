using System.ComponentModel.DataAnnotations;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Request para login de usuário
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Usuário é obrigatório")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response do login com token JWT
    /// </summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsAdmin { get; set; }
    }
}
