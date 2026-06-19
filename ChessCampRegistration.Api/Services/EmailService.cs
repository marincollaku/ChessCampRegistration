using ChessCampRegistration.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ChessCampRegistration.Api.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendRegistrationConfirmationAsync(Registration registration, CancellationToken cancellationToken = default)
    {
        var smtpHost = configuration["Email:SmtpHost"];
        var smtpPort = configuration.GetValue("Email:SmtpPort", 587);
        var smtpUser = configuration["Email:SmtpUser"];
        var smtpPassword = configuration["Email:SmtpPassword"];
        var fromAddress = configuration["Email:FromAddress"] ?? smtpUser;
        var fromName = configuration["Email:FromName"] ?? "Kampi i Shahut";

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogWarning("Email not configured. Skipping confirmation email for registration {Id}.", registration.Id);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(registration.ParentEmail));
        message.Subject = $"♟ {registration.KidFullName} është regjistruar — Kampi i Shahut!";

        var plainBody = BuildPlainBody(registration);
        var htmlBody = BuildHtmlBody(registration);

        var bodyBuilder = new BodyBuilder
        {
            TextBody = plainBody,
            HtmlBody = htmlBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
        {
            await client.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Confirmation email sent to {Email} for registration {Id}.", registration.ParentEmail, registration.Id);
    }

    private static string BuildPlainBody(Registration registration) =>
        $"""
        Përshëndetje {registration.ParentName}! ♟

        Lajm i mirë — {registration.KidFullName} tani është pjesë e Kampit të Shahut!
        Hapi i parë është bërë — tani mbetet të presim me padisë ditët e kampit.

        Detajet e regjistrimit:
        ─────────────────────
        ♙ Fëmija:        {registration.KidFullName}
        ♟ Mosha:         {registration.KidAge} vjeç
        ♞ Shkolla:       {registration.KidSchool}
        ♜ Niveli:        {registration.KidChessLevel}
        ─────────────────────
        Kontakt prindi:
        👤 {registration.ParentName}
        📞 {registration.ParentPhone}
        ✉️  {registration.ParentEmail}

        Do t'ju njoftojmë për detajet e kampit sa më shpejt.
        Deri atëherë — stërvituni, argëtohuni dhe mendoni një hap përpara! 😊

        Me respekt,
        Ekipi i Kampit të Shahut ♔
        """;

    private static string BuildHtmlBody(Registration registration) =>
        $"""
        <!DOCTYPE html>
        <html lang="sq">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        </head>
        <body style="margin:0;padding:0;background:#f4f1ea;font-family:Segoe UI,Tahoma,sans-serif;color:#1f2937;">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f1ea;padding:24px 12px;">
            <tr>
              <td align="center">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 8px 24px rgba(17,24,39,0.08);">
                  <tr>
                    <td style="background:#1a1a2e;padding:28px 24px;text-align:center;">
                      <div style="font-size:36px;line-height:1;margin-bottom:8px;">♔ ♕ ♖ ♗ ♘ ♙</div>
                      <h1 style="margin:0;color:#f5f0e8;font-size:22px;font-weight:700;">Kampi i Shahut</h1>
                      <p style="margin:8px 0 0;color:#c9b99a;font-size:14px;">Regjistrimi u krye me sukses!</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:28px 24px;">
                      <p style="margin:0 0 16px;font-size:16px;line-height:1.6;">
                        Përshëndetje <strong>{registration.ParentName}</strong>! 👋
                      </p>
                      <p style="margin:0 0 20px;font-size:15px;line-height:1.7;color:#374151;">
                        Lajm i mirë — <strong>{registration.KidFullName}</strong> tani është pjesë e kampit tonë!
                        Si një lojtar i ri në fushë, ka bërë <em>hapin e parë</em> drejt një experience të mrekullueshme me shahun.
                      </p>
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#faf8f3;border:1px solid #e8dfd0;border-radius:12px;margin-bottom:20px;">
                        <tr>
                          <td style="padding:16px 18px;">
                            <p style="margin:0 0 12px;font-size:13px;font-weight:700;color:#6b5344;text-transform:uppercase;letter-spacing:0.5px;">♟ Të dhënat e fëmijës</p>
                            <p style="margin:0 0 8px;font-size:14px;"><strong>Emri:</strong> {registration.KidFullName}</p>
                            <p style="margin:0 0 8px;font-size:14px;"><strong>Mosha:</strong> {registration.KidAge} vjeç</p>
                            <p style="margin:0 0 8px;font-size:14px;"><strong>Shkolla:</strong> {registration.KidSchool}</p>
                            <p style="margin:0;font-size:14px;"><strong>Niveli i shahut:</strong> {registration.KidChessLevel}</p>
                          </td>
                        </tr>
                      </table>
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f0f4f8;border:1px solid #dbe4ee;border-radius:12px;margin-bottom:24px;">
                        <tr>
                          <td style="padding:16px 18px;">
                            <p style="margin:0 0 12px;font-size:13px;font-weight:700;color:#3b5bdb;text-transform:uppercase;letter-spacing:0.5px;">👤 Kontakt prindi</p>
                            <p style="margin:0 0 8px;font-size:14px;"><strong>Emri:</strong> {registration.ParentName}</p>
                            <p style="margin:0 0 8px;font-size:14px;"><strong>Telefoni:</strong> {registration.ParentPhone}</p>
                            <p style="margin:0;font-size:14px;"><strong>Email:</strong> {registration.ParentEmail}</p>
                          </td>
                        </tr>
                      </table>
                      <p style="margin:0 0 8px;font-size:15px;line-height:1.7;color:#374151;">
                        Do t'ju njoftojmë për datën, orarin dhe vendndodhjen e kampit sa më shpejt.
                      </p>
                      <p style="margin:0;font-size:15px;line-height:1.7;color:#374151;">
                        Deri atëherë — stërvituni, argëtohuni dhe mendoni gjithmonë <strong>një hap përpara</strong>! ♟
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="background:#1a1a2e;padding:18px 24px;text-align:center;">
                      <p style="margin:0;color:#c9b99a;font-size:13px;">Me respekt, Ekipi i Kampit të Shahut ♔</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
