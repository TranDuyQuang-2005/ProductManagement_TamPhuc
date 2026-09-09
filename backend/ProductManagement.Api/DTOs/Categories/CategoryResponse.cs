namespace ProductManagement.Api.DTOs.Categories;

public sealed record CategoryResponse(
    int Id,
    string CategoryCode,
    string CategoryName,
    string CodePrefix,
    int NextProductNumber,
    string? Description,
    bool IsActive,
    int ProductCount,
    bool HasProducts,
    bool CanEditPrefix,
    string? CreatedByUserId,
    string? CreatedByUsername,
    string? CreatedByRole,
    string? LastModifiedByUserId,
    string? LastModifiedByUsername,
    string? LastModifiedByRole,
    bool IsAdminProtected,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
