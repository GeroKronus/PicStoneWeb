using System.ComponentModel.DataAnnotations;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Request para criar novo usuário (apenas admin)
    /// </summary>
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Username é obrigatório")]
        [MinLength(3, ErrorMessage = "Username deve ter no mínimo 3 caracteres")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome completo é obrigatório")]
        public string NomeCompleto { get; set; } = string.Empty;
    }
}
