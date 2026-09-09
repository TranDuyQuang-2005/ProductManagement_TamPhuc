namespace ProductManagement.Api.Entities;

public sealed class Category
{
    public int Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CodePrefix { get; set; } = string.Empty;
    public int NextProductNumber { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUsername { get; set; }
    public string? CreatedByRole { get; set; }
    public string? LastModifiedByUserId { get; set; }
    public string? LastModifiedByUsername { get; set; }
    public string? LastModifiedByRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
