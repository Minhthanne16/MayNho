namespace MayNho.Application.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string? Timezone = "Asia/Ho_Chi_Minh");

public record LoginRequest(
    string Email,
    string Password);

public record VerifyEmailRequest(
    string Email,
    string Token);

public record ForgotPasswordRequest(
    string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

public record ChangeEmailRequest(
    string NewEmail,
    string CurrentPassword);

public record ConfirmEmailChangeRequest(
    string NewEmail,
    string Token);

public record UpdateProfileRequest(
    string DisplayName,
    string Timezone,
    string Locale,
    string Theme);

public record UpdatePreferencesRequest(
    bool EmailNotificationsEnabled,
    bool QuietHoursEnabled,
    string? QuietStart,
    string? QuietEnd,
    string? QuietTimezone);

public record UserProfileDto(
    string DisplayName,
    string Timezone,
    string Locale,
    string Theme,
    long Version);

public record UserPreferenceDto(
    bool EmailNotificationsEnabled,
    bool QuietHoursEnabled,
    string? QuietStart,
    string? QuietEnd,
    string? QuietTimezone,
    long RecipientVersion);

public record UserDto(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    UserProfileDto Profile,
    UserPreferenceDto Preferences);

public record UserSessionDto(
    Guid Id,
    bool IsCurrent,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string? IpAddress,
    string? DeviceInfo);

public record CsrfTokenResponse(
    string HeaderName,
    string Token);

public record GoogleLoginRequest(
    string Subject,
    string Email,
    bool EmailVerified,
    string? Name,
    string? Timezone = "Asia/Ho_Chi_Minh");

public record GoogleLinkRequest(
    string Subject,
    string Email);
