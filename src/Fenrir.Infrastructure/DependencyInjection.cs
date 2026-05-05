using DnsClient;
using Fenrir.Application.Abstractions;
using Fenrir.Infrastructure.Cases;
using Fenrir.Infrastructure.Database;
using Fenrir.Infrastructure.DarkWeb;
using Fenrir.Infrastructure.Dns;
using Fenrir.Infrastructure.Jobs;
using Fenrir.Infrastructure.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            options.ConfigureWarnings(warnings =>
            {
                // EF Core 10 treats pending model changes as a hard failure during database updates.
                // Phase 1 ships an explicit migration for the source-configuration tables, so allow
                // the migration command to apply while the snapshot is regenerated in the next schema pass.
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
            });

            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IFenrirDataStore, EfFenrirDataStore>();
        services.AddScoped<ICaseService, EfCaseService>();

        services.AddSingleton<ILookupClient>(_ => new LookupClient());
        services.AddScoped<IDnsLookupService, DnsClientLookupService>();
        services.AddScoped<INetworkProbe, TcpNetworkProbe>();
        services.AddSingleton(DarkWebProviderOptions.FromConfiguration(configuration.GetSection("DarkWeb")));
        services.AddSingleton<IDarkWebProvider, OpenSourceDarkWebProvider>();
        services.AddSingleton<IDarkWebExposureImportService, LocalDarkWebExposureImportService>();

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
