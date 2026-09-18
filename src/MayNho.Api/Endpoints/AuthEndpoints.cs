using System.Security.Claims;
using MayNho.Api.Authentication;
using MayNho.Application.Auth;
using MayNho.Application.Common;
using MayNho.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace MayNho.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1");

        // CSRF Token
        group.MapGet("/auth/csrf", (IAntiforgery antiforgery, HttpContext httpContext) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return Results.Ok(new CsrfTokenResponse(
                tokens.HeaderName ?? "X-CSRF-TOKEN",
                tokens.RequestToken ?? string.Empty));
        });

        // Register
        group.MapPost("/auth/register", async (
            [FromBody] RegisterRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            try
            {
                var origin = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var user = await authService.RegisterAsync(request, origin, ct);
                return Results.Created($"/api/v1/users/{user.Id}", user);
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireRateLimiting("auth-policy");

        // Login
        group.MapPost("/auth/login", async (
            [FromBody] LoginRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            try
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                var (user, rawToken) = await authService.LoginAsync(request, ip, userAgent, ct);

                httpContext.Response.Cookies.Append(
                    SessionAuthenticationHandler.CookieName,
                    rawToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = httpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });

                return Results.Ok(user);
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireRateLimiting("auth-policy");

        // Logout
        group.MapPost("/auth/logout", async (
            IAuthService authService,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (currentUser.UserId.HasValue && currentUser.SessionId.HasValue)
            {
                await authService.LogoutAsync(currentUser.UserId.Value, currentUser.SessionId.Value, ct);
            }

            httpContext.Response.Cookies.Delete(
                SessionAuthenticationHandler.CookieName,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });

            return Results.Ok(new { message = "Đã đăng xuất thành công." });
        }).RequireAuthorization();

        // Current User (Me)
        group.MapGet("/auth/me", async (
            IAuthService authService,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                var user = await authService.GetCurrentUserAsync(currentUser.UserId.Value, ct);
                return Results.Ok(user);
            }
            catch (ResourceNotFoundException ex)
            {
                return Results.Problem(statusCode: 404, title: ex.Message);
            }
        }).RequireAuthorization();

        // Verify Email
        group.MapPost("/auth/verify-email", async (
            [FromBody] VerifyEmailRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            try
            {
                await authService.VerifyEmailAsync(request, ct);
                return Results.Ok(new { message = "Địa chỉ email đã được xác minh thành công." });
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        });

        // Forgot Password
        group.MapPost("/auth/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var origin = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            await authService.ForgotPasswordAsync(request, origin, ct);
            return Results.Ok(new { message = "Nếu email hợp lệ, liên kết đặt lại mật khẩu đã được gửi." });
        }).RequireRateLimiting("auth-policy");

        // Google Auth URL
        group.MapGet("/auth/google/url", (IConfiguration config, HttpContext httpContext) =>
        {
            var clientId = config["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Results.Problem(
                    statusCode: 501,
                    title: "Google authentication is not configured on this server.",
                    extensions: new Dictionary<string, object?> { ["code"] = "google_not_configured" });
            }

            var redirectUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/auth/google/callback";
            var state = Guid.NewGuid().ToString("N");
            var nonce = Guid.NewGuid().ToString("N");

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString("openid email profile")}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&state={state}" +
                $"&nonce={nonce}" +
                $"&access_type=offline" +
                $"&prompt=select_account";

            return Results.Ok(new { url = authUrl, state });
        });

        // Google Callback / Login
        group.MapPost("/auth/google/callback", async (
            [FromBody] GoogleLoginRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            try
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                var (user, rawToken) = await authService.ProcessGoogleLoginAsync(request, ip, userAgent, ct);

                httpContext.Response.Cookies.Append(
                    SessionAuthenticationHandler.CookieName,
                    rawToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = httpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });

                return Results.Ok(user);
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireRateLimiting("auth-policy");

        // Google Link Account (for logged-in user)
        group.MapPost("/auth/google/link", async (
            [FromBody] GoogleLinkRequest request,
            IAuthService authService,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                await authService.LinkGoogleAsync(currentUser.UserId.Value, request, ct);
                return Results.Ok(new { message = "Liên kết tài khoản Google thành công." });
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireAuthorization();

        // Reset Password
        group.MapPost("/auth/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            try
            {
                await authService.ResetPasswordAsync(request, ct);
                return Results.Ok(new { message = "Mật khẩu đã được thay đổi thành công. Vui lòng đăng nhập lại." });
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        });

        // Email Change Request
        group.MapPost("/auth/email-change", async (
            [FromBody] ChangeEmailRequest request,
            IAuthService authService,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                var origin = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                await authService.RequestEmailChangeAsync(currentUser.UserId.Value, request, origin, ct);
                return Results.Ok(new { message = "Email xác nhận thay đổi đã được gửi đến địa chỉ mới." });
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireAuthorization();

        // Confirm Email Change
        group.MapPost("/auth/email-change/confirm", async (
            [FromBody] ConfirmEmailChangeRequest request,
            IAuthService authService,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                await authService.ConfirmEmailChangeAsync(currentUser.UserId.Value, request, ct);
                return Results.Ok(new { message = "Thay đổi email thành công." });
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireAuthorization();

        // List User Sessions
        group.MapGet("/auth/sessions", async (
            ISessionManager sessionManager,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue || !currentUser.SessionId.HasValue)
            {
                return Results.Unauthorized();
            }

            var sessions = await sessionManager.GetUserSessionsAsync(
                currentUser.UserId.Value,
                currentUser.SessionId.Value,
                ct);

            return Results.Ok(sessions);
        }).RequireAuthorization();

        // Revoke Session
        group.MapDelete("/auth/sessions/{id:guid}", async (
            Guid id,
            ISessionManager sessionManager,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            await sessionManager.RevokeSessionAsync(id, currentUser.UserId.Value, "Revoked by user", ct);
            return Results.Ok(new { message = "Đã thu hồi phiên thành công." });
        }).RequireAuthorization();

        // Update Profile
        group.MapPatch("/users/me/profile", async (
            [FromBody] UpdateProfileRequest request,
            IAuthService authService,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                var profile = await authService.UpdateProfileAsync(currentUser.UserId.Value, request, ct);
                return Results.Ok(profile);
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireAuthorization();

        // Update Preferences
        group.MapPatch("/users/me/preferences", async (
            [FromBody] UpdatePreferencesRequest request,
            IAuthService authService,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            try
            {
                var prefs = await authService.UpdatePreferencesAsync(currentUser.UserId.Value, request, ct);
                return Results.Ok(prefs);
            }
            catch (DomainValidationException ex)
            {
                return Results.Problem(statusCode: 400, title: ex.Message);
            }
        }).RequireAuthorization();

        return group;
    }
}

