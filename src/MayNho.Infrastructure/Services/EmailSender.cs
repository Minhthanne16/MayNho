using MailKit.Net.Smtp;
using MayNho.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace MayNho.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verifyUrl, CancellationToken cancellationToken = default)
    {
        var subject = "[Mây Nhỏ] Xác nhận địa chỉ email của bạn";
        var body = $"""
            <div style="font-family: sans-serif; line-height: 1.6; color: #334155; max-width: 600px; margin: 0 auto; padding: 24px;">
              <h2 style="color: #0284c7;">Chào mừng bạn đến với Mây Nhỏ!</h2>
              <p>Vui lòng nhấn vào liên kết bên dưới để xác minh địa chỉ email của bạn:</p>
              <p style="margin: 24px 0;">
                <a href="{verifyUrl}" style="background-color: #0284c7; color: #ffffff; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: 500;">
                  Xác nhận email
                </a>
              </p>
              <p>Liên kết này có hiệu lực trong vòng 24 giờ.</p>
              <p style="font-size: 13px; color: #64748b;">Nếu bạn không yêu cầu đăng ký tài khoản tại Mây Nhỏ, vui lòng bỏ qua email này.</p>
            </div>
            """;

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default)
    {
        var subject = "[Mây Nhỏ] Yêu cầu đặt lại mật khẩu";
        var body = $"""
            <div style="font-family: sans-serif; line-height: 1.6; color: #334155; max-width: 600px; margin: 0 auto; padding: 24px;">
              <h2 style="color: #0284c7;">Đặt lại mật khẩu</h2>
              <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản Mây Nhỏ của bạn.</p>
              <p style="margin: 24px 0;">
                <a href="{resetUrl}" style="background-color: #0284c7; color: #ffffff; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: 500;">
                  Đặt lại mật khẩu
                </a>
              </p>
              <p>Liên kết này có hiệu lực trong vòng 30 phút.</p>
              <p style="font-size: 13px; color: #64748b;">Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email và mật khẩu của bạn sẽ không thay đổi.</p>
            </div>
            """;

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendEmailChangeConfirmationAsync(string toEmail, string confirmUrl, CancellationToken cancellationToken = default)
    {
        var subject = "[Mây Nhỏ] Xác nhận thay đổi địa chỉ email";
        var body = $"""
            <div style="font-family: sans-serif; line-height: 1.6; color: #334155; max-width: 600px; margin: 0 auto; padding: 24px;">
              <h2 style="color: #0284c7;">Xác nhận địa chỉ email mới</h2>
              <p>Vui lòng xác nhận địa chỉ email mới này cho tài khoản Mây Nhỏ của bạn:</p>
              <p style="margin: 24px 0;">
                <a href="{confirmUrl}" style="background-color: #0284c7; color: #ffffff; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: 500;">
                  Xác nhận đổi email
                </a>
              </p>
              <p>Liên kết này có hiệu lực trong 24 giờ.</p>
              <p style="font-size: 13px; color: #64748b;">Nếu bạn không thực hiện thay đổi này, tài khoản của bạn vẫn an toàn và gắn với email cũ.</p>
            </div>
            """;

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var smtpHost = _configuration["Smtp:Host"] ?? "localhost";
        var smtpPort = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 1025;
        var senderEmail = _configuration["Smtp:SenderEmail"] ?? "noreply@maynho.local";
        var senderName = _configuration["Smtp:SenderName"] ?? "Mây Nhỏ";

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, false, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Sent email to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP delivery failed. Email payload and tokens are not logged.");
            throw;
        }
    }
}
