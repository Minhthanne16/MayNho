using MayNho.Domain;

namespace MayNho.Application.Auth;

public interface ISessionManager
{
    Task<(UserSession Session, string RawToken)> CreateSessionAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo,
        CancellationToken ct = default);

    Task<UserSession?> ValidateAndSlideSessionAsync(
        string rawToken,
        CancellationToken ct = default);

    Task RevokeSessionAsync(
        Guid sessionId,
        Guid userId,
        string reason = "User revoked session",
        CancellationToken ct = default);

    Task RevokeAllUserSessionsAsync(
        Guid userId,
        Guid? exceptSessionId = null,
        string reason = "Revoked all sessions",
        CancellationToken ct = default);

    Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct = default);
}
