namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Modelo da tabela Materiais
    /// </summary>
    public class Material
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; } = 0;
    }
}
