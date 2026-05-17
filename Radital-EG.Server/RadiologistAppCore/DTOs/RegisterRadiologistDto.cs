using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace RadiologistAppCore.DTOs
{
    public class RegisterRadiologistDto
    {
        public required string Name { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public required string PhoneNumber { get; set; }
        public required GenderEnum Gender { get; set; }
        public required string Address { get; set; }
        public required string LoginId { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required StatusEnum Status { get; set; }
        public required SpecialityEnum Speciality { get; set; }
        public required List<AvailabilityTimeDto> AvailabilityTimes { get; set; }  

    }
}
