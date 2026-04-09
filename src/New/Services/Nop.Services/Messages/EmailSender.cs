using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Nop.Core.Domain.Messages;
using Nop.Services.Media;

namespace Nop.Services.Messages;

public class EmailSender(IDownloadService downloadService) : IEmailSender
{
    public async Task SendEmailAsync(EmailAccount emailAccount, string subject, string body,
        string fromAddress, string fromName, string toAddress, string toName,
        string? replyToAddress = null, string? replyToName = null,
        IEnumerable<string>? bcc = null, IEnumerable<string>? cc = null,
        string? attachmentFilePath = null, string? attachmentFileName = null,
        int attachedDownloadId = 0, IDictionary<string, string>? headers = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));

        if (!string.IsNullOrEmpty(replyToAddress))
            message.ReplyTo.Add(new MailboxAddress(replyToName ?? string.Empty, replyToAddress));

        if (bcc != null)
            foreach (var addr in bcc.Where(a => !string.IsNullOrWhiteSpace(a)))
                message.Bcc.Add(MailboxAddress.Parse(addr.Trim()));

        if (cc != null)
            foreach (var addr in cc.Where(a => !string.IsNullOrWhiteSpace(a)))
                message.Cc.Add(MailboxAddress.Parse(addr.Trim()));

        if (headers != null)
            foreach (var header in headers)
                message.Headers.Add(header.Key, header.Value);

        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };

        // file attachment
        if (!string.IsNullOrEmpty(attachmentFilePath) && File.Exists(attachmentFilePath))
        {
            var attachment = await builder.Attachments.AddAsync(attachmentFilePath);
            if (!string.IsNullOrEmpty(attachmentFileName))
                attachment.ContentDisposition!.FileName = attachmentFileName;
        }

        // download attachment
        if (attachedDownloadId > 0)
        {
            var download = await downloadService.GetDownloadByIdAsync(attachedDownloadId);
            if (download?.DownloadBinary != null && !download.UseDownloadUrl)
            {
                var fileName = !string.IsNullOrWhiteSpace(download.Filename)
                    ? download.Filename + download.Extension
                    : download.Id + (download.Extension ?? string.Empty);

                builder.Attachments.Add(fileName, download.DownloadBinary);
            }
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var secureOption = emailAccount.EnableSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(emailAccount.Host, emailAccount.Port, secureOption);

        if (!emailAccount.UseDefaultCredentials && !string.IsNullOrEmpty(emailAccount.Username))
            await client.AuthenticateAsync(emailAccount.Username, emailAccount.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
