using NodaTime;

namespace MayNho.Domain;

public sealed class UserSession
{
    public static readonly Duration DefaultIdleTimeout = Duration.FromDays(7);
    public static readonly Duration DefaultAbsoluteTimeout = Duration.FromDays(30);

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string SessionTokenHash { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceInfo { get; private set; }
    public Instant CreatedAt { get; private set; }
    public Instant ExpiresAt { get; private set; }
    public Instant AbsoluteExpiresAt { get; private set; }
    public Instant? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private UserSession() { } // EF Core

    public static UserSession Create(
        Guid userId,
        string sessionTokenHash,
        Instant now,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceInfo = null,
        Duration? idleTimeout = null,
        Duration? absoluteTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(sessionTokenHash))
        {
            throw new DomainValidationException("Hash của token phiên không được để trống.");
        }

        var idle = idleTimeout ?? DefaultIdleTimeout;
        var absolute = absoluteTimeout ?? DefaultAbsoluteTimeout;

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionTokenHash = sessionTokenHash,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo,
            CreatedAt = now,
            ExpiresAt = now + idle,
            AbsoluteExpiresAt = now + absolute
        };
    }

    public bool IsActive(Instant now)
    {
        return RevokedAt is null && ExpiresAt > now && AbsoluteExpiresAt > now;
    }

    public void SlideExpiration(Instant now, Duration? idleTimeout = null)
    {
        if (!IsActive(now))
        {
            return;
        }

        var idle = idleTimeout ?? DefaultIdleTimeout;
        var proposed = now + idle;
        ExpiresAt = proposed < AbsoluteExpiresAt ? proposed : AbsoluteExpiresAt;
    }

    public void Revoke(Instant now, string? reason = "User logged out")
    {
        if (RevokedAt is null)
        {
            RevokedAt = now;
            RevokedReason = reason;
        }
    }
}
