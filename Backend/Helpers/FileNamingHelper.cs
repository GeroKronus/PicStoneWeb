using System;
using System.IO;

namespace PicStoneFotoAPI.Helpers
{
    /// <summary>
    /// Helper centralizado para nomenclatura de arquivos - Princípio DRY
    /// Padrão: {tipo}_{variacao}_{parametros}_User{id}.jpg
    /// </summary>
    public static class FileNamingHelper
    {
        /// <summary>
        /// Gera nome de arquivo padronizado para qualquer tipo de mockup
        /// </summary>
        public static string GenerateMockupFileName(
            string mockupType,      // Ex: "bancada1", "cavalete_simples", "bathroom1"
            string variation,       // Ex: "normal", "rotacionado", "quadrant1"
            int userId,
            string additionalInfo = null,  // Ex: "claro", "escuro"
            string extension = "jpg")
        {
            // Monta o nome base
            var fileName = $"{mockupType.ToLower()}_{variation.ToLower()}";

            // Adiciona informação adicional se fornecida
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                fileName += $"_{additionalInfo.ToLower()}";
            }

            // Adiciona ID do usuário e extensão
            fileName += $"_User{userId}.{extension}";

            return fileName;
        }

        /// <summary>
        /// Gera nome para Bancadas
        /// </summary>
        public static string GenerateBancadaFileName(int bancadaNumber, string variation, int userId)
        {
            return GenerateMockupFileName($"bancada{bancadaNumber}", variation, userId);
        }

        /// <summary>
        /// Gera nome para Nicho
        /// </summary>
        public static string GenerateNichoFileName(int nichoNumber, string variation, int userId)
        {
            return GenerateMockupFileName($"nicho{nichoNumber}", variation, userId);
        }

        /// <summary>
        /// Gera nome para Bathroom (sem opção de fundo)
        /// </summary>
        public static string GenerateBathroomFileName(int bathroomNumber, int quadrant, string background, int userId)
        {
            // Bathroom não tem opção de fundo - ignora o parâmetro background
            return GenerateMockupFileName($"bathroom{bathroomNumber}", $"quadrant{quadrant}", userId);
        }

        /// <summary>
        /// Gera nome para Living Room (sem opção de fundo)
        /// </summary>
        public static string GenerateLivingRoomFileName(int livingRoomNumber, int quadrant, string background, int userId)
        {
            // Living Room não tem opção de fundo - ignora o parâmetro background
            return GenerateMockupFileName($"livingroom{livingRoomNumber}", $"quadrant{quadrant}", userId);
        }

        /// <summary>
        /// Gera nome para Stairs (sem opção de fundo)
        /// </summary>
        public static string GenerateStairsFileName(int stairsNumber, string variation, string background, int userId)
        {
            // Stairs não tem opção de fundo - ignora o parâmetro background
            return GenerateMockupFileName($"stairs{stairsNumber}", variation, userId);
        }

        /// <summary>
        /// Gera nome para Cavalete Simples
        /// </summary>
        public static string GenerateCavaleteSimpleFileName(string background, int userId)
        {
            return GenerateMockupFileName("cavalete", "simples", userId, background);
        }

        /// <summary>
        /// Gera nome para Cavalete Duplo
        /// </summary>
        public static string GenerateCavaleteDuploFileName(string variation, string background, int userId)
        {
            return GenerateMockupFileName("cavalete", $"duplo_{variation}", userId, background);
        }

        /// <summary>
        /// Gera nome para BookMatch
        /// </summary>
        public static string GenerateBookMatchFileName(string type, int userId, string originalFileName = "image")
        {
            // Remove extensão do nome original se houver
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            return GenerateMockupFileName("bookmatch", type.ToLower().Replace(" ", "_"), userId, baseName);
        }

        /// <summary>
        /// Extrai apenas o nome do arquivo de um caminho completo
        /// Remove path e query parameters
        /// </summary>
        public static string ExtractCleanFileName(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return $"mockup_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";

            // Remove query parameters
            var cleanPath = fullPath.Split('?')[0].Split('&')[0];

            // Extrai apenas o nome do arquivo
            return Path.GetFileName(cleanPath);
        }

        /// <summary>
        /// Gera caminho relativo para upload
        /// </summary>
        public static string GenerateUploadPath(string fileName)
        {
            return $"/uploads/mockups/{fileName}";
        }

        /// <summary>
        /// Adiciona cache-busting timestamp a uma URL
        /// </summary>
        public static string AddCacheBusting(string path)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var separator = path.Contains("?") ? "&" : "?";
            return $"{path}{separator}v={timestamp}";
        }
    }
}