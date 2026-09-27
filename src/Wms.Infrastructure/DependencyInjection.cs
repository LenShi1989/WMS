using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Application.Interfaces;
using Wms.Infrastructure.Persistence;
using Wms.Infrastructure.Services;

namespace Wms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("缺少連線字串設定 ConnectionStrings:Default。");

        services.AddDbContext<WmsDbContext>(options =>
        {
            // 不啟用 EnableRetryOnFailure：庫存流程大量使用顯式交易，
            // 重試策略會與 BeginTransaction 衝突，且交易重送對庫存異動並不安全。
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
            });
        });

        services.AddScoped<IWmsDbContext>(provider => provider.GetRequiredService<WmsDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<INumberGenerator, NumberGenerator>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}
