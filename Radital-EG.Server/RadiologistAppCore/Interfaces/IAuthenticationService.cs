using RadiologistAppCore.DTOs;

namespace RadiologistAppCore.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);

        Task<AuthResponseDto> RegisterAsync(RegisterRadiologistDto dto);
    }
}
