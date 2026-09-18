namespace MayNho.Application.Common;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(string toEmail, string verifyUrl, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default);
    Task SendEmailChangeConfirmationAsync(string toEmail, string confirmUrl, CancellationToken cancellationToken = default);
}
