using NodaTime;

namespace MayNho.Domain;

public sealed class UserPreference
{
    public Guid UserId { get; private set; }
    public bool EmailNotificationsEnabled { get; private set; } = true;
    public bool QuietHoursEnabled { get; private set; } = false;
    public LocalTime? QuietStart { get; private set; }
    public LocalTime? QuietEnd { get; private set; }
    public string? QuietTimezone { get; private set; }
    public long RecipientVersion { get; private set; } = 1;
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }

    private UserPreference() { } // EF Core

    public static UserPreference CreateDefault(Guid userId, Instant now, string? defaultTimezone = "Asia/Ho_Chi_Minh")
    {
        return new UserPreference
        {
            UserId = userId,
            EmailNotificationsEnabled = true,
            QuietHoursEnabled = false,
            QuietStart = new LocalTime(22, 0),
            QuietEnd = new LocalTime(7, 0),
            QuietTimezone = defaultTimezone ?? "Asia/Ho_Chi_Minh",
            RecipientVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        bool emailNotificationsEnabled,
        bool quietHoursEnabled,
        LocalTime? quietStart,
        LocalTime? quietEnd,
        string? quietTimezone,
        Instant now)
    {
        if (quietHoursEnabled)
        {
            if (!quietStart.HasValue || !quietEnd.HasValue)
            {
                throw new DomainValidationException("Giờ bắt đầu và kết thúc khoảng thời gian yên lặng là bắt buộc khi bật chế độ yên lặng.");
            }

            if (!string.IsNullOrWhiteSpace(quietTimezone) && !TimezoneValidator.IsValidIanaTimeZone(quietTimezone))
            {
                throw new DomainValidationException($"Múi giờ yên lặng '{quietTimezone}' không hợp lệ.");
            }
        }

        EmailNotificationsEnabled = emailNotificationsEnabled;
        QuietHoursEnabled = quietHoursEnabled;
        QuietStart = quietStart;
        QuietEnd = quietEnd;
        QuietTimezone = quietTimezone;
        UpdatedAt = now;
    }

    public void IncrementRecipientVersion(Instant now)
    {
        RecipientVersion++;
        UpdatedAt = now;
    }

    public bool IsInQuietHours(Instant instant, string fallbackTimezone = "Asia/Ho_Chi_Minh")
    {
        if (!QuietHoursEnabled || !QuietStart.HasValue || !QuietEnd.HasValue)
        {
            return false;
        }

        var tzId = !string.IsNullOrWhiteSpace(QuietTimezone) ? QuietTimezone : fallbackTimezone;
        var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(tzId) ?? DateTimeZoneProviders.Tzdb["Asia/Ho_Chi_Minh"];
        var zonedDateTime = instant.InZone(tz);
        var currentLocalTime = zonedDateTime.TimeOfDay;

        var start = QuietStart.Value;
        var end = QuietEnd.Value;

        if (start < end)
        {
            // Same day range (e.g. 13:00 to 15:00)
            return currentLocalTime >= start && currentLocalTime < end;
        }
        else if (start > end)
        {
            // Overnight range (e.g. 22:00 to 07:00)
            return currentLocalTime >= start || currentLocalTime < end;
        }
        else
        {
            // start == end: 24h quiet period
            return true;
        }
    }
}
