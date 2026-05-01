using DnsClient;
using Fenrir.Application.Abstractions;
using Fenrir.Infrastructure.Database;
using Fenrir.Infrastructure.Dns;
using Fenrir.Infrastructure.Jobs;
using Fenrir.Infrastructure.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Fenrir.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFenrirInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("FenrirDb")
            ?? "Host=localhost;Port=5432;Database=fenrir_soc_core;Username=fenrir;Password=fenrir_dev_password";

        services.AddDbContext<FenrirDbContext>(options =>
        {
            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IFenrirDataStore, EfFenrirDataStore>();

        services.AddSingleton<ILookupClient>(_ => new LookupClient());
        services.AddScoped<IDnsLookupService, DnsClientLookupService>();
        services.AddScoped<INetworkProbe, TcpNetworkProbe>();

        services.AddScoped<IBackgroundJobScheduler, QuartzNetworkScanScheduler>();
        services.AddQuartz(options =>
        {
            options.SchedulerName = "Fenrir SOC Core Jobs";
            options.UseSimpleTypeLoader();
            options.UseInMemoryStore();
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
