using MayNho.Application.Auth;
using MayNho.Application.Common;
using MayNho.Infrastructure.Data;
using MayNho.Infrastructure.Identity;
using MayNho.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MayNho.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("AppDb")
            ?? "Host=localhost;Port=5433;Database=maynho_dev;Username=maynho_user;Password=maynho_dev_password";


        services.AddDbContext<MayNhoDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNodaTime();
                npgsql.MigrationsAssembly(typeof(MayNhoDbContext).Assembly.FullName);
            });
        });

        services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;

            options.Tokens.EmailConfirmationTokenProvider = EmailConfirmationTokenProvider<AppUser>.ProviderName;
            options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProvider<AppUser>.ProviderName;
        })
        .AddEntityFrameworkStores<MayNhoDbContext>()
        .AddTokenProvider<EmailConfirmationTokenProvider<AppUser>>(EmailConfirmationTokenProvider<AppUser>.ProviderName)
        .AddTokenProvider<PasswordResetTokenProvider<AppUser>>(PasswordResetTokenProvider<AppUser>.ProviderName)
        .AddDefaultTokenProviders();

        services.AddDataProtection()
            .PersistKeysToDbContext<MayNhoDbContext>()
            .SetApplicationName("MayNho");

        services.AddSingleton<IClock, SystemClockAdapter>();
        services.AddScoped<ISessionManager, SessionManager>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
