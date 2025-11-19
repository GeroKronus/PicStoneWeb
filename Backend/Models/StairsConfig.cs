using System.Collections.Generic;

namespace PicStoneFotoAPI.Models
{
    /// <summary>
    /// Configuração de um elemento individual da escada (degrau ou espelho)
    /// </summary>
    public class StairsElementConfig
    {
        public string Nome { get; set; } = string.Empty;
        public StairsElementType Tipo { get; set; }

        // Parâmetros de Extração
        public int PosicaoX { get; set; }
        public int Largura { get; set; }

        // Parâmetros de Transformação para Degraus
        public int? LadoMaior { get; set; }
        public int? LadoMenor { get; set; }
        public int? NovaLargura { get; set; }
        public int? NovaAltura { get; set; }
        public int? Acrescimo { get; set; }
        public int? FatorSkew { get; set; }

        // Parâmetros de Transformação para Espelhos (usa NovaLargura e NovaAltura)

        // Parâmetros de Posicionamento no Canvas
        public int CanvasX { get; set; }
        public int CanvasY { get; set; }
    }

    /// <summary>
    /// Tipo de elemento da escada
    /// </summary>
    public enum StairsElementType
    {
        Degrau,
        Espelho
    }

    /// <summary>
    /// Configuração completa de uma escada
    /// </summary>
    public class StairsConfig
    {
        public int NumeroEscada { get; set; }
        public int LarguraPadrao { get; set; }
        public int LarguraCanvas { get; set; }
        public int AlturaCanvas { get; set; }
        public string ArquivoOverlay { get; set; } = string.Empty;
        public bool RotacaoFinal { get; set; } = false;
        public float AnguloRotacaoFinal { get; set; } = 0;
        public List<StairsElementConfig> Elementos { get; set; } = new List<StairsElementConfig>();
    }
}