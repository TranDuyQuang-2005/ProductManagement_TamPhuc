namespace ProductManagement.Api.Entities;

public sealed class Product
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
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

    public Category Category { get; set; } = null!;
}
