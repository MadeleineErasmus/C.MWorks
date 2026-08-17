using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace JobCardApp.Api.Services;

/// <summary>
/// Thin wrapper around MailKit for sending the real quote/invoice emails
/// (§ decision: "Real email sent from the API server... Requires SMTP").
/// Config is read from IConfiguration under the "Smtp" section — in dev this
/// should come from .NET User Secrets (see appsettings.json Smtp._comment),
/// never hardcoded and never committed with real values.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Sends the same PDF to every recipient in <paramref name="toEmails"/> —
    /// e.g. a customer's primary email plus any additional addresses
    /// (accounts department, a manager) on file for them. No per-recipient
    /// display name is used; the customer's name belongs in the body/greeting
    /// once, not on each address.
    /// </summary>
    public async Task SendWithAttachmentAsync(
        IEnumerable<string> toEmails,
        string subject,
        string bodyText,
        string attachmentFileName,
        byte[] attachmentBytes)
    {
        var recipients = toEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
            throw new InvalidOperationException("No recipient email addresses were supplied.");

        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "Smtp:Host is not configured. Set SMTP credentials via `dotnet user-secrets set \"Smtp:Host\" \"<your-smtp-host>\"` " +
                "(and Smtp:Port/Username/Password/EnableSsl/FromAddress/FromName) from the JobCardApp.Api project directory — " +
                "see the Smtp._comment note in appsettings.json.");
        }

        var port = _config.GetValue("Smtp:Port", 587);
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        var enableSsl = _config.GetValue("Smtp:EnableSsl", true);
        var fromAddress = _config["Smtp:FromAddress"];
        var fromName = _config["Smtp:FromName"] ?? "JobCard Pro";

        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Smtp:FromAddress is not configured — set it alongside Smtp:Host.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        foreach (var recipient in recipients)
            message.To.Add(new MailboxAddress(recipient, recipient));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = bodyText };
        builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse("application/pdf"));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(host, port, socketOptions);

        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password ?? string.Empty);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
