using ProductManagement.Api.DTOs.Auth;

namespace ProductManagement.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
