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

        [Required(ErrorMessage = "Número do lote é obrigatório")]
        public string Lote { get; set; } = string.Empty;

        [Required(ErrorMessage = "Número da chapa é obrigatório")]
        public string Chapa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Processo é obrigatório")]
        public string Processo { get; set; } = string.Empty;

        public int? Espessura { get; set; }
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
