using SkiaSharp;

var imagesPath = @"..\Backend\wwwroot\images";
var targetWidth = 300; // Similar ao icon-ambientes.png em proporção

Console.WriteLine("Iniciando otimização de thumbnails...\n");

for (int i = 1; i <= 8; i++)
{
    var inputFile = Path.Combine(imagesPath, $"thumb-bancada{i}.png");
    var backupFile = Path.Combine(imagesPath, $"thumb-bancada{i}.png.backup");

    if (!File.Exists(inputFile))
    {
        Console.WriteLine($"❌ Arquivo não encontrado: {inputFile}");
        continue;
    }

    // Backup da original
    if (!File.Exists(backupFile))
    {
        File.Copy(inputFile, backupFile);
        Console.WriteLine($"✓ Backup criado: {backupFile}");
    }

    // Carregar imagem original
    using var inputStream = File.OpenRead(inputFile);
    using var original = SKBitmap.Decode(inputStream);

    var originalSize = new FileInfo(inputFile).Length;

    // Calcular nova altura mantendo proporção
    var aspectRatio = (float)original.Height / original.Width;
    var targetHeight = (int)(targetWidth * aspectRatio);

    // Redimensionar
    var imageInfo = new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var resized = original.Resize(imageInfo, SKFilterQuality.High);

    if (resized == null)
    {
        Console.WriteLine($"❌ Erro ao redimensionar: {inputFile}");
        continue;
    }

    // Salvar com qualidade otimizada
    using var image = SKImage.FromBitmap(resized);
    using var data = image.Encode(SKEncodedImageFormat.Png, 85);
    File.WriteAllBytes(inputFile, data.ToArray());

    var newSize = new FileInfo(inputFile).Length;
    var reduction = ((originalSize - newSize) / (float)originalSize) * 100;

    Console.WriteLine($"✓ Bancada {i}: {original.Width}x{original.Height} ({originalSize / 1024 / 1024:F2} MB) → {targetWidth}x{targetHeight} ({newSize / 1024:F2} KB) | Redução: {reduction:F1}%");
}

Console.WriteLine("\n✓ Otimização concluída!");
