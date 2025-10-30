namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Modelo da tabela FotosMobile no SQL Server
    /// </summary>
    public class FotoMobile
    {
        public int Id { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
        public string? Material { get; set; }
        public string? Bloco { get; set; }
        public string? Lote { get; set; } // Mantido para compatibilidade
        public string Chapa { get; set; } = string.Empty;
        public string? Processo { get; set; } // Mantido para compatibilidade
        public int? Espessura { get; set; }
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;
        public string Usuario { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
    }
}
