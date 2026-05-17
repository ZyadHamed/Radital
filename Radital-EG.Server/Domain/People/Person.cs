using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.People
{
public class Person: IdentifiableEntity
    {

        public required string Name { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public required string PhoneNumber { get; set; }
        public required GenderEnum Gender { get; set; }
        public required string Address { get; set; }

    }
}
