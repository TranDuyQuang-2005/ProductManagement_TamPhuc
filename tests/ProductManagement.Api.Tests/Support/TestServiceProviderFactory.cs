using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Api.Data;
using ProductManagement.Api.Repositories;
using ProductManagement.Api.Repositories.Interfaces;
using ProductManagement.Api.Services;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Tests.Support;

internal static class TestServiceProviderFactory
{
    public static ServiceProvider Create(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<DapperContext>();
        services.AddSingleton<ICurrentUserService, AdminCurrentUserService>();
        services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
        services.AddScoped<IProductCommandRepository, ProductCommandRepository>();
        services.AddScoped<ICategoryQueryRepository, CategoryQueryRepository>();
        services.AddScoped<IAuditLogQueryRepository, AuditLogQueryRepository>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
