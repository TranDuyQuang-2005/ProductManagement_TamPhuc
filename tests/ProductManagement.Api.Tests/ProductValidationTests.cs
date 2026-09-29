using System.ComponentModel.DataAnnotations;
using ProductManagement.Api.DTOs.Products;

namespace ProductManagement.Api.Tests;

public sealed class ProductValidationTests
{
    [Fact]
    public void CreateRequest_rejects_required_fields()
    {
        var request = new ProductCreateRequest
        {
            ProductName = null!,
            CategoryId = 0,
            Unit = null!,
            Price = null
        };

        var errors = Validate(request);

        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.ProductName)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.CategoryId)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.Unit)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.Price)));
    }

    [Fact]
    public void CreateRequest_rejects_length_boundaries()
    {
        var request = new ProductCreateRequest
        {
            ProductName = new string('A', 251),
            CategoryId = 1,
            Unit = new string('U', 51),
            Price = 1m,
            Description = new string('D', 1001)
        };

        var errors = Validate(request);

        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.ProductName)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.Unit)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductCreateRequest.Description)));
    }

    [Fact]
    public void CreateRequest_accepts_maximum_lengths_and_decimal_boundaries()
    {
        var request = new ProductCreateRequest
        {
            ProductName = new string('A', 250),
            CategoryId = 1,
            Unit = new string('U', 50),
            Price = 9_999_999_999_999_999.99m,
            Description = new string('D', 1000)
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void UpdateRequest_rejects_missing_status_and_out_of_range_price()
    {
        var request = new ProductUpdateRequest
        {
            Id = 1,
            ProductName = "Valid",
            Unit = "kg",
            Price = -0.01m,
            IsActive = null
        };

        var errors = Validate(request);

        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductUpdateRequest.Price)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(ProductUpdateRequest.IsActive)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
