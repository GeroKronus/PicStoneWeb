namespace PicStoneFotoAPI.Models
{
    public class MockupRequest
    {
        /// <summary>
        /// ID da foto original no banco
        /// </summary>
        public int FotoId { get; set; }

        /// <summary>
        /// Imagem cropada enviada pelo usuário (crop interno)
        /// </summary>
        public IFormFile? ImagemCropada { get; set; }

        /// <summary>
        /// Tipo de cavalete: "simples" ou "duplo"
        /// </summary>
        public string TipoCavalete { get; set; } = "simples";

        /// <summary>
        /// Fundo: "claro" ou "escuro"
        /// </summary>
        public string Fundo { get; set; } = "claro";
    }

    public class MockupResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public List<string> CaminhosGerados { get; set; } = new();
    }
}
