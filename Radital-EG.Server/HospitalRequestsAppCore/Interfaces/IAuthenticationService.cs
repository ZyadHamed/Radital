using HospitalRequestsAppCore.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace HospitalRequestsAppCore.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);

        Task<AuthResponseDto> RegisterAsync(RegisterStaffMemberDto dto);
    }
}
