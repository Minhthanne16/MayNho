using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MayNho.Application.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MayNho.Integration;

public class AuthFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateCookieClient()
    {
        return _factory.CreateDefaultClient(new Uri("http://localhost:5000"),
            new CsrfHandler(), new Microsoft.AspNetCore.Mvc.Testing.Handlers.CookieContainerHandler());
    }

    private sealed class CsrfHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            {
                using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(request.RequestUri!, "/api/v1/auth/csrf"));
                using var response = await base.SendAsync(tokenRequest, ct);
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>(ct);
                request.Headers.Add(token!.HeaderName, token.Token);
            }
            return await base.SendAsync(request, ct);
        }
    }

    private HttpClient CreateUnprotectedClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,

            BaseAddress = new Uri("http://localhost:5000")
        });
    }

    [Fact]
    public async Task Register_WithoutCsrf_IsRejected()
    {
        using var client = CreateUnprotectedClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("csrf@maynho.local", "LongPassword123!", "CSRF"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid CSRF token", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CsrfToken_ReturnsTokenAndHeaderName()
    {
        var client = CreateCookieClient();
        var response = await client.GetAsync("/api/v1/auth/csrf");

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();

        Assert.NotNull(data);
        Assert.Equal("X-CSRF-TOKEN", data.HeaderName);
        Assert.False(string.IsNullOrWhiteSpace(data.Token));
    }

    [Fact]
    public async Task Register_WithPasswordLessThan12Chars_ReturnsBadRequest()
    {
        var client = CreateCookieClient();
        var request = new RegisterRequest("shortpass@maynho.local", "Short123!", "Short Pass", "Asia/Ho_Chi_Minh");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("tối thiểu 12 ký tự", body);
    }

    [Fact]
    public async Task ForgotPassword_AntiEnumeration_ReturnsOkForBothExistentAndNonExistent()
    {
        var client = CreateCookieClient();

        // Non-existent email
        var res1 = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest("doesnotexist@maynho.local"));
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // Register a user
        var uniqueEmail = $"user_{Guid.NewGuid():N}@maynho.local";
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(uniqueEmail, "ValidPassphrase123!", "Forgot Test", "Asia/Ho_Chi_Minh"));

        // Existent email
        var res2 = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(uniqueEmail));
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
    }

    [Fact]
    public async Task Register_Login_Me_Logout_Flow_Succeeds()
    {
        var client = CreateCookieClient();
        var uniqueEmail = $"user_{Guid.NewGuid():N}@maynho.local";
        var password = "SuperSecretPassword123!";

        // 1. Register
        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            uniqueEmail,
            password,
            "Nguyen Van Test",
            "Asia/Ho_Chi_Minh"));

        Assert.Equal(HttpStatusCode.Created, regResponse.StatusCode);

        // 2. Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            uniqueEmail,
            password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // 3. Current User (/api/v1/auth/me)
        var meResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var userDto = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(userDto);
        Assert.Equal(uniqueEmail, userDto.Email);
        Assert.Equal("Nguyen Van Test", userDto.Profile.DisplayName);
        Assert.Equal("Asia/Ho_Chi_Minh", userDto.Profile.Timezone);

        // 4. Update Profile
        var updateProfRes = await client.PatchAsJsonAsync("/api/v1/users/me/profile", new UpdateProfileRequest(
            "Nguyen Van Updated",
            "Asia/Tokyo",
            "ja-JP",
            "dark"));
        Assert.Equal(HttpStatusCode.OK, updateProfRes.StatusCode);

        // 5. Check sessions
        var sessionsRes = await client.GetAsync("/api/v1/auth/sessions");
        Assert.Equal(HttpStatusCode.OK, sessionsRes.StatusCode);
        var sessions = await sessionsRes.Content.ReadFromJsonAsync<List<UserSessionDto>>();
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        Assert.True(sessions[0].IsCurrent);

        // 6. Logout
        var logoutRes = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);

        // 7. Verify unauthenticated after logout
        var afterLogoutMe = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutMe.StatusCode);
    }

    [Fact]
    public async Task TwoSeparateUsers_AreStrictlyIsolated()
    {
        var clientA = CreateCookieClient();
        var clientB = CreateCookieClient();

        var emailA = $"userA_{Guid.NewGuid():N}@maynho.local";
        var emailB = $"userB_{Guid.NewGuid():N}@maynho.local";
        var pass = "VeryLongPassphrase456!";

        // Register A and B
        await clientA.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(emailA, pass, "Alpha", "Asia/Ho_Chi_Minh"));
        await clientB.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(emailB, pass, "Beta", "Asia/Tokyo"));

        // Login A and B
        await clientA.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(emailA, pass));
        await clientB.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(emailB, pass));

        // Verify A sees Alpha
        var meA = await clientA.GetFromJsonAsync<UserDto>("/api/v1/auth/me");
        Assert.NotNull(meA);
        Assert.Equal(emailA.ToLowerInvariant(), meA.Email);
        Assert.Equal("Alpha", meA.Profile.DisplayName);

        // Verify B sees Beta
        var meB = await clientB.GetFromJsonAsync<UserDto>("/api/v1/auth/me");
        Assert.NotNull(meB);
        Assert.Equal(emailB.ToLowerInvariant(), meB.Email);
        Assert.Equal("Beta", meB.Profile.DisplayName);


        // User A cannot revoke user B's session
        var sessionsB = await clientB.GetFromJsonAsync<List<UserSessionDto>>("/api/v1/auth/sessions");
        Assert.NotNull(sessionsB);
        var sessionBId = sessionsB[0].Id;

        // User A calls DELETE /api/v1/auth/sessions/{sessionBId}
        var attackRes = await clientA.DeleteAsync($"/api/v1/auth/sessions/{sessionBId}");
        Assert.Equal(HttpStatusCode.OK, attackRes.StatusCode);

        // User B's session must STILL be active
        var stillActiveB = await clientB.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, stillActiveB.StatusCode);
    }

    [Fact]
    public async Task GoogleAuth_EmailMatchesLocalUser_DoesNotAutoLink_ReturnsBadRequest()
    {
        var client = CreateCookieClient();
        var localEmail = $"local_{Guid.NewGuid():N}@maynho.local";
        var pass = "LocalPassword123!";

        // 1. Register local account
        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            localEmail,
            pass,
            "Local User",
            "Asia/Ho_Chi_Minh"));
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);

        // 2. Attempt Google login with the same email
        var googleRes = await client.PostAsJsonAsync("/api/v1/auth/google/callback", new GoogleLoginRequest(
            Subject: $"google-sub-{Guid.NewGuid():N}",
            Email: localEmail,
            EmailVerified: true,
            Name: "Google Attempter"));

        Assert.Equal(HttpStatusCode.BadRequest, googleRes.StatusCode);
        var body = await googleRes.Content.ReadAsStringAsync();
        Assert.Contains("Tài khoản với email này đã tồn tại bằng mật khẩu", body);
    }

    [Fact]
    public async Task GoogleAuth_NewUser_RegistersAndLogsInSuccessfully()
    {
        var client = CreateCookieClient();
        var googleEmail = $"google_new_{Guid.NewGuid():N}@gmail.com";
        var googleSub = $"sub_{Guid.NewGuid():N}";

        // 1. First Google login creates new account
        var googleRes = await client.PostAsJsonAsync("/api/v1/auth/google/callback", new GoogleLoginRequest(
            Subject: googleSub,
            Email: googleEmail,
            EmailVerified: true,
            Name: "Google User"));

        Assert.Equal(HttpStatusCode.OK, googleRes.StatusCode);
        var userDto = await googleRes.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(userDto);
        Assert.Equal(googleEmail.ToLowerInvariant(), userDto.Email);
        Assert.True(userDto.EmailConfirmed);

        // 2. Call /api/v1/auth/me to verify session cookie is active
        var meRes = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);

        // 3. Repeat Google login with same subject signs in cleanly
        var client2 = CreateCookieClient();
        var repeatRes = await client2.PostAsJsonAsync("/api/v1/auth/google/callback", new GoogleLoginRequest(
            Subject: googleSub,
            Email: googleEmail,
            EmailVerified: true,
            Name: "Google User"));

        Assert.Equal(HttpStatusCode.OK, repeatRes.StatusCode);
    }
}

