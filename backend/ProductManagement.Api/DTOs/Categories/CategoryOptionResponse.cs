namespace ProductManagement.Api.DTOs.Categories;

public sealed record CategoryOptionResponse(int Id, string CategoryCode, string CategoryName, string CodePrefix, bool IsActive);
