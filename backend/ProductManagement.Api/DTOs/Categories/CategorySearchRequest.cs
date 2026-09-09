using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Categories;

public sealed class CategorySearchRequest
{
    [StringLength(200)]
    public string? Keyword { get; init; }

    public bool? IsActive { get; init; }

    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(50)]
    public string SortBy { get; init; } = "createdAt";

    [StringLength(4)]
    public string SortDirection { get; init; } = "desc";
}
