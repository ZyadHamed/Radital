using System;
using System.Collections.Generic;
using System.Text;

namespace RadiologistAppCore.DTOs
{
    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public required DateTime ExpiresAt { get; set; }
        public required string RadiologistId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
    }
}
