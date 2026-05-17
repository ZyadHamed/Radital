using Domain;

namespace RadiologistAppCore.DTOs
{
    public class RadiologistRequestResponseDto
    {
        public Guid Id { get; set; }

        // --- Patient ---
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime PatientDateOfBirth { get; set; }
        public string PatientPhoneNumber { get; set; } = string.Empty;
        public GenderEnum PatientGender { get; set; }
        public string PatientAddress { get; set; } = string.Empty;
        public string? PatientMedicalHistory { get; set; }
        public string? PatientNotes { get; set; }

        // --- Medical Image ---
        public Guid MedicalImageId { get; set; }
        public ImageModalitiesEnum ImageModality { get; set; }
        public string StorageReference { get; set; } = string.Empty;

        // --- Request Details ---
        public string RequestedByName { get; set; } = string.Empty;
        public string SuggestedDepartment { get; set; } = string.Empty;
        public PrioritiesEnum Priority { get; set; }
        public bool IsEmergency { get; set; }
        public string? EmergencyJustification { get; set; }

        // --- Report ---
        public Guid? ReportId { get; set; }

        // --- Status ---
        public ReportingRequestStatusEnum Status { get; set; }
        public DateTime SubmissionTime { get; set; }
        public DateTime DueDate { get; set; }
        public string StatusLabel => Status.ToString();
    }
}