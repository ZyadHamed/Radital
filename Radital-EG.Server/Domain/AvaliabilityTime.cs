using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class AvaliabilityTime : IdentifiableEntity
    {
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public AvaliabilityTime()
        {
        }

        public AvaliabilityTime(DayOfWeek day, TimeSpan startTime, TimeSpan endTime)
        {
            Day = day;
            StartTime = startTime;
            EndTime = endTime;
        }

        public bool IsAvailable(DayOfWeek day, TimeSpan time)
        {
            return Day == day && time >= StartTime && time <= EndTime;
        }
    }
}
