using System;
using System.Collections.Generic;
using System.IO;
using PicStoneFotoAPI.Models;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Factory para criar configurações de escadas
    /// Centraliza todos os parâmetros hardcoded em um só lugar (DRY)
    /// </summary>
    public static class StairsConfigFactory
    {
        private static readonly string _resourcesPath =
            Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Escadas");

        /// <summary>
        /// Configuração da Escada #1 (já implementada)
        /// 4 degraus + 6 espelhos
        /// </summary>
        public static StairsConfig GetStairs1Config()
        {
            return new StairsConfig
            {
                NumeroEscada = 1,
                LarguraPadrao = 2800,
                LarguraCanvas = 2000,
                AlturaCanvas = 2900,
                ArquivoOverlay = "escada1.webp",
                Elementos = new List<StairsElementConfig>
                {
                    // DEGRAUS
                    new StairsElementConfig
                    {
                        Nome = "degrau1",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2500, Largura = 300,
                        LadoMaior = 2181, LadoMenor = 1600,
                        NovaLargura = 420, NovaAltura = 2185,
                        Acrescimo = 0, FatorSkew = 300,
                        CanvasX = -358, CanvasY = 2484
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau2",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2100, Largura = 300,
                        LadoMaior = 1625, LadoMenor = 1310,
                        NovaLargura = 188, NovaAltura = 1623,
                        Acrescimo = 0, FatorSkew = 180,
                        CanvasX = 40, CanvasY = 2027
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau3",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 1700, Largura = 300,
                        LadoMaior = 1298, LadoMenor = 1080,
                        NovaLargura = 86, NovaAltura = 1298,
                        Acrescimo = 0, FatorSkew = 125,
                        CanvasX = 249, CanvasY = 1728
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau4",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 1300, Largura = 300,
                        LadoMaior = 1084, LadoMenor = 939,
                        NovaLargura = 36, NovaAltura = 1095,
                        Acrescimo = 0, FatorSkew = 70,
                        CanvasX = 402, CanvasY = 1510
                    },

                    // ESPELHOS
                    new StairsElementConfig
                    {
                        Nome = "espelho1",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2400, Largura = 100,
                        NovaLargura = 138, NovaAltura = 1620,
                        CanvasX = 221, CanvasY = 2216
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho2",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2000, Largura = 100,
                        NovaLargura = 117, NovaAltura = 1305,
                        CanvasX = 369, CanvasY = 1812
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho3",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1600, Largura = 100,
                        NovaLargura = 99, NovaAltura = 1091,
                        CanvasX = 475, CanvasY = 1546
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho4",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1200, Largura = 100,
                        NovaLargura = 88, NovaAltura = 929,
                        CanvasX = 549, CanvasY = 1350
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho5",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 800, Largura = 100,
                        NovaLargura = 76, NovaAltura = 808,
                        CanvasX = 607, CanvasY = 1213
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho6",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 400, Largura = 100,
                        NovaLargura = 69, NovaAltura = 717,
                        CanvasX = 651, CanvasY = 1120
                    }
                }
            };
        }

        /// <summary>
        /// Configuração da Escada #2
        /// 6 degraus + 5 espelhos, rotação final 30°
        /// </summary>
        public static StairsConfig GetStairs2Config()
        {
            return new StairsConfig
            {
                NumeroEscada = 2,
                LarguraPadrao = 2800,
                LarguraCanvas = 3000,
                AlturaCanvas = 3000,
                ArquivoOverlay = "escada2.webp",
                RotacaoFinal = true,
                AnguloRotacaoFinal = 30,
                Elementos = new List<StairsElementConfig>
                {
                    // DEGRAUS - Escada 2 usa Skew simples ao invés de Distortion
                    new StairsElementConfig
                    {
                        Nome = "degrau1",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2530, Largura = 270,
                        FatorSkew = 400, // Apenas Skew para Escada 2
                        CanvasX = 2050, CanvasY = 1400
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau2",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2090, Largura = 270,
                        FatorSkew = 400,
                        CanvasX = 1700, CanvasY = 1266
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau3",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 1650, Largura = 270,
                        FatorSkew = 400,
                        CanvasX = 1350, CanvasY = 1132
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau4",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 1210, Largura = 270,
                        FatorSkew = 400,
                        CanvasX = 1000, CanvasY = 998
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau5",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 770, Largura = 270,
                        FatorSkew = 400,
                        CanvasX = 650, CanvasY = 864
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau6",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 330, Largura = 540,
                        FatorSkew = 400,
                        CanvasX = 300, CanvasY = 730
                    },

                    // ESPELHOS - Escada 2 usa RotateImage3 (18.45°)
                    new StairsElementConfig
                    {
                        Nome = "espelho1",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2360, Largura = 170,
                        FatorSkew = 100, // Espelhos também usam Skew
                        CanvasX = 1612, CanvasY = 1070
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho2",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1920, Largura = 170,
                        FatorSkew = 100,
                        CanvasX = 1262, CanvasY = 937
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho3",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1480, Largura = 170,
                        FatorSkew = 100,
                        CanvasX = 912, CanvasY = 804
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho4",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1040, Largura = 170,
                        FatorSkew = 100,
                        CanvasX = 562, CanvasY = 671
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho5",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 600, Largura = 170,
                        FatorSkew = 100,
                        CanvasX = 212, CanvasY = 538
                    }
                }
            };
        }

        /// <summary>
        /// Configuração da Escada #3
        /// 2 degraus + 16 espelhos
        /// </summary>
        public static StairsConfig GetStairs3Config()
        {
            return new StairsConfig
            {
                NumeroEscada = 3,
                LarguraPadrao = 3200,
                LarguraCanvas = 2000,
                AlturaCanvas = 1372,
                ArquivoOverlay = "escada3.webp",
                Elementos = new List<StairsElementConfig>
                {
                    // DEGRAUS - Escada 3 usa DistortionInclina
                    new StairsElementConfig
                    {
                        Nome = "degrau1",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2920, Largura = 280,
                        LadoMaior = 2200, LadoMenor = 1750,
                        NovaLargura = 100, NovaAltura = 2200,
                        Acrescimo = 0, FatorSkew = 0,
                        CanvasX = -350, CanvasY = 1256
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau2",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2560, Largura = 280,
                        LadoMaior = 1720, LadoMenor = 1500,
                        NovaLargura = 30, NovaAltura = 1720,
                        Acrescimo = 40, FatorSkew = 20,
                        CanvasX = 32, CanvasY = 1041
                    },

                    // ESPELHOS - 16 espelhos com transformações progressivas
                    new StairsElementConfig
                    {
                        Nome = "espelho1",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2740, Largura = 180,
                        LadoMaior = 208, LadoMenor = 170,
                        NovaLargura = 1750, NovaAltura = 208,
                        CanvasX = 110, CanvasY = 1102
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho2",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2380, Largura = 180,
                        LadoMaior = 173, LadoMenor = 155,
                        NovaLargura = 1530, NovaAltura = 177,
                        CanvasX = 295, CanvasY = 725
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho3",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1920, Largura = 180,
                        LadoMaior = 151, LadoMenor = 136,
                        NovaLargura = 1360, NovaAltura = 155,
                        CanvasX = 392, CanvasY = 756
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho4",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1460, Largura = 180,
                        LadoMaior = 120, LadoMenor = 115,
                        NovaLargura = 1220, NovaAltura = 115,
                        CanvasX = 475, CanvasY = 646
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho5",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1000, Largura = 180,
                        LadoMaior = 110, LadoMenor = 95,
                        NovaLargura = 1110, NovaAltura = 110,
                        CanvasX = 550, CanvasY = 552
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho6",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 540, Largura = 180,
                        LadoMaior = 76, LadoMenor = 76,
                        NovaLargura = 1010, NovaAltura = 76,
                        CanvasX = 619, CanvasY = 478
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho7",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 80, Largura = 180,
                        LadoMaior = 65, LadoMenor = 65,
                        NovaLargura = 935, NovaAltura = 65,
                        CanvasX = 667, CanvasY = 413
                    },
                    // Espelhos 8-16 com Flip Y
                    new StairsElementConfig
                    {
                        Nome = "espelho8",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2740, Largura = 180,
                        LadoMaior = 56, LadoMenor = 56,
                        NovaLargura = 867, NovaAltura = 56,
                        CanvasX = 707, CanvasY = 358
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho9",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2380, Largura = 180,
                        LadoMaior = 50, LadoMenor = 50,
                        NovaLargura = 810, NovaAltura = 50,
                        CanvasX = 736, CanvasY = 309
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho10",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1920, Largura = 180,
                        LadoMaior = 46, LadoMenor = 46,
                        NovaLargura = 760, NovaAltura = 46,
                        CanvasX = 753, CanvasY = 273
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho11",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1460, Largura = 180,
                        LadoMaior = 40, LadoMenor = 40,
                        NovaLargura = 710, NovaAltura = 40,
                        CanvasX = 774, CanvasY = 238
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho12",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1000, Largura = 180,
                        LadoMaior = 33, LadoMenor = 33,
                        NovaLargura = 675, NovaAltura = 33,
                        CanvasX = 782, CanvasY = 210
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho13",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 540, Largura = 180,
                        LadoMaior = 32, LadoMenor = 32,
                        NovaLargura = 637, NovaAltura = 32,
                        CanvasX = 800, CanvasY = 183
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho14",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 80, Largura = 180,
                        LadoMaior = 26, LadoMenor = 26,
                        NovaLargura = 604, NovaAltura = 26,
                        CanvasX = 805, CanvasY = 160
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho15",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 540, Largura = 180,
                        LadoMaior = 26, LadoMenor = 26,
                        NovaLargura = 575, NovaAltura = 26,
                        CanvasX = 808, CanvasY = 138
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho16",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 80, Largura = 180,
                        LadoMaior = 25, LadoMenor = 25,
                        NovaLargura = 546, NovaAltura = 25,
                        CanvasX = 812, CanvasY = 116
                    }
                }
            };
        }

        /// <summary>
        /// Configuração da Escada #4
        /// 6 degraus + 12 espelhos (montagem espelhada)
        /// </summary>
        public static StairsConfig GetStairs4Config()
        {
            return new StairsConfig
            {
                NumeroEscada = 4,
                LarguraPadrao = 3200,
                LarguraCanvas = 2000,
                AlturaCanvas = 1333,
                ArquivoOverlay = "escada4.webp",
                Elementos = new List<StairsElementConfig>
                {
                    // DEGRAUS - DistortionInclina (pares espelhados)
                    new StairsElementConfig
                    {
                        Nome = "degrau1",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2920, Largura = 280,
                        LadoMaior = 767, LadoMenor = 629,
                        NovaLargura = 143, NovaAltura = 767,
                        CanvasX = 233, CanvasY = 1190
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau1_flip",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2920, Largura = 280,
                        LadoMaior = 767, LadoMenor = 629,
                        NovaLargura = 143, NovaAltura = 767,
                        CanvasX = 1000, CanvasY = 1190 // Espelhado na direita
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau2",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2460, Largura = 280,
                        LadoMaior = 629, LadoMenor = 533,
                        NovaLargura = 77, NovaAltura = 629,
                        CanvasX = 323, CanvasY = 966
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau2_flip",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2460, Largura = 280,
                        LadoMaior = 629, LadoMenor = 533,
                        NovaLargura = 77, NovaAltura = 629,
                        CanvasX = 1000, CanvasY = 966
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau3",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2000, Largura = 280,
                        LadoMaior = 533, LadoMenor = 461,
                        NovaLargura = 46, NovaAltura = 533,
                        CanvasX = 392, CanvasY = 793
                    },
                    new StairsElementConfig
                    {
                        Nome = "degrau3_flip",
                        Tipo = StairsElementType.Degrau,
                        PosicaoX = 2000, Largura = 280,
                        LadoMaior = 533, LadoMenor = 461,
                        NovaLargura = 46, NovaAltura = 533,
                        CanvasX = 1000, CanvasY = 793
                    },

                    // ESPELHOS - 12 (6 pares)
                    new StairsElementConfig
                    {
                        Nome = "espelho1",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2740, Largura = 180,
                        NovaLargura = 108, NovaAltura = 629,
                        CanvasX = 371, CanvasY = 1082
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho1_flip",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2740, Largura = 180,
                        NovaLargura = 108, NovaAltura = 629,
                        CanvasX = 1000, CanvasY = 1082
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho2",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2280, Largura = 180,
                        NovaLargura = 91, NovaAltura = 533,
                        CanvasX = 398, CanvasY = 877
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho2_flip",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 2280, Largura = 180,
                        NovaLargura = 91, NovaAltura = 533,
                        CanvasX = 1000, CanvasY = 877
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho3",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1820, Largura = 180,
                        NovaLargura = 79, NovaAltura = 461,
                        CanvasX = 436, CanvasY = 720
                    },
                    new StairsElementConfig
                    {
                        Nome = "espelho3_flip",
                        Tipo = StairsElementType.Espelho,
                        PosicaoX = 1820, Largura = 180,
                        NovaLargura = 79, NovaAltura = 461,
                        CanvasX = 1000, CanvasY = 720
                    }
                }
            };
        }

        /// <summary>
        /// Obtém a configuração de uma escada pelo número
        /// </summary>
        public static StairsConfig GetStairsConfig(int numeroEscada)
        {
            return numeroEscada switch
            {
                1 => GetStairs1Config(),
                2 => GetStairs2Config(),
                3 => GetStairs3Config(),
                4 => GetStairs4Config(),
                _ => throw new ArgumentException($"Escada #{numeroEscada} não está configurada")
            };
        }
    }
}