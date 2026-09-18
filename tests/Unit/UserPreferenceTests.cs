using MayNho.Domain;
using NodaTime;

namespace MayNho.Unit;

public class UserPreferenceTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 18, 10, 0);

    [Fact]
    public void IsInQuietHours_WhenDisabled_ReturnsFalse()
    {
        var pref = UserPreference.CreateDefault(Guid.NewGuid(), Now);
        // Default has QuietHoursEnabled = false
        var testTime = Instant.FromUtc(2026, 9, 18, 23, 30); // 23:30 UTC = 06:30 VN
        Assert.False(pref.IsInQuietHours(testTime));
    }

    [Fact]
    public void IsInQuietHours_Overnight_CalculatesCorrectly()
    {
        var pref = UserPreference.CreateDefault(Guid.NewGuid(), Now);
        // Enable quiet hours: 22:00 to 07:00 (Asia/Ho_Chi_Minh is UTC+7)
        pref.Update(true, true, new LocalTime(22, 0), new LocalTime(7, 0), "Asia/Ho_Chi_Minh", Now);

        // Test 21:59 VN (14:59 UTC) -> False
        var beforeQuiet = Instant.FromUtc(2026, 9, 18, 14, 59);
        Assert.False(pref.IsInQuietHours(beforeQuiet));

        // Test 22:00 VN (15:00 UTC) -> True
        var startQuiet = Instant.FromUtc(2026, 9, 18, 15, 0);
        Assert.True(pref.IsInQuietHours(startQuiet));

        // Test 03:00 VN (20:00 UTC previous day) -> True
        var midQuiet = Instant.FromUtc(2026, 9, 18, 20, 0);
        Assert.True(pref.IsInQuietHours(midQuiet));

        // Test 06:59 VN (23:59 UTC previous day) -> True
        var endQuietMinusOne = Instant.FromUtc(2026, 9, 18, 23, 59);
        Assert.True(pref.IsInQuietHours(endQuietMinusOne));

        // Test 07:00 VN (00:00 UTC) -> False
        var endQuiet = Instant.FromUtc(2026, 9, 19, 0, 0);
        Assert.False(pref.IsInQuietHours(endQuiet));
    }

    [Fact]
    public void IsInQuietHours_SameDay_CalculatesCorrectly()
    {
        var pref = UserPreference.CreateDefault(Guid.NewGuid(), Now);
        // Enable quiet hours: 13:00 to 15:00 (Asia/Ho_Chi_Minh is UTC+7)
        pref.Update(true, true, new LocalTime(13, 0), new LocalTime(15, 0), "Asia/Ho_Chi_Minh", Now);

        // 12:30 VN (05:30 UTC) -> False
        Assert.False(pref.IsInQuietHours(Instant.FromUtc(2026, 9, 18, 5, 30)));

        // 13:30 VN (06:30 UTC) -> True
        Assert.True(pref.IsInQuietHours(Instant.FromUtc(2026, 9, 18, 6, 30)));

        // 15:00 VN (08:00 UTC) -> False
        Assert.False(pref.IsInQuietHours(Instant.FromUtc(2026, 9, 18, 8, 0)));
    }

    [Fact]
    public void IncrementRecipientVersion_IncrementsMonotonically()
    {
        var pref = UserPreference.CreateDefault(Guid.NewGuid(), Now);
        Assert.Equal(1, pref.RecipientVersion);

        pref.IncrementRecipientVersion(Now + Duration.FromMinutes(10));
        Assert.Equal(2, pref.RecipientVersion);

        pref.IncrementRecipientVersion(Now + Duration.FromMinutes(20));
        Assert.Equal(3, pref.RecipientVersion);
    }
}
