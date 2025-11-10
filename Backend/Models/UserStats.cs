namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Estatísticas de uso do usuário
    /// </summary>
    public class UserStats
    {
        public int TotalLogins { get; set; }
        public int TotalAmbientesGerados { get; set; }
        public DateTime? PrimeiroAcesso { get; set; }
        public DateTime? UltimoAcesso { get; set; }
    }
}
