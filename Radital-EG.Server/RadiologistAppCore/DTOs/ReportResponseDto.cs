using Domain;

namespace RadiologistAppCore.DTOs
{
    public class ReportResponseDto
    {
        public Guid Id { get; set; }
        public Guid ReportingRequestId { get; set; }
        public string ClinicalHistory { get; set; } = string.Empty;
        public string Technique { get; set; } = string.Empty;
        public string Findings { get; set; } = string.Empty;
        public string Impression { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public ReportStatusEnum Status { get; set; }
        public string StatusLabel => Status.ToString();

        // --- Author ---
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;

        // --- PDF ---
        public string StorageReference { get; set; } = string.Empty;
    }
}