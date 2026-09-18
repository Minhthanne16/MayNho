using NodaTime;

namespace MayNho.Domain;

public static class TimezoneValidator
{
    public static bool IsValidIanaTimeZone(string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return false;
        }

        return DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezoneId) is not null;
    }
}
