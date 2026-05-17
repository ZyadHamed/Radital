using System;
using System.Collections.Generic;
using System.Text;

namespace HospitalRequestsAppCore.DTOs
{
    public class LoginRequestDto
    {
        public required string LoginId { get; set; }
        public required string Password { get; set; }
    }
}
