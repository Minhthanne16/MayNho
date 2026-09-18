using MayNho.Domain;

namespace MayNho.Application.Auth;

public static class PasswordValidator
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 128;

    public static void Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new DomainValidationException("Mật khẩu không được để trống.");
        }

        if (password.Length < MinimumLength)
        {
            throw new DomainValidationException($"Mật khẩu phải có độ dài tối thiểu {MinimumLength} ký tự.");
        }

        if (password.Length > MaximumLength)
        {
            throw new DomainValidationException($"Mật khẩu không được vượt quá {MaximumLength} ký tự.");
        }
    }
}
