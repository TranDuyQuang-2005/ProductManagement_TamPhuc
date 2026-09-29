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
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

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
        user.Property(x => x.IsActive).IsRequired();
        user.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
        user.Property(x => x.LastLoginAt).HasColumnType("datetime2(3)");

        var category = modelBuilder.Entity<Category>();
        category.ToTable("Categories", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Categories_CategoryCode_NotBlank", "LEN(LTRIM(RTRIM([CategoryCode]))) > 0");
            table.HasCheckConstraint("CK_Categories_CategoryName_NotBlank", "LEN(LTRIM(RTRIM([CategoryName]))) > 0");
            table.HasCheckConstraint("CK_Categories_CodePrefix_Format", "LEN(LTRIM(RTRIM([CodePrefix]))) BETWEEN 2 AND 10 AND [CodePrefix] NOT LIKE '%[^A-Z0-9]%'");
            table.HasCheckConstraint("CK_Categories_NextProductNumber_Positive", "[NextProductNumber] >= 1");
        });
        category.HasKey(x => x.Id);
        category.Property(x => x.CategoryCode).HasMaxLength(20).IsUnicode(false).UseCollation("SQL_Latin1_General_CP1_CI_AS").IsRequired();
        category.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
        category.Property(x => x.CodePrefix).HasMaxLength(10).IsUnicode(false).UseCollation("SQL_Latin1_General_CP1_CI_AS").IsRequired();
        category.Property(x => x.NextProductNumber).IsRequired().HasDefaultValue(1);
        category.Property(x => x.Description).HasMaxLength(500);
        category.Property(x => x.IsActive).IsRequired();
        category.Property(x => x.CreatedByUserId).HasMaxLength(128);
        category.Property(x => x.LastModifiedByUserId).HasMaxLength(128);
        category.Property(x => x.IsAdminProtected).IsRequired().HasDefaultValue(false);
        category.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
        category.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        category.HasIndex(x => x.CategoryCode).IsUnique().HasDatabaseName("UX_Categories_CategoryCode");
        category.HasIndex(x => x.CodePrefix).IsUnique().HasDatabaseName("UX_Categories_CodePrefix");
        category.HasIndex(x => x.CategoryName).HasDatabaseName("IX_Categories_CategoryName");
        category.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Categories_AspNetUsers_CreatedByUserId");
        category.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Categories_AspNetUsers_LastModifiedByUserId");

        var product = modelBuilder.Entity<Product>();
        product.ToTable("Products", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Products_ProductCode_NotBlank", "LEN(LTRIM(RTRIM([ProductCode]))) > 0");
            table.HasCheckConstraint("CK_Products_ProductName_NotBlank", "LEN(LTRIM(RTRIM([ProductName]))) > 0");
            table.HasCheckConstraint("CK_Products_Unit_NotBlank", "LEN(LTRIM(RTRIM([Unit]))) > 0");
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint("CK_Products_StockQuantity_NonNegative", "[StockQuantity] >= 0");
        });
        product.HasKey(x => x.Id);
        product.Property(x => x.ProductCode).HasMaxLength(20).IsUnicode(false).UseCollation("SQL_Latin1_General_CP1_CI_AS").IsRequired();
        product.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        product.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        product.Property(x => x.Price).HasPrecision(18, 2);
        product.Property(x => x.StockQuantity).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
        product.Property(x => x.Description).HasMaxLength(1000);
        product.Property(x => x.IsActive).IsRequired();
        product.Property(x => x.CreatedByUserId).HasMaxLength(128);
        product.Property(x => x.LastModifiedByUserId).HasMaxLength(128);
        product.Property(x => x.IsAdminProtected).IsRequired().HasDefaultValue(false);
        product.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        product.Property(x => x.DeletedByUserId).HasMaxLength(128);
        product.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
        product.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        product.Property(x => x.DeletedAt).HasColumnType("datetime2(3)");
        product.HasQueryFilter(x => !x.IsDeleted);
        product.HasIndex(x => x.ProductCode).IsUnique().HasDatabaseName("UX_Products_ProductCode");
        product.HasIndex(x => x.ProductName).HasDatabaseName("IX_Products_ProductName");
        product.HasIndex(x => x.CategoryId).HasDatabaseName("IX_Products_CategoryId");

        product.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Products_Categories");
        product.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Products_AspNetUsers_CreatedByUserId");
        product.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Products_AspNetUsers_LastModifiedByUserId");
        product.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DeletedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Products_AspNetUsers_DeletedByUserId");

        var audit = modelBuilder.Entity<AuditLog>();
        audit.ToTable("AuditLogs", "dbo", table =>
        {
            table.HasCheckConstraint("CK_AuditLogs_Action_NotBlank", "LEN(LTRIM(RTRIM([Action]))) > 0");
            table.HasCheckConstraint("CK_AuditLogs_EntityType_NotBlank", "LEN(LTRIM(RTRIM([EntityType]))) > 0");
        });
        audit.HasKey(x => x.Id);
        audit.Property(x => x.UserId).HasMaxLength(128);
        audit.Property(x => x.Username).HasMaxLength(256).IsRequired();
        audit.Property(x => x.Action).HasMaxLength(30).IsUnicode(false).IsRequired();
        audit.Property(x => x.EntityType).HasMaxLength(30).IsUnicode(false).IsRequired();
        audit.Property(x => x.EntityId).HasMaxLength(128).IsUnicode(false);
        audit.Property(x => x.EntityCode).HasMaxLength(100).IsUnicode(false).UseCollation("SQL_Latin1_General_CP1_CI_AS");
        audit.Property(x => x.OldValues).HasColumnType("nvarchar(max)");
        audit.Property(x => x.NewValues).HasColumnType("nvarchar(max)");
        audit.Property(x => x.ChangedFields).HasColumnType("nvarchar(max)");
        audit.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);
        audit.Property(x => x.Description).HasMaxLength(500);
        audit.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
        audit.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
        audit.HasIndex(x => x.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        audit.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_AuditLogs_EntityType_EntityId");
        audit.HasIndex(x => x.EntityCode).HasDatabaseName("IX_AuditLogs_EntityCode");
        audit.HasIndex(x => x.Action).HasDatabaseName("IX_AuditLogs_Action");
        audit.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AuditLogs_AspNetUsers_UserId");

        var inventory = modelBuilder.Entity<InventoryTransaction>();
        inventory.ToTable("InventoryTransactions", "dbo", table =>
        {
            table.HasCheckConstraint("CK_InventoryTransactions_QuantityBefore_NonNegative", "[QuantityBefore] >= 0");
            table.HasCheckConstraint("CK_InventoryTransactions_QuantityAfter_NonNegative", "[QuantityAfter] >= 0");
            table.HasCheckConstraint("CK_InventoryTransactions_QuantityChange_NotZero", "[QuantityChange] <> 0");
        });
        inventory.HasKey(x => x.Id);
        inventory.Property(x => x.MovementType).HasMaxLength(20).IsUnicode(false).IsRequired();
        inventory.Property(x => x.QuantityChange).HasPrecision(18, 2);
        inventory.Property(x => x.QuantityBefore).HasPrecision(18, 2);
        inventory.Property(x => x.QuantityAfter).HasPrecision(18, 2);
        inventory.Property(x => x.ReferenceCode).HasMaxLength(50).IsUnicode(false);
        inventory.Property(x => x.Note).HasMaxLength(500);
        inventory.Property(x => x.CreatedByUserId).HasMaxLength(128);
        inventory.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        inventory.HasIndex(x => new { x.ProductId, x.CreatedAt }).HasDatabaseName("IX_InventoryTransactions_ProductId_CreatedAt").IsDescending(false, true);
        inventory.HasIndex(x => x.CreatedByUserId).HasDatabaseName("IX_InventoryTransactions_CreatedByUserId");
        inventory.HasOne(x => x.Product)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_InventoryTransactions_Products_ProductId");
        inventory.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_InventoryTransactions_AspNetUsers_CreatedByUserId");
    }
}
