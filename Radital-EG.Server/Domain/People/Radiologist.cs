using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.People
{
    public class Radiologist : Person
    {
        public required string Email { get; set; }
        public required string LoginId { get; set; }
        public required string HashedPassword { get; set; }
        public required StatusEnum Status { get; set; }
        public required SpecialityEnum Speciality { get; set; }
        public required List<AvaliabilityTime> AvailabilityTimes { get; set; }
    }
}
