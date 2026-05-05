using Fenrir.Application.Abstractions;
using Fenrir.Application.Services;
using Fenrir.Application.Siem.Parsing;
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
        services.AddScoped<ISiemIngestionWorker, SiemService>();
        services.AddHostedService<SiemIngestionWorkerHostedService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAgentPackageBuilder, AgentPackageBuilder>();
        services.AddScoped<ISiemParserRegistry, SiemParserRegistry>();
        services.AddScoped<ISiemParser, GenericJsonSiemParser>();
        services.AddScoped<ISiemParser, ZeekJsonSiemParser>();
        services.AddScoped<ISiemParser, SuricataEveJsonSiemParser>();
        services.AddScoped<ISiemParser, M365AuditSiemParser>();
        services.AddScoped<ISiemParser, AwsCloudTrailSiemParser>();
        services.AddScoped<ISiemParser, WindowsEventJsonSiemParser>();
        services.AddScoped<ISiemParser, SyslogBasicSiemParser>();
        services.AddScoped<IFindingService, FindingService>();
        services.AddScoped<IJobService, JobService>();
        services.AddSingleton<IDarkWebProvider, NoopDarkWebProvider>();
        return services;
    }
}
