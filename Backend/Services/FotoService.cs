using PicStoneFotoAPI.Data;
using PicStoneFotoAPI.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;
using FluentFTP;
using SkiaSharp;
using Microsoft.EntityFrameworkCore;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para processamento e upload de fotos
    /// </summary>
    public class FotoService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FotoService> _logger;
        private readonly string _uploadPath;
        private static readonly string[] PermittedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public FotoService(AppDbContext context, IConfiguration configuration, ILogger<FotoService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _uploadPath = _configuration["UPLOAD_PATH"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");

            // Cria diretório de upload se não existir
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
                _logger.LogInformation("Diretório de upload criado: {Path}", _uploadPath);
            }
        }

        /// <summary>
        /// Processa upload de foto com validações e salvamento
        /// </summary>
        public async Task<FotoUploadResponse> ProcessarUploadAsync(FotoUploadRequest request, ClaimsPrincipal user)
        {
            try
            {
                // Validações
                var validacao = ValidarArquivo(request.Arquivo);
                if (!validacao.valido)
                {
                    return new FotoUploadResponse
                    {
                        Sucesso = false,
                        Mensagem = validacao.erro
                    };
                }

                // Gera nome do arquivo padronizado
                var nomeArquivo = GerarNomeArquivo(request.Bloco, request.Chapa);
                var caminhoCompleto = Path.Combine(_uploadPath, nomeArquivo);

                // Salva arquivo no disco
                await SalvarArquivoAsync(request.Arquivo, caminhoCompleto);

                // Adicionar legenda na imagem (não bloqueia upload se falhar)
                try
                {
                    await AdicionarLegendaNaImagemAsync(caminhoCompleto, request);
                }
                catch (Exception exLegenda)
                {
                    _logger.LogWarning(exLegenda, "Falha ao adicionar legenda, continuando com upload");
                }

                // Registra no banco de dados
                var username = user.Identity?.Name ?? "Desconhecido";
                var fotoMobile = new FotoMobile
                {
                    NomeArquivo = nomeArquivo,
                    Material = SanitizarString(request.Material),
                    Bloco = SanitizarString(request.Bloco),
                    Lote = request.Lote,
                    Chapa = SanitizarString(request.Chapa),
                    Processo = request.Processo,
                    Espessura = request.Espessura,
                    DataUpload = DateTime.UtcNow,
                    Usuario = username,
                    CaminhoArquivo = caminhoCompleto
                };

                _context.FotosMobile.Add(fotoMobile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Foto salva com sucesso: {NomeArquivo} por {Usuario}", nomeArquivo, username);

                // Tenta transferir via FTP (em background, não bloqueia resposta)
                _ = Task.Run(() => TentarTransferenciaFTPAsync(caminhoCompleto, nomeArquivo));

                return new FotoUploadResponse
                {
                    Sucesso = true,
                    Mensagem = "Foto enviada com sucesso!",
                    NomeArquivo = nomeArquivo,
                    CaminhoArquivo = caminhoCompleto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar upload de foto");

                var innerMsg = ex.InnerException?.Message ?? "";
                var fullMessage = $"Erro ao processar foto: {ex.Message}";

                if (!string.IsNullOrEmpty(innerMsg))
                {
                    fullMessage += $" | Detalhe: {innerMsg}";
                }

                return new FotoUploadResponse
                {
                    Sucesso = false,
                    Mensagem = fullMessage
                };
            }
        }

        /// <summary>
        /// Retorna histórico das últimas 50 fotos
        /// </summary>
        public async Task<List<FotoMobile>> ObterHistoricoAsync(int limite = 50)
        {
            try
            {
                var fotos = await _context.FotosMobile
                    .OrderByDescending(f => f.DataUpload)
                    .Take(limite)
                    .ToListAsync();

                _logger.LogInformation("Histórico obtido: {Count} fotos", fotos?.Count ?? 0);
                return fotos ?? new List<FotoMobile>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter histórico");
                throw;
            }
        }

        /// <summary>
        /// Valida tipo e tamanho do arquivo
        /// </summary>
        private (bool valido, string erro) ValidarArquivo(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                return (false, "Arquivo não fornecido ou vazio");
            }

            if (arquivo.Length > MaxFileSize)
            {
                return (false, $"Arquivo excede o tamanho máximo de {MaxFileSize / 1024 / 1024}MB");
            }

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extensao) || !PermittedExtensions.Contains(extensao))
            {
                return (false, "Tipo de arquivo não permitido. Use JPG, JPEG ou PNG");
            }

            // Validação adicional de MIME type
            var mimeType = arquivo.ContentType.ToLowerInvariant();
            if (!mimeType.StartsWith("image/"))
            {
                return (false, "O arquivo não é uma imagem válida");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Gera nome do arquivo no formato BLOCO_CHAPA_YYYYMMDD_HHMMSS.jpg
        /// </summary>
        private string GerarNomeArquivo(string bloco, string chapa)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var blocoSanitizado = SanitizarNomeArquivo(bloco);
            var chapaSanitizada = SanitizarNomeArquivo(chapa);
            return $"{blocoSanitizado}_{chapaSanitizada}_{timestamp}.jpg";
        }

        /// <summary>
        /// Remove caracteres inválidos de nome de arquivo
        /// </summary>
        private string SanitizarNomeArquivo(string nome)
        {
            var invalidos = Path.GetInvalidFileNameChars();
            return string.Join("", nome.Split(invalidos, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Sanitiza string para SQL (previne injection)
        /// </summary>
        private string SanitizarString(string input)
        {
            return Regex.Replace(input, @"[^\w\s-]", "");
        }

        /// <summary>
        /// Salva arquivo no disco
        /// </summary>
        private async Task SalvarArquivoAsync(IFormFile arquivo, string caminho)
        {
            using var stream = new FileStream(caminho, FileMode.Create);
            await arquivo.CopyToAsync(stream);
        }

        /// <summary>
        /// Adiciona legenda com dados na imagem (canto superior esquerdo, texto branco)
        /// </summary>
        private Task AdicionarLegendaNaImagemAsync(string caminhoImagem, FotoUploadRequest request)
        {
            try
            {
                _logger.LogInformation("=== INICIANDO ADIÇÃO DE LEGENDA ===");
                _logger.LogInformation("Caminho: {Caminho}", caminhoImagem);
                _logger.LogInformation("Material: {Material}, Bloco: {Bloco}, Chapa: {Chapa}",
                    request.Material, request.Bloco, request.Chapa);

                // Verifica se arquivo existe
                if (!File.Exists(caminhoImagem))
                {
                    _logger.LogError("Arquivo não encontrado para adicionar legenda: {Caminho}", caminhoImagem);
                    return Task.CompletedTask;
                }

                var tamanhoAntes = new FileInfo(caminhoImagem).Length;
                _logger.LogInformation("Tamanho do arquivo antes: {Tamanho} bytes", tamanhoAntes);

                // Carrega a imagem
                _logger.LogInformation("Carregando imagem...");
                using var inputStream = File.OpenRead(caminhoImagem);
                using var original = SKBitmap.Decode(inputStream);
                inputStream.Close();

                if (original == null)
                {
                    _logger.LogError("Falha ao decodificar imagem");
                    return Task.CompletedTask;
                }

                _logger.LogInformation("Imagem carregada: {Width}x{Height}", original.Width, original.Height);

                // Cria uma superfície para desenhar
                var imageInfo = new SKImageInfo(original.Width, original.Height);
                using var surface = SKSurface.Create(imageInfo);

                if (surface == null)
                {
                    _logger.LogError("Falha ao criar superfície de desenho");
                    return Task.CompletedTask;
                }

                var canvas = surface.Canvas;

                // Desenha a imagem original
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(original, 0, 0);
                _logger.LogInformation("Imagem original desenhada no canvas");

                // Configuração do texto (usando fonte padrão, sem especificar família)
                using var paint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = 40,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Typeface = SKTypeface.Default
                };

                // Configuração da sombra/outline para melhor legibilidade
                using var shadowPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 40,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3,
                    Typeface = SKTypeface.Default
                };

                // Monta o texto da legenda no padrão PicStone: Material BLOCK Bloco Espessura SLAB Chapa
                var espessura = request.Espessura.HasValue ? $"{request.Espessura}mm" : "";
                var legenda = $"{request.Material} BLOCK {request.Bloco} {espessura} SLAB {request.Chapa}".Trim();

                _logger.LogInformation("Legenda: {Legenda}", legenda);

                // Posição (canto superior esquerdo com margem)
                float x = 20;
                float y = 60;

                // Desenha o texto (primeiro a sombra, depois o texto)
                canvas.DrawText(legenda, x, y, shadowPaint);
                canvas.DrawText(legenda, x, y, paint);

                _logger.LogInformation("Texto desenhado no canvas");

                // Salva a imagem com a legenda
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

                // Sobrescreve o arquivo original
                using var outputStream = File.OpenWrite(caminhoImagem);
                outputStream.SetLength(0); // Limpa conteúdo anterior
                data.SaveTo(outputStream);
                outputStream.Flush();

                var tamanhoDepois = new FileInfo(caminhoImagem).Length;
                _logger.LogInformation("Tamanho do arquivo depois: {Tamanho} bytes", tamanhoDepois);
                _logger.LogInformation("=== LEGENDA ADICIONADA COM SUCESSO ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO CRÍTICO ao adicionar legenda na imagem: {CaminhoImagem}", caminhoImagem);
                _logger.LogError("Tipo: {Type}, Mensagem: {Message}", ex.GetType().FullName, ex.Message);
                _logger.LogError("Stack: {Stack}", ex.StackTrace);
                // Não lança exceção para não bloquear o upload
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Tenta transferir arquivo via FTP com retry
        /// </summary>
        private async Task TentarTransferenciaFTPAsync(string caminhoLocal, string nomeArquivo)
        {
            var ftpServer = _configuration["FTP_SERVER"];
            var ftpUser = _configuration["FTP_USER"];
            var ftpPassword = _configuration["FTP_PASSWORD"];

            // Se FTP não está configurado, apenas loga e retorna
            if (string.IsNullOrEmpty(ftpServer))
            {
                _logger.LogInformation("FTP não configurado. Arquivo salvo apenas localmente.");
                return;
            }

            const int maxTentativas = 3;
            for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
            {
                try
                {
                    _logger.LogInformation("Tentativa {Tentativa} de transferência FTP para {Arquivo}", tentativa, nomeArquivo);

                    using var client = new AsyncFtpClient(ftpServer, ftpUser, ftpPassword);
                    await client.Connect();

                    var ftpPath = $"/fotos/{nomeArquivo}";
                    await client.UploadFile(caminhoLocal, ftpPath);

                    await client.Disconnect();

                    _logger.LogInformation("Transferência FTP bem-sucedida: {Arquivo}", nomeArquivo);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha na tentativa {Tentativa} de FTP para {Arquivo}", tentativa, nomeArquivo);

                    if (tentativa < maxTentativas)
                    {
                        await Task.Delay(2000 * tentativa); // Delay exponencial
                    }
                }
            }

            _logger.LogError("Falha em todas as tentativas de FTP para {Arquivo}", nomeArquivo);
        }

        /// <summary>
        /// Testa conexão com o banco de dados
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão com banco");
                return false;
            }
        }

        /// <summary>
        /// Retorna contagem total de fotos no banco
        /// </summary>
        public async Task<int> GetPhotoCountAsync()
        {
            try
            {
                return await _context.FotosMobile.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar fotos no banco");
                return -1;
            }
        }
    }
}
