namespace MayNho.Application.Auth;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(
        RegisterRequest request,
        string origin,
        CancellationToken ct = default);

    Task<(UserDto User, string RawSessionToken)> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task LogoutAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default);

    Task VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken ct = default);

    Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string origin,
        CancellationToken ct = default);

    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken ct = default);

    Task RequestEmailChangeAsync(
        Guid userId,
        ChangeEmailRequest request,
        string origin,
        CancellationToken ct = default);

    Task ConfirmEmailChangeAsync(
        Guid userId,
        ConfirmEmailChangeRequest request,
        CancellationToken ct = default);

    Task<UserDto> GetCurrentUserAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default);

    Task<UserPreferenceDto> UpdatePreferencesAsync(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken ct = default);

    Task<(UserDto User, string RawSessionToken)> ProcessGoogleLoginAsync(
        GoogleLoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task LinkGoogleAsync(
        Guid userId,
        GoogleLinkRequest request,
        CancellationToken ct = default);
}
