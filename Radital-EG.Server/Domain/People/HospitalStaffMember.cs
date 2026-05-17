using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.People
{
    public class HospitalStaffMember : Person
    {
        public required string LoginId { get; set; }
        public required string Email { get; set; }
        public required string HashedPassword { get; set; }
        public DepartmentsEnum Department { get; set; }
        public RolesEnum Role { get; set; }
    }
}
