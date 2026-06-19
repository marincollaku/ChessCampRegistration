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
        var fromName = configuration["Email:FromName"] ?? "Regjistrimi në Kampin e Shahut";

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogWarning("Email not configured. Skipping confirmation email for registration {Id}.", registration.Id);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(registration.ParentEmail));
        message.Subject = "Konfirmimi i regjistrimit në kampin e shahut";

        var body = $"""
            Përshëndetje {registration.ParentName},

            Faleminderit që regjistruat {registration.KidFullName} në kampin e shahut.

            Detajet e regjistrimit:
            - Fëmija: {registration.KidFullName}
            - Mosha: {registration.KidAge}
            - Shkolla: {registration.KidSchool}
            - Niveli i shahut: {registration.KidChessLevel}
            - Prindi: {registration.ParentName}
            - Telefoni: {registration.ParentPhone}
            - Email: {registration.ParentEmail}

            Ju presim me padisë në kamp!

            Ekipi i Kampit të Shahut
            """;

        message.Body = new TextPart("plain") { Text = body };

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
}
