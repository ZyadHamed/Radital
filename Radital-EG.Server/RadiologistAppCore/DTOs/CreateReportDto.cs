namespace RadiologistAppCore.DTOs
{
    public class CreateReportDto
    {
        public required Guid ReportingRequestId { get; set; }
        public required string ClinicalHistory { get; set; }
        public required string Technique { get; set; }
        public required string Findings { get; set; }
        public required string Impression { get; set; }
        public required string Recommendation { get; set; }
    }
}