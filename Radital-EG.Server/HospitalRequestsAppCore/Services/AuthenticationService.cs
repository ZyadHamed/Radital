using Domain.People;
using HospitalRequestsAppCore.DTOs;
using HospitalRequestsAppCore.Exceptions.Auth;
using HospitalRequestsAppCore.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using System.Text;

namespace HospitalRequestsAppCore.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IRepository<HospitalStaffMember> _staffRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IPasswordHasher<HospitalStaffMember> _passwordHasher;

        public AuthenticationService(
            IRepository<HospitalStaffMember> staffRepository,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger,
            IPasswordHasher<HospitalStaffMember> passwordHasher)
        {
            _staffRepository = staffRepository;
            _configuration = configuration;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            _logger.LogInformation("Login attempt for LoginId: {LoginId}", dto.LoginId);

            var allStaff = await _staffRepository.GetAllAsync();
            var staffMember = allStaff.FirstOrDefault(s =>
                s.LoginId.Equals(dto.LoginId, StringComparison.OrdinalIgnoreCase));

            if (staffMember is null)
            {
                _logger.LogWarning("Login failed — LoginId not found: {LoginId}", dto.LoginId);
                throw new AuthenticationFailedException("Invalid LoginId or password.");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                staffMember, staffMember.HashedPassword, dto.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed — invalid password for LoginId: {LoginId}", dto.LoginId);
                throw new AuthenticationFailedException("Invalid LoginId or password.");
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation("Rehashing password for LoginId: {LoginId}", dto.LoginId);
                staffMember.HashedPassword = _passwordHasher.HashPassword(staffMember, dto.Password);
                await _staffRepository.UpdateAsync(staffMember);
                await _staffRepository.CommitAsync(Guid.Empty);
            }

            _logger.LogInformation("Login successful for LoginId: {LoginId}", dto.LoginId);
            return GenerateAuthResponse(staffMember);
        }


        public async Task<AuthResponseDto> RegisterAsync(RegisterStaffMemberDto dto)
        {
            _logger.LogInformation("Registering new staff member with LoginId: {LoginId}", dto.LoginId);

            var allStaff = await _staffRepository.GetAllAsync();

            if (allStaff.Any(s => s.LoginId.Equals(dto.LoginId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateEntityException(
                    $"A staff member with LoginId '{dto.LoginId}' already exists.");
            }

            if (allStaff.Any(s => s.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateEntityException(
                    $"A staff member with Email '{dto.Email}' already exists.");
            }

            var staffMember = new HospitalStaffMember
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                Gender = dto.Gender,
                Address = dto.Address,
                LoginId = dto.LoginId,
                Email = dto.Email,
                HashedPassword = string.Empty,
                Department = dto.Department,
                Role = dto.Role
            };

            staffMember.HashedPassword = _passwordHasher.HashPassword(staffMember, dto.Password);

            await _staffRepository.InsertAsync(staffMember);
            await _staffRepository.CommitAsync(Guid.Empty);

            _logger.LogInformation("Staff member registered with Id: {Id}", staffMember.Id);

            return GenerateAuthResponse(staffMember);
        }


        private AuthResponseDto GenerateAuthResponse(HospitalStaffMember staff)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"]!;
            var issuer = jwtSection["Issuer"]!;
            var audience = jwtSection["Audience"]!;
            var expirationMinutes = int.Parse(jwtSection["ExpirationInMinutes"] ?? "480");

            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = staff.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [JwtRegisteredClaimNames.Email] = staff.Email,
                ["name"] = staff.Name,
                ["loginId"] = staff.LoginId,
                [ClaimTypes.Role] = staff.Role.ToString(),
                ["department"] = staff.Department.ToString()
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Claims = claims,
                Expires = expiresAt,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                SigningCredentials = credentials
            };

            var tokenHandler = new JsonWebTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                StaffMemberId = staff.Id.ToString(),
                Name = staff.Name,
                Email = staff.Email,
                Role = staff.Role.ToString(),
                Department = staff.Department.ToString()
            };
        }
    }
}
