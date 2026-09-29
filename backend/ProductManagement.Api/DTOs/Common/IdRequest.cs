using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Common;

public sealed class IdRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id khong hop le.")]
    public int Id { get; init; }
}
