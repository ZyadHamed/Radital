using System;
using System.Collections.Generic;
using System.Text;

namespace HospitalRequestsAppCore.DTOs
{
    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public required DateTime ExpiresAt { get; set; }
        public required string StaffMemberId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string Department { get; set; }
    }
}
