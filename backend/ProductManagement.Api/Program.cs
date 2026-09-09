using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductManagement.Api.Common;
using ProductManagement.Api.Configuration;
using ProductManagement.Api.Data;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Middleware;
using ProductManagement.Api.Repositories;
using ProductManagement.Api.Repositories.Interfaces;
using ProductManagement.Api.Services;
using ProductManagement.Api.Services.Interfaces;
using ProductManagement.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    pair => ToCamelCase(pair.Key),
                    pair => pair.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Giá trị không hợp lệ."
                            : error.ErrorMessage)
                        .ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            return new BadRequestObjectResult(new ApiErrorResponse(
                false,
                "Dữ liệu không hợp lệ.",
                errors,
                context.HttpContext.TraceIdentifier));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<DapperContext>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.Configure<StaffSeedOptions>(builder.Configuration.GetSection("StaffSeed"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration is missing.");
if (Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32)
    throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes.");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // IdentityDbContext uses this max length for string key properties such as
        // UserId, LoginProvider and ProviderKey. 128 keeps composite PKs under
        // SQL Server's 900-byte clustered index key limit.
        options.Stores.MaxLengthForKeys = 128;
        options.User.RequireUniqueEmail = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.StaffOrAdmin, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Staff));
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
builder.Services.AddScoped<IProductCommandRepository, ProductCommandRepository>();
builder.Services.AddScoped<ICategoryQueryRepository, CategoryQueryRepository>();
builder.Services.AddScoped<ICategoryCommandRepository, CategoryCommandRepository>();
builder.Services.AddScoped<IAuditLogQueryRepository, AuditLogQueryRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<AdminUserSeeder>();
builder.Services.AddScoped<StaffUserSeeder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AdminUserSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<StaffUserSeeder>().SeedAsync();
}

app.Run();

static string ToCamelCase(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return value;
    var segments = value.Split('.');
    var last = segments[^1];
    if (string.IsNullOrEmpty(last)) return value;
    return char.ToLowerInvariant(last[0]) + last[1..];
}
