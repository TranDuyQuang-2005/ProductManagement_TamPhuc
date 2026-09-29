using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Common;

public sealed class LongIdRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "Id khong hop le.")]
    public long Id { get; init; }
}
