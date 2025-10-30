using PicStoneFotoAPI.Data;
using PicStoneFotoAPI.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;
using FluentFTP;

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
                var nomeArquivo = GerarNomeArquivo(request.Lote, request.Chapa);
                var caminhoCompleto = Path.Combine(_uploadPath, nomeArquivo);

                // Salva arquivo no disco
                await SalvarArquivoAsync(request.Arquivo, caminhoCompleto);

                // Registra no banco de dados
                var username = user.Identity?.Name ?? "Desconhecido";
                var fotoMobile = new FotoMobile
                {
                    NomeArquivo = nomeArquivo,
                    Lote = SanitizarString(request.Lote),
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
                return new FotoUploadResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro ao processar foto: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Retorna histórico das últimas 50 fotos
        /// </summary>
        public async Task<List<FotoMobile>> ObterHistoricoAsync(int limite = 50)
        {
            return await Task.Run(() =>
                _context.FotosMobile
                    .OrderByDescending(f => f.DataUpload)
                    .Take(limite)
                    .ToList()
            );
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
        /// Gera nome do arquivo no formato LOTE_CHAPA_YYYYMMDD_HHMMSS.jpg
        /// </summary>
        private string GerarNomeArquivo(string lote, string chapa)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var loteSanitizado = SanitizarNomeArquivo(lote);
            var chapaSanitizada = SanitizarNomeArquivo(chapa);
            return $"{loteSanitizado}_{chapaSanitizada}_{timestamp}.jpg";
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
    }
}
