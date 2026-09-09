using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Categories;

public sealed class CategoryCreateRequest
{
    [Required, StringLength(50)]
    public string CategoryCode { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string CategoryName { get; init; } = string.Empty;

    [Required, StringLength(10, MinimumLength = 2)]
    public string CodePrefix { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}
