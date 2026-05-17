using Domain.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class ReportingRequest : IdentifiableEntity
    {
        public HospitalStaffMember RequestedBy { get; set; }
        public MedicalImage Image { get; set; }
        public string SuggestedDepartment { get; set; }
        public ReportingRequestStatusEnum Status { get; set; }
        public DateTime SubmissionTime { get; set; }
        public DateTime DueDate { get; set; }

        public DateTime? CompletionTime { get; set; }
        public PrioritiesEnum Priority { get; set; }
        public Radiologist? AssignedRadiologist { get; set; }
        public Guid? ReportId { get; set; }
        public Report? Report { get; set; }
        public bool IsEmergency { get; set; }
        public string? EmergencyJustification { get; set; }
        public DateTime? AssignedAt { get; set; }          
        public List<Guid> EscalationHistory { get; set; } = new(); 
    }
}
