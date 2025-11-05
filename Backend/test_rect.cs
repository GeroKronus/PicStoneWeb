using SkiaSharp;
using System;

class TestRect {
    static void Main() {
        // Teste: construtor SKRectI(int left, int top, int right, int bottom)
        // Para uma imagem 2000x1863:
        int width = 2000;
        int height = 1863;
        int doisTercos = (int)(width / 1.5); // 1333
        int umTerco = width - doisTercos; // 667
        
        Console.WriteLine($"Width: {width}, Height: {height}");
        Console.WriteLine($"doisTercos: {doisTercos}, umTerco: {umTerco}");
        
        // Linha 742: new SKRectI(0, 0, doisTercos, imagemBookMatch.Height)
        // Seria: Left=0, Top=0, Right=1333, Bottom=1863 ✓ CORRETO
        var rect1 = new SKRectI(0, 0, doisTercos, height);
        Console.WriteLine($"rect1: {rect1} (Width={rect1.Width}, Height={rect1.Height})");
        
        // Linha 743: new SKRectI(doisTercos - 10, 0, umTerco + 10, imagemBookMatch.Height)
        // Seria: Left=1323, Top=0, Right=677, Bottom=1863 ✗ ERRADO (Right < Left!)
        try {
            var rect2 = new SKRectI(doisTercos - 10, 0, umTerco + 10, height);
            Console.WriteLine($"rect2: {rect2} (Width={rect2.Width}, Height={rect2.Height})");
        } catch (Exception ex) {
            Console.WriteLine($"ERRO rect2: {ex.Message}");
        }
        
        // CORRETO seria: Left=1323, Top=0, Right=2000+10=2010?, Bottom=1863
        // Mas não pode ultrapassar a imagem...
        int left = doisTercos - 10;
        int right = width; // ou doisTercos - 10 + (umTerco + 10) = 1990
        var rect3 = new SKRectI(left, 0, right, height);
        Console.WriteLine($"rect3 CORRETO: {rect3} (Width={rect3.Width}, Height={rect3.Height})");
    }
}
