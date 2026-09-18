using System.Threading.RateLimiting;
using MayNho.Api.Authentication;
using MayNho.Api.Endpoints;
using MayNho.Application.Common;
using MayNho.Infrastructure;
using MayNho.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "mn_csrf";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = false;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = SessionAuthenticationHandler.SchemeName;
    options.DefaultChallengeScheme = SessionAuthenticationHandler.SchemeName;
    options.DefaultScheme = SessionAuthenticationHandler.SchemeName;
})
.AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
    SessionAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-policy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            detail: "Quá nhiều yêu cầu. Vui lòng thử lại sau ít phút.",
            extensions: new Dictionary<string, object?> { ["code"] = "rate_limit_exceeded" })
            .ExecuteAsync(context.HttpContext);
    };
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method))
    {
        try
        {
            await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>()
                .ValidateRequestAsync(context);
        }
        catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
        {
            await Results.Problem(statusCode: 400, title: "Invalid CSRF token").ExecuteAsync(context);
            return;
        }
    }
    await next();
});

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.MapGet("/health/ready", async (MayNhoDbContext db, CancellationToken ct) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        if (canConnect)
        {
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync(ct);
            if (pendingMigrations.Any())
            {
                return Results.Problem(
                    statusCode: 503,
                    title: "Database has pending migrations",
                    extensions: new Dictionary<string, object?> { ["code"] = "database_pending_migrations" });
            }

            return Results.Ok(new { status = "ready", database = "connected" });
        }
    }
    catch
    {
        // Fail closed on error
    }

    return Results.Problem(
        statusCode: 503,
        title: "Database connection is not ready",
        extensions: new Dictionary<string, object?> { ["code"] = "database_not_ready" });
});

app.MapGet("/version", () => Results.Ok(new
{
    application = "MayNho",
    milestone = "M1-identity",
    gitSha = Environment.GetEnvironmentVariable("RELEASE_SHA") ?? "local"
}));

app.MapGet("/api/v1/hello", () => Results.Ok(new { message = "Mây Nhỏ — Hôm nay mình làm từng chút nhé" }));

app.MapAuthEndpoints();

app.MapFallback("/api/{**path}", () => Results.Problem(statusCode: 404, title: "Endpoint not found"));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;



