using System.Web;
using MayNho.Application.Auth;
using MayNho.Application.Common;
using MayNho.Domain;
using MayNho.Infrastructure.Data;
using MayNho.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using AppClock = MayNho.Application.Common.IClock;

namespace MayNho.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly MayNhoDbContext _db;
    private readonly ISessionManager _sessionManager;
    private readonly IEmailSender _emailSender;
    private readonly AppClock _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        MayNhoDbContext db,
        ISessionManager sessionManager,
        IEmailSender emailSender,
        AppClock clock,
        ILogger<AuthService> logger)

    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _sessionManager = sessionManager;
        _emailSender = emailSender;
        _clock = clock;
        _logger = logger;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, string origin, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainValidationException("Địa chỉ email không hợp lệ.");
        }

        PasswordValidator.Validate(request.Password);

        var timezone = !string.IsNullOrWhiteSpace(request.Timezone) ? request.Timezone : "Asia/Ho_Chi_Minh";
        var now = _clock.GetCurrentInstant();
        var userId = Guid.NewGuid();

        // Validate domain rules before creating database entities
        var profile = UserProfile.Create(userId, request.DisplayName, timezone, now);
        var preference = UserPreference.CreateDefault(userId, now, timezone);

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new DomainValidationException("Email này đã được sử dụng trên hệ thống.");
        }

        var user = new AppUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = false
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new DomainValidationException($"Đăng ký thất bại: {errors}");
            }

            _db.UserProfiles.Add(profile);
            _db.UserPreferences.Add(preference);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var verifyUrl = $"{origin}/verify-email?token={encodedToken}&email={HttpUtility.UrlEncode(email)}";
            await _emailSender.SendVerificationEmailAsync(email, verifyUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gửi email xác nhận cho {Email}", email);
        }

        return ToUserDto(user, profile, preference);
    }

    public async Task<(UserDto User, string RawSessionToken)> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new DomainValidationException("Email hoặc mật khẩu không chính xác.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            throw new DomainValidationException("Tài khoản đã bị tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau.");
        }
        if (!signInResult.Succeeded)
        {
            throw new DomainValidationException("Email hoặc mật khẩu không chính xác.");
        }

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? UserProfile.Create(user.Id, email, "Asia/Ho_Chi_Minh", _clock.GetCurrentInstant());
        var preference = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? UserPreference.CreateDefault(user.Id, _clock.GetCurrentInstant());

        var (session, rawToken) = await _sessionManager.CreateSessionAsync(
            user.Id,
            ipAddress,
            userAgent,
            deviceInfo: null,
            ct);

        return (ToUserDto(user, profile, preference), rawToken);
    }

    public async Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        await _sessionManager.RevokeSessionAsync(sessionId, userId, "User logged out", ct);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new DomainValidationException("Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            throw new DomainValidationException("Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string origin, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        // Anti-enumeration: Return without error if user does not exist
        if (user is null)
        {
            _logger.LogInformation("Forgot password requested for non-existent email: {Email}", email);
            return;
        }

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var resetUrl = $"{origin}/reset-password?token={encodedToken}&email={HttpUtility.UrlEncode(email)}";

            await _emailSender.SendPasswordResetEmailAsync(email, resetUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gửi email đặt lại mật khẩu cho {Email}", email);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        PasswordValidator.Validate(request.NewPassword);

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new DomainValidationException("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new DomainValidationException("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        // Revoke all sessions on password reset
        await _sessionManager.RevokeAllUserSessionsAsync(user.Id, null, "Password was reset", ct);
    }

    public async Task RequestEmailChangeAsync(Guid userId, ChangeEmailRequest request, string origin, CancellationToken ct = default)
    {
        var newEmail = (request.NewEmail ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains('@'))
        {
            throw new DomainValidationException("Địa chỉ email mới không hợp lệ.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new ResourceNotFoundException("Không tìm thấy người dùng.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!passwordValid)
        {
            throw new DomainValidationException("Mật khẩu hiện tại không chính xác.");
        }

        var existingUser = await _userManager.FindByEmailAsync(newEmail);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            throw new DomainValidationException("Địa chỉ email mới này đã được sử dụng.");
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var encodedToken = HttpUtility.UrlEncode(token);
        var confirmUrl = $"{origin}/confirm-email-change?token={encodedToken}&newEmail={HttpUtility.UrlEncode(newEmail)}";

        await _emailSender.SendEmailChangeConfirmationAsync(newEmail, confirmUrl, ct);
    }

    public async Task ConfirmEmailChangeAsync(Guid userId, ConfirmEmailChangeRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new ResourceNotFoundException("Không tìm thấy người dùng.");
        }

        var result = await _userManager.ChangeEmailAsync(user, request.NewEmail, request.Token);
        if (!result.Succeeded)
        {
            throw new DomainValidationException("Mã xác nhận đổi email không hợp lệ hoặc đã hết hạn.");
        }

        user.UserName = request.NewEmail;
        await _userManager.UpdateAsync(user);

        var preference = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (preference is not null)
        {
            preference.IncrementRecipientVersion(_clock.GetCurrentInstant());
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new ResourceNotFoundException("Không tìm thấy thông tin tài khoản.");
        }

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? UserProfile.Create(userId, user.UserName ?? "User", "Asia/Ho_Chi_Minh", _clock.GetCurrentInstant());
        var preference = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? UserPreference.CreateDefault(userId, _clock.GetCurrentInstant());

        return ToUserDto(user, profile, preference);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var now = _clock.GetCurrentInstant();
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
        {
            profile = UserProfile.Create(userId, request.DisplayName, request.Timezone, now, request.Locale, request.Theme);
            _db.UserProfiles.Add(profile);
        }
        else
        {
            profile.Update(request.DisplayName, request.Timezone, request.Locale, request.Theme, now);
        }

        await _db.SaveChangesAsync(ct);
        return ToProfileDto(profile);
    }

    public async Task<UserPreferenceDto> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken ct = default)
    {
        var now = _clock.GetCurrentInstant();
        var pref = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (pref is null)
        {
            pref = UserPreference.CreateDefault(userId, now);
            _db.UserPreferences.Add(pref);
        }

        LocalTime? quietStart = null;
        LocalTime? quietEnd = null;

        if (!string.IsNullOrWhiteSpace(request.QuietStart) && TimeOnly.TryParse(request.QuietStart, out var qs))
        {
            quietStart = new LocalTime(qs.Hour, qs.Minute, qs.Second);
        }

        if (!string.IsNullOrWhiteSpace(request.QuietEnd) && TimeOnly.TryParse(request.QuietEnd, out var qe))
        {
            quietEnd = new LocalTime(qe.Hour, qe.Minute, qe.Second);
        }

        pref.Update(
            request.EmailNotificationsEnabled,
            request.QuietHoursEnabled,
            quietStart,
            quietEnd,
            request.QuietTimezone,
            now);

        await _db.SaveChangesAsync(ct);
        return ToPreferenceDto(pref);
    }

    public async Task<(UserDto User, string RawSessionToken)> ProcessGoogleLoginAsync(
        GoogleLoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new DomainValidationException("Thiếu định danh người dùng từ Google (Subject).");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainValidationException("Email từ Google không hợp lệ.");
        }

        // 1. Check if user already linked with this Google subject
        var user = await _userManager.FindByLoginAsync("Google", request.Subject);
        if (user is not null)
        {
            var p = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id, ct)
                ?? UserProfile.Create(user.Id, email, "Asia/Ho_Chi_Minh", _clock.GetCurrentInstant());
            var pr = await _db.UserPreferences.FirstOrDefaultAsync(x => x.UserId == user.Id, ct)
                ?? UserPreference.CreateDefault(user.Id, _clock.GetCurrentInstant());

            var (sess, token) = await _sessionManager.CreateSessionAsync(user.Id, ipAddress, userAgent, null, ct);
            return (ToUserDto(user, p, pr), token);
        }

        // 2. If not linked by Google subject, check if local user exists with the same email
        var userByEmail = await _userManager.FindByEmailAsync(email);
        if (userByEmail is not null)
        {
            // AC02: Do NOT auto link; require local authentication first
            throw new DomainValidationException("Tài khoản với email này đã tồn tại bằng mật khẩu. Vui lòng đăng nhập bằng mật khẩu trước để liên kết tài khoản Google.");
        }

        // 3. New user registration via Google
        if (!request.EmailVerified)
        {
            throw new DomainValidationException("Email từ tài khoản Google chưa được xác minh.");
        }

        var now = _clock.GetCurrentInstant();
        var userId = Guid.NewGuid();
        var timezone = !string.IsNullOrWhiteSpace(request.Timezone) ? request.Timezone : "Asia/Ho_Chi_Minh";
        var displayName = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : email.Split('@')[0];

        var profile = UserProfile.Create(userId, displayName, timezone, now);
        var preference = UserPreference.CreateDefault(userId, now, timezone);

        var newUser = new AppUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true // Verified by Google OIDC
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new DomainValidationException($"Đăng ký tài khoản Google thất bại: {errors}");
            }

            var addLoginResult = await _userManager.AddLoginAsync(
                newUser,
                new UserLoginInfo("Google", request.Subject, "Google"));

            if (!addLoginResult.Succeeded)
            {
                var errors = string.Join("; ", addLoginResult.Errors.Select(e => e.Description));
                throw new DomainValidationException($"Liên kết Google thất bại: {errors}");
            }

            _db.UserProfiles.Add(profile);
            _db.UserPreferences.Add(preference);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var (session, rawToken) = await _sessionManager.CreateSessionAsync(newUser.Id, ipAddress, userAgent, null, ct);
        return (ToUserDto(newUser, profile, preference), rawToken);
    }

    public async Task LinkGoogleAsync(Guid userId, GoogleLinkRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new DomainValidationException("Thiếu định danh Google.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new ResourceNotFoundException("Không tìm thấy người dùng.");
        }

        var existingUserWithLogin = await _userManager.FindByLoginAsync("Google", request.Subject);
        if (existingUserWithLogin is not null && existingUserWithLogin.Id != userId)
        {
            throw new DomainValidationException("Tài khoản Google này đã được liên kết với một tài khoản khác.");
        }

        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider == "Google"))
        {
            return; // Already linked
        }

        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", request.Subject, "Google"));
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainValidationException($"Không thể liên kết Google: {errors}");
        }
    }

    private static UserDto ToUserDto(AppUser user, UserProfile profile, UserPreference preference)
    {
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            ToProfileDto(profile),
            ToPreferenceDto(preference));
    }

    private static UserProfileDto ToProfileDto(UserProfile p)
    {
        return new UserProfileDto(p.DisplayName, p.Timezone, p.Locale, p.Theme, p.Version);
    }

    private static UserPreferenceDto ToPreferenceDto(UserPreference p)
    {
        return new UserPreferenceDto(
            p.EmailNotificationsEnabled,
            p.QuietHoursEnabled,
            p.QuietStart?.ToString("HH:mm", null),
            p.QuietEnd?.ToString("HH:mm", null),
            p.QuietTimezone,
            p.RecipientVersion);
    }
}


