using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MayNho.Infrastructure.Data;

public class MayNhoDbContextFactory : IDesignTimeDbContextFactory<MayNhoDbContext>
{
    public MayNhoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MayNhoDbContext>();
        var connectionString = "Host=localhost;Port=5433;Database=maynho_dev;Username=maynho_user;Password=maynho_dev_password";


        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.UseNodaTime();
            npgsql.MigrationsAssembly(typeof(MayNhoDbContext).Assembly.FullName);
        });

        return new MayNhoDbContext(optionsBuilder.Options);
    }
}
