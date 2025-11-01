using SkiaSharp;
using PicStoneFotoAPI.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace PicStoneFotoAPI
{
    public class TestBancada1
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== TESTE BANCADA1 ===");

            // Configurar logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var transformLogger = loggerFactory.CreateLogger<GraphicsTransformService>();
            var bancadaLogger = loggerFactory.CreateLogger<BancadaService>();

            // Criar serviços
            var transformService = new GraphicsTransformService(transformLogger);
            var bancadaService = new BancadaService(bancadaLogger, transformService);

            // Carregar imagem de teste (a imagem que o usuário forneceu)
            string testImagePath = @"D:\Claude Code\PicStone WEB\Backend\TestImages\pedra_teste.png";

            if (!File.Exists(testImagePath))
            {
                Console.WriteLine($"ERRO: Imagem não encontrada em {testImagePath}");
                Console.WriteLine("Por favor, salve a imagem da pedra como 'pedra_teste.png' na pasta TestImages");
                return;
            }

            Console.WriteLine($"Carregando imagem: {testImagePath}");
            using var stream = File.OpenRead(testImagePath);
            var imagemOriginal = SKBitmap.Decode(stream);

            Console.WriteLine($"Imagem carregada: {imagemOriginal.Width}x{imagemOriginal.Height}");

            // Gerar mockups
            Console.WriteLine("\nGerando mockups...");
            var mockups = bancadaService.GerarBancada1(imagemOriginal, flip: false);

            Console.WriteLine($"\nMockups gerados: {mockups.Count}");

            // Salvar resultados
            string outputDir = @"D:\Claude Code\PicStone WEB\Backend\TestOutput";
            Directory.CreateDirectory(outputDir);

            for (int i = 0; i < mockups.Count; i++)
            {
                string filename = Path.Combine(outputDir, $"bancada1_resultado_{i + 1}.jpg");
                using var image = SKImage.FromBitmap(mockups[i]);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
                using var fileStream = File.OpenWrite(filename);
                data.SaveTo(fileStream);
                Console.WriteLine($"Salvo: {filename} ({mockups[i].Width}x{mockups[i].Height})");
            }

            Console.WriteLine("\n=== TESTE CONCLUÍDO ===");
            Console.WriteLine($"Verifique os arquivos em: {outputDir}");
            Console.WriteLine($"E os arquivos de debug em: D:\\Claude Code\\PicStone WEB\\Backend\\DEBUG_Bancada1");
        }
    }
}
