using MayNho.Domain;
using NodaTime;

namespace MayNho.Unit;

public class UserSessionTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 9, 18, 10, 0);

    [Fact]
    public void Create_InitializesProperExpirations()
    {
        var userId = Guid.NewGuid();
        var session = UserSession.Create(userId, "hash123", Now);

        Assert.Equal(userId, session.UserId);
        Assert.Equal("hash123", session.SessionTokenHash);
        Assert.Equal(Now, session.CreatedAt);
        Assert.Equal(Now + Duration.FromDays(7), session.ExpiresAt);
        Assert.Equal(Now + Duration.FromDays(30), session.AbsoluteExpiresAt);
        Assert.True(session.IsActive(Now));
    }

    [Fact]
    public void Revoke_MakesSessionInactive()
    {
        var session = UserSession.Create(Guid.NewGuid(), "hash123", Now);
        Assert.True(session.IsActive(Now));

        var revokeTime = Now + Duration.FromHours(2);
        session.Revoke(revokeTime, "User logged out");

        Assert.False(session.IsActive(revokeTime));
        Assert.Equal(revokeTime, session.RevokedAt);
        Assert.Equal("User logged out", session.RevokedReason);
    }

    [Fact]
    public void IsActive_WhenIdleExpired_ReturnsFalse()
    {
        var session = UserSession.Create(Guid.NewGuid(), "hash123", Now);
        var eightDaysLater = Now + Duration.FromDays(8);

        Assert.False(session.IsActive(eightDaysLater));
    }

    [Fact]
    public void SlideExpiration_DoesNotExceedAbsoluteExpiration()
    {
        // 5 days idle timeout, 7 days absolute timeout
        var session = UserSession.Create(
            Guid.NewGuid(),
            "hash123",
            Now,
            idleTimeout: Duration.FromDays(5),
            absoluteTimeout: Duration.FromDays(7));

        // Slide at day 4: proposed idle is day 4 + 5 = day 9, but absolute is day 7
        var fourDaysLater = Now + Duration.FromDays(4);
        session.SlideExpiration(fourDaysLater);

        Assert.Equal(session.AbsoluteExpiresAt, session.ExpiresAt);
    }

}
