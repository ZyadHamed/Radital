using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace HospitalRequestsAppCore.DTOs
{
    public class RegisterStaffMemberDto
    {
        // Person fields
        public required string Name { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public required string PhoneNumber { get; set; }
        public required GenderEnum Gender { get; set; }
        public required string Address { get; set; }

        // HospitalStaffMember fields
        public required string LoginId { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required DepartmentsEnum Department { get; set; }
        public required RolesEnum Role { get; set; }
    }
}
