namespace ProductManagement.Api.DTOs.Categories;

public sealed record ProductCodePreviewResponse(
    int CategoryId,
    string CodePrefix,
    int NextProductNumber,
    string ProductCodePreview);
