namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Modelo da tabela FotosMobile no SQL Server
    /// </summary>
    public class FotoMobile
    {
        public int Id { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Chapa { get; set; } = string.Empty;
        public string Processo { get; set; } = string.Empty;
        public int? Espessura { get; set; }
        public DateTime DataUpload { get; set; } = DateTime.Now;
        public string Usuario { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
    }
}
