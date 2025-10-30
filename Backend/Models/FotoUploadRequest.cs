using System.ComponentModel.DataAnnotations;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Request para upload de foto com metadados
    /// </summary>
    public class FotoUploadRequest
    {
        [Required(ErrorMessage = "Arquivo é obrigatório")]
        public IFormFile Arquivo { get; set; } = null!;

        [Required(ErrorMessage = "Nome do material é obrigatório")]
        public string Material { get; set; } = string.Empty;

        [Required(ErrorMessage = "Número do bloco é obrigatório")]
        public string Bloco { get; set; } = string.Empty;

        [Required(ErrorMessage = "Número da chapa é obrigatório")]
        public string Chapa { get; set; } = string.Empty;

        public int? Espessura { get; set; }

        // Campos antigos mantidos para compatibilidade
        public string? Lote { get; set; }
        public string? Processo { get; set; }
    }

    /// <summary>
    /// Response do upload de foto
    /// </summary>
    public class FotoUploadResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string NomeArquivo { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
    }
}
