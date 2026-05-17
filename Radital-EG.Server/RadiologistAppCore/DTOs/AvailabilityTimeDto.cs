using System;
using System.Collections.Generic;
using System.Text;

namespace RadiologistAppCore.DTOs
{
    public class AvailabilityTimeDto
    {
        public required DayOfWeek Day { get; set; }
        public required TimeSpan StartTime { get; set; }
        public required TimeSpan EndTime { get; set; }
    }
}
