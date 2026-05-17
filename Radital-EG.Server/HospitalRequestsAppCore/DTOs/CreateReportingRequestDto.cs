using Domain;
using Domain.People;

namespace HospitalRequestsAppCore.DTOs
{
    /// <summary>
    /// US-01: Data required from the Hospital Technician to create a new imaging request.
    /// </summary>
    public class CreateReportingRequestDto
    {
        // --- Patient Demographics ---
        public required string PatientName { get; set; }
        public required DateTime PatientDateOfBirth { get; set; }
        public required string PatientPhoneNumber { get; set; }
        public required GenderEnum PatientGender { get; set; }
        public required string PatientAddress { get; set; }
        public required string PatientMedicalHistory { get; set; }
        public string PatientNotes { get; set; } = string.Empty;

        // --- Scan / Request Details ---
        public required ImageModalitiesEnum ImageModality { get; set; }
        public required string StorageReference { get; set; }
        public required string SuggestedDepartment { get; set; }
        public required PrioritiesEnum Priority { get; set; }
        public required DateTime DueDate { get; set; }
        public required Guid AssignedRadiologistId { get; set; }
        public bool IsEmergency { get; set; } = false;
        public string EmergencyJustification { get; set; } = string.Empty;
    }
}
