using MayNho.Application.Auth;
using MayNho.Domain;
using MayNho.Infrastructure.Identity;
using Xunit;

namespace MayNho.Unit;

public class IdentityAndAuthTests
{
    [Fact]
    public void TokenProviderLifespans_ComplyWithSpecifications()
    {
        // Specification: Email confirmation token 24 hours, Password reset token 30 minutes
        var emailOptions = new EmailConfirmationTokenProviderOptions();
        var passwordResetOptions = new PasswordResetTokenProviderOptions();

        Assert.Equal(TimeSpan.FromHours(24), emailOptions.TokenLifespan);
        Assert.Equal(TimeSpan.FromMinutes(30), passwordResetOptions.TokenLifespan);
    }

    [Fact]
    public void RegisterRequest_InvalidTimezone_ThrowsDomainValidation()
    {
        var now = NodaTime.SystemClock.Instance.GetCurrentInstant();
        var userId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() =>
            UserProfile.Create(userId, "Test User", "Invalid/Timezone", now));
    }

    [Fact]
    public void RegisterRequest_BlankDisplayName_ThrowsDomainValidation()
    {
        var now = NodaTime.SystemClock.Instance.GetCurrentInstant();
        var userId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() =>
            UserProfile.Create(userId, "   ", "Asia/Ho_Chi_Minh", now));
    }
}
