using MayNho.Domain;
using NodaTime;

namespace MayNho.Unit;

public class UserProfileTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 18, 10, 0);

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Nguyen Van A", "Asia/Ho_Chi_Minh", Now);

        Assert.Equal(userId, profile.UserId);
        Assert.Equal("Nguyen Van A", profile.DisplayName);
        Assert.Equal("Asia/Ho_Chi_Minh", profile.Timezone);
        Assert.Equal("vi-VN", profile.Locale);
        Assert.Equal("system", profile.Theme);
        Assert.Equal(1, profile.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyDisplayName_ThrowsException(string? displayName)
    {
        var userId = Guid.NewGuid();
        Assert.Throws<DomainValidationException>(() =>
            UserProfile.Create(userId, displayName!, "Asia/Ho_Chi_Minh", Now));
    }

    [Fact]
    public void Create_WithInvalidTimezone_ThrowsException()
    {
        var userId = Guid.NewGuid();
        Assert.Throws<DomainValidationException>(() =>
            UserProfile.Create(userId, "Nguyen Van A", "Invalid/Timezone", Now));
    }

    [Fact]
    public void Update_IncrementsVersion()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Nguyen Van A", "Asia/Ho_Chi_Minh", Now);
        Assert.Equal(1, profile.Version);

        var later = Now + Duration.FromHours(1);
        profile.Update("Nguyen Van B", "Asia/Tokyo", "ja-JP", "dark", later);

        Assert.Equal(2, profile.Version);
        Assert.Equal("Nguyen Van B", profile.DisplayName);
        Assert.Equal("Asia/Tokyo", profile.Timezone);
        Assert.Equal("ja-JP", profile.Locale);
        Assert.Equal("dark", profile.Theme);
        Assert.Equal(later, profile.UpdatedAt);
    }
}
