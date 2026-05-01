using Fenrir.Application.Abstractions;
using Fenrir.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fenrir.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFenrirApplication(this IServiceCollection services)
    {
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IEmailHeaderCheckService, EmailHeaderCheckService>();
        services.AddScoped<IIocService, IocService>();
        services.AddScoped<IDnsMonitoringService, DnsMonitoringService>();
        services.AddScoped<IDarkWebService, DarkWebService>();
        services.AddScoped<INetworkScanningService, NetworkScanningService>();
        services.AddScoped<INetworkScanExecutor, NetworkScanExecutor>();
        services.AddScoped<ISiemService, SiemService>();
        services.AddScoped<IFindingService, FindingService>();
        services.AddScoped<IJobService, JobService>();
        services.AddSingleton<IDarkWebProvider, NoopDarkWebProvider>();
        return services;
    }
}
