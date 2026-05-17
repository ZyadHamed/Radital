using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.People
{
    public class Patient : Person
    {
        public required string MedicalHistory { get; set; }
        public string Notes { get; set; }
    }
}
