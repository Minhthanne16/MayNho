using MayNho.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

Console.WriteLine("[MayNho.Migrator] Starting database migration process...");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? configuration.GetConnectionString("AppDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__AppDb")
    ?? "Host=localhost;Port=5433;Database=maynho_dev;Username=maynho_user;Password=maynho_dev_password";



var optionsBuilder = new DbContextOptionsBuilder<MayNhoDbContext>();
optionsBuilder.UseNpgsql(connectionString, npgsql =>
{
    npgsql.UseNodaTime();
    npgsql.MigrationsAssembly(typeof(MayNhoDbContext).Assembly.FullName);
});

using var db = new MayNhoDbContext(optionsBuilder.Options);

const long AdvisoryLockId = 718291471;

try
{
    Console.WriteLine("[MayNho.Migrator] Connecting to database and acquiring advisory lock...");
    await db.Database.OpenConnectionAsync();
    await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({AdvisoryLockId});");
    Console.WriteLine("[MayNho.Migrator] Advisory lock acquired.");

    Console.WriteLine("[MayNho.Migrator] Applying EF Core migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("[MayNho.Migrator] Migrations applied successfully.");

    await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({AdvisoryLockId});");
    Console.WriteLine("[MayNho.Migrator] Advisory lock released.");
    await db.Database.CloseConnectionAsync();

    Console.WriteLine("[MayNho.Migrator] Completed successfully. Exiting code 0.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[MayNho.Migrator] Migration failed with error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    try
    {
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({AdvisoryLockId});");
    }
    catch
    {
        // ignored
    }
    return 1;
}

