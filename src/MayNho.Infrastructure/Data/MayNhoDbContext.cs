using MayNho.Domain;
using MayNho.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MayNho.Infrastructure.Data;

public class MayNhoDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public MayNhoDbContext(DbContextOptions<MayNhoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity table names
        builder.Entity<AppUser>(b =>
        {
            b.ToTable("users");
            b.HasOne(u => u.Profile)
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(u => u.Preference)
                .WithOne()
                .HasForeignKey<UserPreference>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(u => u.Sessions)
                .WithOne()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentityRole<Guid>>(b => b.ToTable("roles"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens"));
        builder.Entity<DataProtectionKey>(b => b.ToTable("data_protection_keys"));

        // UserProfile
        builder.Entity<UserProfile>(b =>
        {
            b.ToTable("user_profiles");
            b.HasKey(p => p.UserId);
            b.Property(p => p.DisplayName).HasMaxLength(100).IsRequired();
            b.Property(p => p.Timezone).HasMaxLength(50).IsRequired();
            b.Property(p => p.Locale).HasMaxLength(10).IsRequired();
            b.Property(p => p.Theme).HasMaxLength(20).IsRequired();
            b.Property(p => p.Version).IsConcurrencyToken();
        });

        // UserPreference
        builder.Entity<UserPreference>(b =>
        {
            b.ToTable("user_preferences");
            b.HasKey(p => p.UserId);
            b.Property(p => p.QuietTimezone).HasMaxLength(50);
        });

        // UserSession
        builder.Entity<UserSession>(b =>
        {
            b.ToTable("user_sessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.SessionTokenHash).HasMaxLength(64).IsRequired();
            b.Property(s => s.IpAddress).HasMaxLength(45);
            b.Property(s => s.UserAgent).HasMaxLength(512);
            b.Property(s => s.DeviceInfo).HasMaxLength(256);
            b.Property(s => s.RevokedReason).HasMaxLength(256);

            b.HasIndex(s => s.SessionTokenHash);
            b.HasIndex(s => s.UserId);
            b.HasIndex(s => s.ExpiresAt);
        });
    }
}
