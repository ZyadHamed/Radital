// ─────────────────────────────────────────────────────────────────────────────
// radiologist-dashboard.service.ts
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';
import { AuthService } from '../../auth.service';
import { CaseCard }    from './radiologist-dashboard';

export interface WorkloadDto {
  id:                     string;
  patientId:              string;
  patientName:            string;
  patientDateOfBirth:     string;
  patientPhoneNumber:     string;
  patientGender:          number;
  patientAddress:         string;
  patientMedicalHistory:  string;
  patientNotes:           string;
  medicalImageId:         string;
  imageModality:          number;
  storageReference:       string;
  requestedByName:        string;
  suggestedDepartment:    string;
  priority:               number;
  isEmergency:            boolean;
  emergencyJustification: string;
  reportId:               string;
  status:                 number;
  submissionTime:         string;
  dueDate:                string;
  statusLabel:            string;
}

// priority 0 = Routine, 1 = Urgent, 2 = STAT, 3 = Emergency
const MODALITY_LABEL: Record<number, 'MRI' | 'CT Scan' | 'X-Ray'> = {
  0: 'CT Scan',
  1: 'MRI',
  2: 'X-Ray',
  3: 'MRI',        // Ultrasound has no CaseCard equivalent — map to closest
};

function formatSubmissionTime(iso: string): string {
  const diff = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
  if (diff < 60)   return `${diff}m ago`;
  if (diff < 1440) return `${Math.floor(diff / 60)}h ago`;
  return `${Math.floor(diff / 1440)}d ago`;
}

function dtoToCaseCard(dto: WorkloadDto): CaseCard {
  const isStat = dto.isEmergency || dto.priority >= 2;
  return {
    id:             0,                           // not used for display
    uuid:           dto.id,
    caseNumber:     `Case #${dto.id.substring(0, 4).toUpperCase()}`,
    patientId:      `PATIENT ID: ${dto.patientId.substring(0, 6).toUpperCase()}`,
    patientName:    dto.patientName,
    modality:       MODALITY_LABEL[dto.imageModality] ?? 'MRI',
    submissionTime: formatSubmissionTime(dto.submissionTime),
    isStat,
    action:         isStat ? 'OPEN CRITICAL STUDY' : 'REVIEW SERIES',

    // Extended fields
    priority:               dto.priority,
    isEmergency:            dto.isEmergency,
    emergencyJustification: dto.emergencyJustification,
    status:                 dto.status,
    statusLabel:            dto.statusLabel,
    dueDate:                dto.dueDate,
    storageReference:       dto.storageReference,
    suggestedDepartment:    dto.suggestedDepartment,
    reportId:               dto.reportId,
  };
}

@Injectable({ providedIn: 'root' })
export class RadiologistDashboardService {

  /** In-memory cache — lives for the lifetime of the Angular app (single session). */
  private cache: CaseCard[] | null = null;

  constructor(private auth: AuthService) {}

  /** Returns cached data immediately if available, otherwise fetches from API. */
  getCachedCases(): CaseCard[] | null {
    return this.cache;
  }

  /** Fetches fresh data from the API, updates the cache, and returns the result. */
  async loadWorkload(): Promise<CaseCard[]> {
    const response = await fetch(`${this.auth.getApiBase()}/api/workload`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });
    await this.assertOk(response);
    const dtos: WorkloadDto[] = await response.json();
    this.cache = dtos.map(dtoToCaseCard);
    return this.cache;
  }

  async getById(id: string): Promise<CaseCard> {
    const response = await fetch(`${this.auth.getApiBase()}/api/workload/${id}`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });
    await this.assertOk(response);
    const dto: WorkloadDto = await response.json();
    return dtoToCaseCard(dto);
  }

  private async assertOk(response: Response): Promise<void> {
    if (response.ok) return;
    let message = `HTTP ${response.status}`;
    try {
      const problem = await response.json();
      message = problem?.detail ?? problem?.title ?? message;
    } catch { /* not JSON */ }
    throw new Error(message);
  }
}