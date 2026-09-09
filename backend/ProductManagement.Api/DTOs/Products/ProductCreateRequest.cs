using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Products;

public sealed class ProductCreateRequest
{
    [Required, StringLength(250)]
    public string ProductName { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Required, StringLength(50)]
    public string Unit { get; init; } = string.Empty;

    [Required, Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? Price { get; init; }

    [Required, Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? Quantity { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}
