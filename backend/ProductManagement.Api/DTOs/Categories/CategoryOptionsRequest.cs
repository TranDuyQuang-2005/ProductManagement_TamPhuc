namespace ProductManagement.Api.DTOs.Categories;

public sealed class CategoryOptionsRequest
{
    public bool ActiveOnly { get; init; } = true;
}
