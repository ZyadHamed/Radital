using Domain;
using Domain.People;
using RadiologistAppCore.DTOs;
using RadiologistAppCore.Exceptions.Auth;
using RadiologistAppCore.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace RadiologistAppCore.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IRepository<Radiologist> _radiologistRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IPasswordHasher<Radiologist> _passwordHasher;

        public AuthenticationService(
            IRepository<Radiologist> radiologistRepository,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger,
            IPasswordHasher<Radiologist> passwordHasher)
        {
            _radiologistRepository = radiologistRepository;
            _configuration = configuration;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            _logger.LogInformation("Login attempt for Radiologist LoginId: {LoginId}", dto.LoginId);

            var allRadiologists = await _radiologistRepository.GetAllAsync();
            var radiologist = allRadiologists.FirstOrDefault(r =>
                r.LoginId.Equals(dto.LoginId, StringComparison.OrdinalIgnoreCase));

            if (radiologist is null)
            {
                _logger.LogWarning("Login failed — Radiologist LoginId not found: {LoginId}", dto.LoginId);
                throw new AuthenticationFailedException("Invalid LoginId or password.");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                radiologist, radiologist.HashedPassword, dto.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed — invalid password for Radiologist LoginId: {LoginId}", dto.LoginId);
                throw new AuthenticationFailedException("Invalid LoginId or password.");
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation("Rehashing password for Radiologist LoginId: {LoginId}", dto.LoginId);
                radiologist.HashedPassword = _passwordHasher.HashPassword(radiologist, dto.Password);
                await _radiologistRepository.UpdateAsync(radiologist);
                await _radiologistRepository.CommitAsync(Guid.Empty);
            }

            _logger.LogInformation("Login successful for Radiologist LoginId: {LoginId}", dto.LoginId);
            return GenerateAuthResponse(radiologist);
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRadiologistDto dto)
        {
            _logger.LogInformation("Registering new Radiologist with LoginId: {LoginId}", dto.LoginId);

            var allRadiologists = await _radiologistRepository.GetAllAsync();

            if (allRadiologists.Any(r => r.LoginId.Equals(dto.LoginId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateEntityException(
                    $"A radiologist with LoginId '{dto.LoginId}' already exists.");
            }

            if (allRadiologists.Any(r => r.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateEntityException(
                    $"A radiologist with Email '{dto.Email}' already exists.");
            }

            var availabilityTimes = dto.AvailabilityTimes.Select(at => new AvaliabilityTime
            {
                Id = Guid.NewGuid(),
                Day = at.Day,
                StartTime = at.StartTime,
                EndTime = at.EndTime
            }).ToList();

            var radiologist = new Radiologist
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
                Status = dto.Status,
                Speciality = dto.Speciality,
                AvailabilityTimes = availabilityTimes
            };

            radiologist.HashedPassword = _passwordHasher.HashPassword(radiologist, dto.Password);

            await _radiologistRepository.InsertAsync(radiologist);
            await _radiologistRepository.CommitAsync(Guid.Empty);

            _logger.LogInformation("Radiologist registered with Id: {Id}", radiologist.Id);

            return GenerateAuthResponse(radiologist);
        }

        private AuthResponseDto GenerateAuthResponse(Radiologist radiologist)
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
                [JwtRegisteredClaimNames.Sub] = radiologist.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [JwtRegisteredClaimNames.Email] = radiologist.Email,
                ["name"] = radiologist.Name,
                ["loginId"] = radiologist.LoginId,
                [ClaimTypes.Role] = "Radiologist",
                ["speciality"] = radiologist.Speciality.ToString(),
                ["status"] = radiologist.Status.ToString()
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
                RadiologistId = radiologist.Id.ToString(),
                Name = radiologist.Name,
                Email = radiologist.Email
            };
        }
    }
}