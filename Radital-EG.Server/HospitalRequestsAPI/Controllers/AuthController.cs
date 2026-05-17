using HospitalRequestsAppCore.DTOs;
using HospitalRequestsAppCore.Exceptions.Auth;
using HospitalRequestsAppCore.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HospitalRequestsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (AuthenticationFailedException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterStaffMemberDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                return CreatedAtAction(nameof(Login), result);
            }
            catch (DuplicateEntityException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
