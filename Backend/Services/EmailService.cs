using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço de envio de emails usando SMTP (MailKit)
    /// Baseado no sistema do Site1 (Quero Bitcoin)
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de verificação com token de confirmação
        /// </summary>
        public async Task<bool> SendVerificationEmailAsync(string email, string nome, string token, string? baseUrl = null)
        {
            try
            {
                var appUrl = baseUrl ?? _configuration["NEXTAUTH_URL"] ?? "http://localhost:5000";
                var verificationUrl = $"{appUrl}/api/auth/verify?token={token}";

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0 0 10px 0; font-size: 32px;'>PicStone Mobile</h1>
            <h2 style='margin: 10px 0 5px 0;'>Bem-vindo ao PicStone Mobile!</h2>
            <p style='margin: 5px 0; font-size: 16px;'>Confirme seu email para começar</p>
        </div>
        <div class='content'>
            <h2>Olá, {nome}!</h2>
            <p>Obrigado por se cadastrar na plataforma <strong>PicStone Mobile</strong>.</p>
            <p>Para confirmar seu email e continuar com o processo de aprovação, clique no botão abaixo:</p>

            <div style='text-align: center;'>
                <a href='{verificationUrl}' class='button'>Confirmar Email</a>
            </div>

            <p>Ou copie e cole o link abaixo no seu navegador:</p>
            <p style='background: #eee; padding: 10px; word-break: break-all; border-radius: 5px;'>{verificationUrl}</p>

            <p><strong>Próximos passos:</strong></p>
            <ol>
                <li>Confirme seu email clicando no botão acima</li>
                <li>Aguarde a aprovação do administrador</li>
                <li>Você receberá um email quando seu acesso for aprovado</li>
            </ol>

            <p style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 14px;'>
                Se você não solicitou este cadastro, por favor ignore este email.
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 PicStone Mobile. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(
                    to: email,
                    subject: "Confirme seu email - PicStone Mobile",
                    htmlBody: htmlBody
                );

                _logger.LogInformation($"Email de verificação enviado para: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de verificação para: {email}");
                return false;
            }
        }

        /// <summary>
        /// Envia email de aprovação do admin
        /// </summary>
        public async Task<bool> SendApprovalEmailAsync(string email, string nome, DateTime? dataExpiracao = null)
        {
            try
            {
                var appUrl = _configuration["NEXTAUTH_URL"] ?? "http://localhost:5000";
                var loginUrl = $"{appUrl}";

                var expiracaoHtml = dataExpiracao.HasValue
                    ? $"<p><strong>Seu acesso expira em:</strong> {dataExpiracao.Value:dd/MM/yyyy HH:mm}</p>"
                    : "<p><strong>Seu acesso é por tempo indeterminado.</strong></p>";

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background: #10b981; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0 0 10px 0; font-size: 32px;'>PicStone Mobile</h1>
            <h2 style='margin: 10px 0 5px 0;'>Acesso Aprovado!</h2>
        </div>
        <div class='content'>
            <h2>Parabéns, {nome}!</h2>
            <p>Seu acesso à plataforma <strong>PicStone Mobile</strong> foi <strong>aprovado</strong> pelo administrador.</p>

            {expiracaoHtml}

            <p>Agora você já pode fazer login e começar a usar todos os recursos disponíveis.</p>

            <div style='text-align: center;'>
                <a href='{loginUrl}' class='button'>Acessar Plataforma</a>
            </div>

            <p><strong>Suas credenciais:</strong></p>
            <ul>
                <li><strong>Email:</strong> {email}</li>
                <li><strong>Senha:</strong> A mesma que você cadastrou</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 PicStone Mobile. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(
                    to: email,
                    subject: "Seu acesso foi aprovado - PicStone Mobile",
                    htmlBody: htmlBody
                );

                _logger.LogInformation($"Email de aprovação enviado para: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de aprovação para: {email}");
                return false;
            }
        }

        /// <summary>
        /// Envia email de rejeição
        /// </summary>
        public async Task<bool> SendRejectionEmailAsync(string email, string nome, string? motivo = null)
        {
            try
            {
                var motivoHtml = !string.IsNullOrEmpty(motivo)
                    ? $"<p><strong>Motivo:</strong> {motivo}</p>"
                    : "";

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0 0 10px 0; font-size: 32px;'>PicStone Mobile</h1>
            <h2 style='margin: 10px 0 5px 0;'>Solicitação não aprovada</h2>
        </div>
        <div class='content'>
            <h2>Olá, {nome}</h2>
            <p>Infelizmente, sua solicitação de acesso à plataforma <strong>PicStone Mobile</strong> não foi aprovada pelo administrador.</p>

            {motivoHtml}

            <p>Se você acha que isso foi um erro ou deseja mais informações, entre em contato com o suporte.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 PicStone Mobile. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(
                    to: email,
                    subject: "Solicitação de acesso - PicStone Mobile",
                    htmlBody: htmlBody
                );

                _logger.LogInformation($"Email de rejeição enviado para: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de rejeição para: {email}");
                return false;
            }
        }

        /// <summary>
        /// Envia email notificando admin sobre nova solicitação
        /// </summary>
        public async Task<bool> SendAdminNotificationAsync(string nomeUsuario, string emailUsuario)
        {
            try
            {
                var adminEmail = _configuration["ADMIN_EMAIL"] ?? "admin@picstone.com.br";
                var appUrl = _configuration["PUBLIC_URL"] ?? _configuration["NEXTAUTH_URL"] ?? "http://localhost:5000";
                var adminPanelUrl = $"{appUrl}";

                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background: #f59e0b; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0 0 10px 0; font-size: 32px;'>PicStone Mobile</h1>
            <h2 style='margin: 10px 0 5px 0;'>Nova Solicitação de Acesso</h2>
        </div>
        <div class='content'>
            <h2>Olá, Administrador</h2>
            <p>Um novo usuário confirmou o email e está aguardando aprovação:</p>

            <p><strong>Dados do usuário:</strong></p>
            <ul>
                <li><strong>Nome:</strong> {nomeUsuario}</li>
                <li><strong>Email:</strong> {emailUsuario}</li>
            </ul>

            <p>Acesse o painel administrativo para aprovar ou rejeitar esta solicitação:</p>

            <div style='text-align: center;'>
                <a href='{adminPanelUrl}' class='button'>Acessar Painel Admin</a>
            </div>
        </div>
        <div class='footer'>
            <p>&copy; 2025 PicStone Mobile - Notificação Automática</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(
                    to: adminEmail,
                    subject: "⚠️ Nova solicitação de acesso - PicStone Mobile",
                    htmlBody: htmlBody
                );

                _logger.LogInformation($"Notificação enviada para admin sobre novo usuário: {emailUsuario}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar notificação para admin");
                return false;
            }
        }

        /// <summary>
        /// Método genérico para enviar email via SMTP (MailKit)
        /// </summary>
        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var smtpHost = _configuration["SMTP_HOST"] ?? "smtp.gmail.com";
            var smtpPortStr = _configuration["SMTP_PORT"] ?? "587";
            var smtpUser = _configuration["SMTP_USER"] ?? "";
            var smtpPassword = _configuration["SMTP_PASSWORD"] ?? "";
            var emailFrom = _configuration["EMAIL_FROM"] ?? smtpUser;

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Skipping email send.");
                return;
            }

            var smtpPort = int.Parse(smtpPortStr);

            _logger.LogInformation($"=== ENVIANDO EMAIL ===");
            _logger.LogInformation($"Host: {smtpHost}");
            _logger.LogInformation($"Port: {smtpPort}");
            _logger.LogInformation($"User: {smtpUser}");
            _logger.LogInformation($"From: {emailFrom}");
            _logger.LogInformation($"To: {to}");
            _logger.LogInformation($"Subject: {subject}");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PicStone Mobile", emailFrom));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Conecta ao servidor SMTP
                if (smtpPort == 465)
                {
                    // SSL (porta 465)
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    // TLS (porta 587)
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                }

                // Autentica
                await client.AuthenticateAsync(smtpUser, smtpPassword);

                // Envia email
                await client.SendAsync(message);

                _logger.LogInformation($"✅ Email enviado com sucesso para: {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erro ao enviar email para: {to}");
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

    }
}
