using NodaTime;

namespace MayNho.Domain;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = "Asia/Ho_Chi_Minh";
    public string Locale { get; private set; } = "vi-VN";
    public string Theme { get; private set; } = "system";
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public long Version { get; private set; } = 1;

    private UserProfile() { } // EF Core

    public static UserProfile Create(
        Guid userId,
        string displayName,
        string timezone,
        Instant now,
        string locale = "vi-VN",
        string theme = "system")
    {
        displayName = (displayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException("Tên hiển thị không được để trống.");
        }
        if (displayName.Length > 100)
        {
            throw new DomainValidationException("Tên hiển thị tối đa 100 ký tự.");
        }

        timezone = (timezone ?? string.Empty).Trim();
        if (!TimezoneValidator.IsValidIanaTimeZone(timezone))
        {
            throw new DomainValidationException($"Múi giờ '{timezone}' không hợp lệ theo chuẩn IANA.");
        }

        theme = (theme ?? string.Empty).Trim().ToLowerInvariant();
        if (theme is not ("system" or "light" or "dark"))
        {
            theme = "system";
        }

        locale = (locale ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(locale) || locale.Length > 10)
        {
            locale = "vi-VN";
        }

        return new UserProfile
        {
            UserId = userId,
            DisplayName = displayName,
            Timezone = timezone,
            Locale = locale,
            Theme = theme,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };
    }


    public void Update(
        string displayName,
        string timezone,
        string locale,
        string theme,
        Instant now)
    {
        displayName = (displayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException("Tên hiển thị không được để trống.");
        }
        if (displayName.Length > 100)
        {
            throw new DomainValidationException("Tên hiển thị tối đa 100 ký tự.");
        }

        timezone = (timezone ?? string.Empty).Trim();
        if (!TimezoneValidator.IsValidIanaTimeZone(timezone))
        {
            throw new DomainValidationException($"Múi giờ '{timezone}' không hợp lệ theo chuẩn IANA.");
        }

        theme = (theme ?? string.Empty).Trim().ToLowerInvariant();
        if (theme is not ("system" or "light" or "dark"))
        {
            theme = "system";
        }

        locale = (locale ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(locale) || locale.Length > 10)
        {
            locale = "vi-VN";
        }

        DisplayName = displayName;
        Timezone = timezone;
        Locale = locale;
        Theme = theme;
        UpdatedAt = now;
        Version++;
    }
}
