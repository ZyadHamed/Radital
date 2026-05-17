using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public enum GenderEnum
    {
        Male,
        Female
    }

    public enum StatusEnum
    {
        Active,
        Inactive
    }

    public enum SpecialityEnum
    {
        General,
        Neuroradiology,
        CardiovascularRadiology,
        PediatricRadiology,
        MusculoskeletalRadiology,
        AbdominalRadiology,
        BreastImaging,
        InterventionalRadiology,
        NuclearMedicine,
        EmergencyRadiology
    }

    public enum DepartmentsEnum
    {
        Radiology,
        EmergencyDepartment,
        Cardiology,
        Neurology,
        Orthopedics,
        Pediatrics,
        Oncology,
        Surgery,
        InternalMedicine,
        ICU
    }

    public enum RolesEnum
    {
        Technician,
        SeniorTechnician,
        Nurse,
        Receptionist,
        DepartmentHead,
        Administrator
    }

    public enum ImageModalitiesEnum
    {
        XRay,
        MRI,
        CTScan,
        Ultrasound
    }

    public enum ReportStatusEnum
    {
        Draft,
        Completed,
        Reviewed
    }

    public enum ReportingRequestStatusEnum
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    public enum PrioritiesEnum
    {
        Low,
        Medium,
        High,
        Critical
    }

}
