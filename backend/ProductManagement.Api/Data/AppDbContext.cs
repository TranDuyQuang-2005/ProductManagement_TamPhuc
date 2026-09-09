using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Entities;

namespace ProductManagement.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Keep ASP.NET Identity key columns below SQL Server's 900-byte clustered-key limit.
        // NVARCHAR(128) => at most 256 bytes per key segment.
        var role = modelBuilder.Entity<IdentityRole>();
        role.Property(x => x.Id).HasMaxLength(128);

        var roleClaim = modelBuilder.Entity<IdentityRoleClaim<string>>();
        roleClaim.Property(x => x.RoleId).HasMaxLength(128);

        var userClaim = modelBuilder.Entity<IdentityUserClaim<string>>();
        userClaim.Property(x => x.UserId).HasMaxLength(128);

        var userLogin = modelBuilder.Entity<IdentityUserLogin<string>>();
        userLogin.Property(x => x.LoginProvider).HasMaxLength(128);
        userLogin.Property(x => x.ProviderKey).HasMaxLength(128);
        userLogin.Property(x => x.UserId).HasMaxLength(128);

        var userRole = modelBuilder.Entity<IdentityUserRole<string>>();
        userRole.Property(x => x.UserId).HasMaxLength(128);
        userRole.Property(x => x.RoleId).HasMaxLength(128);

        var userToken = modelBuilder.Entity<IdentityUserToken<string>>();
        userToken.Property(x => x.UserId).HasMaxLength(128);
        userToken.Property(x => x.LoginProvider).HasMaxLength(128);
        userToken.Property(x => x.Name).HasMaxLength(128);

        var user = modelBuilder.Entity<ApplicationUser>();
        user.Property(x => x.Id).HasMaxLength(128);
        user.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        user.Property(x => x.Role).HasMaxLength(50).IsRequired();
        user.Property(x => x.IsActive).IsRequired();
        user.Property(x => x.CreatedAt).IsRequired();

        var category = modelBuilder.Entity<Category>();
        category.ToTable("Categories", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Categories_CategoryCode_NotBlank", "LEN(LTRIM(RTRIM([CategoryCode]))) > 0");
            table.HasCheckConstraint("CK_Categories_CategoryName_NotBlank", "LEN(LTRIM(RTRIM([CategoryName]))) > 0");
            table.HasCheckConstraint("CK_Categories_CodePrefix_Format", "LEN(LTRIM(RTRIM([CodePrefix]))) BETWEEN 2 AND 10 AND [CodePrefix] NOT LIKE '%[^A-Z0-9]%'");
            table.HasCheckConstraint("CK_Categories_NextProductNumber_Positive", "[NextProductNumber] >= 1");
            table.HasCheckConstraint("CK_Categories_CreatedByRole_Valid", "[CreatedByRole] IS NULL OR [CreatedByRole] IN (N'ADMIN', N'STAFF')");
            table.HasCheckConstraint("CK_Categories_LastModifiedByRole_Valid", "[LastModifiedByRole] IS NULL OR [LastModifiedByRole] IN (N'ADMIN', N'STAFF')");
        });
        category.HasKey(x => x.Id);
        category.Property(x => x.CategoryCode).HasMaxLength(50).UseCollation("SQL_Latin1_General_CP1_CI_AS").IsRequired();
        category.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
        category.Property(x => x.CodePrefix).HasMaxLength(10).UseCollation("SQL_Latin1_General_CP1_CI_AS").IsRequired();
        category.Property(x => x.NextProductNumber).IsRequired().HasDefaultValue(1);
        category.Property(x => x.Description).HasMaxLength(500);
        category.Property(x => x.IsActive).IsRequired();
        category.Property(x => x.CreatedByUserId).HasMaxLength(128);
        category.Property(x => x.CreatedByUsername).HasMaxLength(256);
        category.Property(x => x.CreatedByRole).HasMaxLength(50);
        category.Property(x => x.LastModifiedByUserId).HasMaxLength(128);
        category.Property(x => x.LastModifiedByUsername).HasMaxLength(256);
        category.Property(x => x.LastModifiedByRole).HasMaxLength(50);
        category.Property(x => x.CreatedAt).IsRequired();
        category.HasIndex(x => x.CategoryCode).IsUnique().HasDatabaseName("UX_Categories_CategoryCode");
        category.HasIndex(x => x.CodePrefix).IsUnique().HasDatabaseName("UX_Categories_CodePrefix");
        category.HasIndex(x => x.CategoryName).HasDatabaseName("IX_Categories_CategoryName");

        var product = modelBuilder.Entity<Product>();
        product.ToTable("Products", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Products_ProductCode_NotBlank", "LEN(LTRIM(RTRIM([ProductCode]))) > 0");
            table.HasCheckConstraint("CK_Products_ProductName_NotBlank", "LEN(LTRIM(RTRIM([ProductName]))) > 0");
            table.HasCheckConstraint("CK_Products_Unit_NotBlank", "LEN(LTRIM(RTRIM([Unit]))) > 0");
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint("CK_Products_Quantity_NonNegative", "[Quantity] >= 0");
            table.HasCheckConstraint("CK_Products_CreatedByRole_Valid", "[CreatedByRole] IS NULL OR [CreatedByRole] IN (N'ADMIN', N'STAFF')");
            table.HasCheckConstraint("CK_Products_LastModifiedByRole_Valid", "[LastModifiedByRole] IS NULL OR [LastModifiedByRole] IN (N'ADMIN', N'STAFF')");
        });
        product.HasKey(x => x.Id);
        product.Property(x => x.ProductCode).HasMaxLength(50).UseCollation("SQL_Latin1_General_CP1_CI_AS").IsRequired();
        product.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        product.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        product.Property(x => x.Price).HasPrecision(18, 2);
        product.Property(x => x.Quantity).HasPrecision(18, 2);
        product.Property(x => x.Description).HasMaxLength(1000);
        product.Property(x => x.IsActive).IsRequired();
        product.Property(x => x.CreatedByUserId).HasMaxLength(128);
        product.Property(x => x.CreatedByUsername).HasMaxLength(256);
        product.Property(x => x.CreatedByRole).HasMaxLength(50);
        product.Property(x => x.LastModifiedByUserId).HasMaxLength(128);
        product.Property(x => x.LastModifiedByUsername).HasMaxLength(256);
        product.Property(x => x.LastModifiedByRole).HasMaxLength(50);
        product.Property(x => x.CreatedAt).IsRequired();
        product.HasIndex(x => x.ProductCode).IsUnique().HasDatabaseName("UX_Products_ProductCode");
        product.HasIndex(x => x.ProductName).HasDatabaseName("IX_Products_ProductName");
        product.HasIndex(x => x.CategoryId).HasDatabaseName("IX_Products_CategoryId");

        product.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Products_Categories");

        var audit = modelBuilder.Entity<AuditLog>();
        audit.ToTable("AuditLogs", "dbo", table =>
        {
            table.HasCheckConstraint("CK_AuditLogs_Action_NotBlank", "LEN(LTRIM(RTRIM([Action]))) > 0");
            table.HasCheckConstraint("CK_AuditLogs_EntityType_NotBlank", "LEN(LTRIM(RTRIM([EntityType]))) > 0");
        });
        audit.HasKey(x => x.Id);
        audit.Property(x => x.UserId).HasMaxLength(128);
        audit.Property(x => x.Username).HasMaxLength(256).IsRequired();
        audit.Property(x => x.Action).HasMaxLength(50).IsRequired();
        audit.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        audit.Property(x => x.EntityId).HasMaxLength(100);
        audit.Property(x => x.EntityCode).HasMaxLength(100).UseCollation("SQL_Latin1_General_CP1_CI_AS");
        audit.Property(x => x.OldValues).HasColumnType("nvarchar(max)");
        audit.Property(x => x.NewValues).HasColumnType("nvarchar(max)");
        audit.Property(x => x.ChangedFields).HasColumnType("nvarchar(max)");
        audit.Property(x => x.IpAddress).HasMaxLength(64);
        audit.Property(x => x.Description).HasMaxLength(500);
        audit.Property(x => x.CreatedAt).IsRequired();
        audit.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
        audit.HasIndex(x => x.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        audit.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_AuditLogs_EntityType_EntityId");
        audit.HasIndex(x => x.EntityCode).HasDatabaseName("IX_AuditLogs_EntityCode");
        audit.HasIndex(x => x.Action).HasDatabaseName("IX_AuditLogs_Action");
    }
}
