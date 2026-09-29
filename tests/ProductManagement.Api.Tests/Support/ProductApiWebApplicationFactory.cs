using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ProductManagement.Api.Data;
using ProductManagement.Api.Entities;

namespace ProductManagement.Api.Tests.Support;

public sealed class ProductApiWebApplicationFactory(SqlServerTestDatabaseFixture database)
    : WebApplicationFactory<Product>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
                ["Jwt:Issuer"] = "ProductManagement.Api",
                ["Jwt:Audience"] = "ProductManagement.Ui",
                ["Jwt:Secret"] = "ProductManagement_Testing_Secret_Key_1234567890"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DapperContext>();

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(database.ConnectionString));
            services.AddSingleton(_ =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = database.ConnectionString
                    })
                    .Build();
                return new DapperContext(configuration);
            });

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });
        });
    }
}
