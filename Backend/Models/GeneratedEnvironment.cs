using System.ComponentModel.DataAnnotations;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Registro de ambiente gerado pelo usuário
    /// </summary>
    public class GeneratedEnvironment
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string TipoAmbiente { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Material { get; set; }

        [MaxLength(50)]
        public string? Bloco { get; set; }

        [MaxLength(50)]
        public string? Chapa { get; set; }

        [MaxLength(500)]
        public string? Detalhes { get; set; }  // JSON com parâmetros extras

        public int QuantidadeImagens { get; set; } = 1;

        // Navigation property
        public Usuario Usuario { get; set; } = null!;
    }
}
