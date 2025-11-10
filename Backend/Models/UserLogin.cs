using System.ComponentModel.DataAnnotations;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Registro de login/acesso do usuário
    /// </summary>
    public class UserLogin
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        // Navigation property
        public Usuario Usuario { get; set; } = null!;
    }
}
