// ─────────────────────────────────────────────────────────────────────────────
// imaging-request.service.ts
//
// Sits between ImagingRequestComponent and ReportingRequestService.
// Responsibilities:
//   • Accept the component's flat form model
//   • Map it to the API's CreateReportingRequestDto
//   • Call POST /api/ReportingRequests
//   • Return the created DTO so the component can show a confirmation
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';

import { ReportingRequestService }           from './reporting-request.service';
import { AuthService }                       from '../../auth.service';
import {
  CreateReportingRequestDto,
  ReportingRequestResponseDto,
  GenderEnum,
  ImageModalitiesEnum,
  PrioritiesEnum,
} from '../../models';

// ── Component form model ────────────────────────────────────────────────────
// This mirrors the `model` object inside ImagingRequestComponent.
export interface ImagingRequestFormModel {
  patientName:           string;
  age:                    number | null;   
  patientId:              string;          
  notes:                  string;
  /** Display string from the <select>: 'Male' | 'Female' */
  gender:                string;
  /** ISO date string, e.g. '1990-05-12' */
  patientDateOfBirth:    string;
  patientPhoneNumber:    string;
  patientAddress:        string;
  patientMedicalHistory: string;
  patientNotes:          string;
  /** Display string from the <select>: 'CT' | 'MRI' | 'X-Ray' | 'Ultrasound' */
  scanType:              string;
  /** URL / storage reference for the DICOM image */
  imageUrl:              string;
  suggestedDepartment:   string;
  /** Display string: 'Routine' | 'Urgent' | 'STAT' | 'Emergency' */
  priority:              string;
  /** ISO date-time string */
  dueDate:               string;
  /** UUID of the assigned radiologist */
  assignedRadiologistId: string;
  isEmergency:           boolean;
  emergencyJustification:string;
}

// ── Mapping helpers ──────────────────────────────────────────────────────────

const GENDER_MAP: Record<string, GenderEnum> = {
  Male:   GenderEnum.Male,
  Female: GenderEnum.Female,
};

const MODALITY_MAP: Record<string, ImageModalitiesEnum> = {
  CT:          ImageModalitiesEnum.CT,
  MRI:         ImageModalitiesEnum.MRI,
  'X-Ray':     ImageModalitiesEnum.XRay,
  Ultrasound:  ImageModalitiesEnum.Ultrasound,
};

const PRIORITY_MAP: Record<string, PrioritiesEnum> = {
  Routine:   PrioritiesEnum.Routine,
  Urgent:    PrioritiesEnum.Urgent,
  STAT:      PrioritiesEnum.STAT,
  Emergency: PrioritiesEnum.Emergency,
};

// ── Service ──────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class ImagingRequestService {

  constructor(
    private requestApi: ReportingRequestService,
    private auth:       AuthService,
  ) {}

  /**
   * Validates, maps and submits the imaging request form.
   *
   * @returns The created ReportingRequestResponseDto from the API (201 Created)
   * @throws  Error with a user-facing message if validation or the API call fails
   */
  async submit(form: ImagingRequestFormModel): Promise<ReportingRequestResponseDto> {
    this.validate(form);
    const dto = this.mapFormToDto(form);
    return this.requestApi.create(dto);
  }

  /**
   * Updates an existing request.
   */
  async update(id: string, form: ImagingRequestFormModel): Promise<ReportingRequestResponseDto> {
    this.validate(form);
    const dto = this.mapFormToDto(form);
    return this.requestApi.update(id, dto);
  }

  /**
   * Fetches a single request and maps it to the form model.
   */
  async getById(id: string): Promise<ImagingRequestFormModel> {
    const dto = await this.requestApi.getById(id);
    return this.mapDtoToForm(dto);
  }

  private mapFormToDto(form: ImagingRequestFormModel): CreateReportingRequestDto {
    return {
      patientName:           form.patientName,
      patientDateOfBirth:    new Date(form.patientDateOfBirth).toISOString(),
      patientPhoneNumber:    form.patientPhoneNumber,
      patientGender:         GENDER_MAP[form.gender]    ?? GenderEnum.Male,
      patientAddress:        form.patientAddress,
      patientMedicalHistory: form.patientMedicalHistory,
      patientNotes:          form.patientNotes || null,
      imageModality:         MODALITY_MAP[form.scanType] ?? ImageModalitiesEnum.CT,
      storageReference:      form.imageUrl,
      suggestedDepartment:   form.suggestedDepartment,
      priority:              PRIORITY_MAP[form.priority] ?? PrioritiesEnum.Routine,
      dueDate:               new Date(form.dueDate).toISOString(),
      assignedRadiologistId: form.assignedRadiologistId,
      isEmergency:           form.isEmergency,
      emergencyJustification: form.isEmergency ? form.emergencyJustification : 'N/A',
    };
  }

  private mapDtoToForm(dto: ReportingRequestResponseDto): ImagingRequestFormModel {
    // Reverse maps
    const genderStr = Object.keys(GENDER_MAP).find(k => GENDER_MAP[k] === dto.patientGender) || 'Male';
    const modalityStr = Object.keys(MODALITY_MAP).find(k => MODALITY_MAP[k] === dto.imageModality) || 'CT';
    const priorityStr = Object.keys(PRIORITY_MAP).find(k => PRIORITY_MAP[k] === dto.priority) || 'Routine';

    return {
      patientName:           dto.patientName ?? "",
      patientId:             dto.patientId || '',
      age:                   null, // We don't have age in DTO, it's calculated or ignored
      notes:                 dto.patientNotes || '',
      gender:                genderStr,
      patientDateOfBirth:    dto.patientDateOfBirth ? dto.patientDateOfBirth.split('T')[0] : '',
      patientPhoneNumber:    dto.patientPhoneNumber || '',
      patientAddress:        dto.patientAddress || '',
      patientMedicalHistory: dto.patientMedicalHistory || '',
      patientNotes:          dto.patientNotes || '',
      scanType:              modalityStr,
      imageUrl:              dto.storageReference || '',
      suggestedDepartment:   dto.suggestedDepartment || '',
      priority:              priorityStr,
      dueDate:               dto.dueDate ? dto.dueDate.substring(0, 16) : '', // YYYY-MM-DDTHH:mm
      assignedRadiologistId: dto.assignedRadiologistId || '',
      isEmergency:           dto.isEmergency || false,
      emergencyJustification: dto.emergencyJustification || '',
    };
  }

  // ── Basic client-side validation ────────────────────────────────────────

  private validate(form: ImagingRequestFormModel): void {
    if (!form.patientName?.trim())
      throw new Error('Patient name is required.');
    if (!form.patientDateOfBirth)
      throw new Error('Patient date of birth is required.');
    if (!form.patientPhoneNumber?.trim())
      throw new Error('Patient phone number is required.');
    if (!form.patientAddress?.trim())
      throw new Error('Patient address is required.');
    if (!form.patientMedicalHistory?.trim())
      throw new Error('Medical history is required.');
    if (!form.scanType)
      throw new Error('Scan type is required.');
    if (!form.imageUrl?.trim())
      throw new Error('Image storage reference (URL) is required.');
    if (!form.suggestedDepartment?.trim())
      throw new Error('Suggested department is required.');
    if (!form.dueDate)
      throw new Error('Due date is required.');
    if (!form.assignedRadiologistId?.trim())
      throw new Error('An assigned radiologist is required.');
    if (form.isEmergency && !form.emergencyJustification?.trim())
      throw new Error('Emergency justification is required for emergency requests.');
  }
}