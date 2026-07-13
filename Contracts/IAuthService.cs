using IBSMobile.DTOs;

namespace IBSMobile.Contracts;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
}
