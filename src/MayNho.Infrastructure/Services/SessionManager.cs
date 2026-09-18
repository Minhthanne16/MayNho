using System.Security.Cryptography;
using System.Text;
using MayNho.Application.Auth;
using MayNho.Application.Common;
using MayNho.Domain;
using MayNho.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MayNho.Infrastructure.Services;

public sealed class SessionManager : ISessionManager
{
    private readonly MayNhoDbContext _db;
    private readonly IClock _clock;

    public SessionManager(MayNhoDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public static string GenerateRawToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public async Task<(UserSession Session, string RawToken)> CreateSessionAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo,
        CancellationToken ct = default)
    {
        var now = _clock.GetCurrentInstant();
        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);

        var session = UserSession.Create(
            userId,
            tokenHash,
            now,
            ipAddress,
            userAgent,
            deviceInfo);

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return (session, rawToken);
    }

    public async Task<UserSession?> ValidateAndSlideSessionAsync(
        string rawToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = HashToken(rawToken);
        var now = _clock.GetCurrentInstant();

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionTokenHash == tokenHash, ct);

        if (session is null || !session.IsActive(now))
        {
            return null;
        }

        session.SlideExpiration(now);
        await _db.SaveChangesAsync(ct);

        return session;
    }

    public async Task RevokeSessionAsync(
        Guid sessionId,
        Guid userId,
        string reason = "User revoked session",
        CancellationToken ct = default)
    {
        var now = _clock.GetCurrentInstant();
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        if (session is not null)
        {
            session.Revoke(now, reason);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RevokeAllUserSessionsAsync(
        Guid userId,
        Guid? exceptSessionId = null,
        string reason = "Revoked all sessions",
        CancellationToken ct = default)
    {
        var now = _clock.GetCurrentInstant();
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var s in sessions)
        {
            if (exceptSessionId.HasValue && s.Id == exceptSessionId.Value)
            {
                continue;
            }
            s.Revoke(now, reason);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct = default)
    {
        var now = _clock.GetCurrentInstant();
        var sessions = await _db.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return sessions.Select(s => new UserSessionDto(
            s.Id,
            s.Id == currentSessionId,
            s.CreatedAt.ToDateTimeOffset(),
            s.ExpiresAt.ToDateTimeOffset(),
            s.IpAddress,
            s.DeviceInfo
        )).ToList();
    }
}
