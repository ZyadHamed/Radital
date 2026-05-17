using Domain.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class Report : IdentifiableEntity
    {
        public required string ClinicalHistory { get; set; }
        public required string Technique { get; set; }
        public required string Findings { get; set; }
        public required string Impression { get; set; }
        public required string Recommendation { get; set; }
        public required ReportStatusEnum Status { get; set; }
        public required Radiologist Author { get; set; }
        public required string StorageReference { get; set; }
    }
}
