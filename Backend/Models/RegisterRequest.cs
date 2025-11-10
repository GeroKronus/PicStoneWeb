using System.ComponentModel.DataAnnotations;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Request para cadastro público de novo usuário
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Nome completo é obrigatório")]
        [MinLength(3, ErrorMessage = "Nome deve ter no mínimo 3 caracteres")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
        public string Senha { get; set; } = string.Empty;
    }
}
