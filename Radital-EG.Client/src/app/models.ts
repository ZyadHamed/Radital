// ─────────────────────────────────────────────────────────────────────────────
// models.ts  –  Shared DTOs and enums derived from swagger.json
// ─────────────────────────────────────────────────────────────────────────────

// ── Enums ────────────────────────────────────────────────────────────────────

/** GenderEnum: 0 = Male, 1 = Female */
export enum GenderEnum {
  Male   = 0,
  Female = 1,
}

/** ImageModalitiesEnum: 0=CT, 1=MRI, 2=XRay, 3=Ultrasound */
export enum ImageModalitiesEnum {
  CT          = 0,
  MRI         = 1,
  XRay        = 2,
  Ultrasound  = 3,
}

/** PrioritiesEnum: 0=Routine, 1=Urgent, 2=STAT, 3=Emergency */
export enum PrioritiesEnum {
  Routine   = 0,
  Urgent    = 1,
  STAT      = 2,
  Emergency = 3,
}

/** ReportingRequestStatusEnum: 0=Pending, 1=InReview, 2=Completed, 3=Escalated */
export enum ReportingRequestStatusEnum {
  Pending   = 0,
  InReview  = 1,
  Completed = 2,
  Escalated = 3,
}

/** RolesEnum: 0=Admin, 1=Radiologist, 2=Technician, 3=Physician, 4=Nurse, 5=Receptionist */
export enum RolesEnum {
  Admin         = 0,
  Radiologist   = 1,
  Technician    = 2,
  Physician     = 3,
  Nurse         = 4,
  Receptionist  = 5,
}

/** DepartmentsEnum: 0–9 */
export enum DepartmentsEnum {
  Radiology    = 0,
  Emergency    = 1,
  Cardiology   = 2,
  Neurology    = 3,
  Orthopedics  = 4,
  Oncology     = 5,
  Pediatrics   = 6,
  Gynecology   = 7,
  Surgery      = 8,
  GeneralMed   = 9,
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

export interface LoginRequestDto {
  loginId:  string;
  password: string;
}

export interface RegisterStaffMemberDto {
  name:        string;
  dateOfBirth: string;       // ISO date-time
  phoneNumber: string;
  gender:      GenderEnum;
  address:     string;
  loginId:     string;
  email:       string;
  password:    string;
  department:  DepartmentsEnum;
  role:        RolesEnum;
}

export interface CreateReportingRequestDto {
  patientName:          string;
  patientDateOfBirth:   string;   // ISO date-time
  patientPhoneNumber:   string;
  patientGender:        GenderEnum;
  patientAddress:       string;
  patientMedicalHistory:string;
  patientNotes?:        string | null;
  imageModality:        ImageModalitiesEnum;
  storageReference:     string;
  suggestedDepartment:  string;
  priority:             PrioritiesEnum;
  dueDate:              string;   // ISO date-time
  assignedRadiologistId:string;   // UUID
  isEmergency:          boolean;
  emergencyJustification?: string | null;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

/** Shape of the login 200 response (token + staff info). */
export interface LoginResponseDto {
  token:     string;
  loginId:   string;
  name:      string;
  role:      RolesEnum;
  staffId:   string;   // UUID
}

export interface ReportingRequestResponseDto {
  id:                     string;   // UUID
  requestedById:          string;   // UUID
  requestedByName:        string | null;
  patientId:              string;   // UUID
  patientName:            string | null;
  patientDateOfBirth:     string;
  patientPhoneNumber:     string | null;
  patientGender:          GenderEnum;
  patientAddress:         string | null;
  patientMedicalHistory:  string | null;
  patientNotes:           string | null;
  medicalImageId:         string;   // UUID
  imageModality:          ImageModalitiesEnum;
  storageReference:       string | null;
  suggestedDepartment:    string | null;
  priority:               PrioritiesEnum;
  isEmergency:            boolean;
  emergencyJustification: string | null;
  assignedRadiologistId:  string | null;  // UUID
  assignedRadiologistName:string | null;
  reportId:               string | null;  // UUID
  status:                 ReportingRequestStatusEnum;
  submissionTime:         string;
  dueDate:                string;
  statusLabel:            string | null;
}