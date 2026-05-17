import { Injectable } from '@angular/core';

import { ReportingRequestService }  from '../imaging-request/reporting-request.service';
import {
  ReportingRequestResponseDto,
  ReportingRequestStatusEnum,
  ImageModalitiesEnum,
} from '../../models';

import { Request, RequestStatus, ProgressStep } from './technician-dashboard';

// ── Mapping helpers ────────────────────────────────────────────────────────

const STATUS_MAP: Record<ReportingRequestStatusEnum, RequestStatus> = {
  [ReportingRequestStatusEnum.Pending]:   'PENDING',
  [ReportingRequestStatusEnum.InReview]:  'IN REVIEW',
  [ReportingRequestStatusEnum.Completed]: 'COMPLETED',
  [ReportingRequestStatusEnum.Escalated]: 'ESCALATED',
};

const PROGRESS_MAP: Record<ReportingRequestStatusEnum, ProgressStep> = {
  [ReportingRequestStatusEnum.Pending]:   'SUBMITTED',
  [ReportingRequestStatusEnum.InReview]:  'PROCESSING',
  [ReportingRequestStatusEnum.Completed]: 'COMPLETED',
  [ReportingRequestStatusEnum.Escalated]: 'FINALIZING',
};

const MODALITY_LABEL: Record<ImageModalitiesEnum, string> = {
  [ImageModalitiesEnum.CT]:         'CT',
  [ImageModalitiesEnum.MRI]:        'MRI',
  [ImageModalitiesEnum.XRay]:       'X-Ray',
  [ImageModalitiesEnum.Ultrasound]: 'Ultrasound',
};

// Priority level → human label (adjust if your enum differs)
const PRIORITY_LABEL: Record<number, string> = {
  0: 'Routine',
  1: 'Urgent',
  2: 'Critical',
};

function toInitials(name: string | null): string {
  if (!name) return '??';
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map(w => w[0].toUpperCase())
    .join('');
}

function formatDateTime(isoString: string | null | undefined): string {
  if (!isoString) return '—';
  const date  = new Date(isoString);
  const today = new Date();
  const isToday =
    date.getFullYear() === today.getFullYear() &&
    date.getMonth()    === today.getMonth()    &&
    date.getDate()     === today.getDate();

  const timeStr = date.toLocaleTimeString('en-US', {
    hour:   '2-digit',
    minute: '2-digit',
  });
  return isToday
    ? `Today, ${timeStr}`
    : `${date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}, ${timeStr}`;
}

function dtoToRequest(dto: ReportingRequestResponseDto): Request {
  const modality   = MODALITY_LABEL[dto.imageModality] ?? 'Unknown';
  const department = dto.suggestedDepartment ? ` ${dto.suggestedDepartment}` : '';

  return {
    // ── Existing fields ──────────────────────────────────────────────────
    id:              `#${dto.id.substring(0, 7).toUpperCase()}`,
    uuid:            dto.id,
    patientInitials: toInitials(dto.patientName),
    patientName:     dto.patientName ?? 'Unknown Patient',
    modality:        `${modality}${department}`.trim(),
    submissionTime:    formatDateTime(dto.submissionTime),
    submissionTimeRaw: dto.submissionTime ?? null,
    status:          STATUS_MAP[dto.status]   ?? 'PENDING',
    progress:        PROGRESS_MAP[dto.status] ?? 'SUBMITTED',
    assignedTo:      dto.assignedRadiologistName ?? undefined,
    expanded:        false,

    // ── New fields from API ──────────────────────────────────────────────
    statusLabel:     dto.statusLabel ?? STATUS_MAP[dto.status] ?? 'Pending',
    isEmergency:     dto.isEmergency ?? false,
    priority:        dto.priority ?? 0,
    priorityLabel:   PRIORITY_LABEL[dto.priority] ?? 'Routine',
    dueDate:         formatDateTime(dto.dueDate),
    dueDateRaw:      dto.dueDate ?? null,   // raw ISO — useful for sorting/overdue checks

    // ── Extended patient info ────────────────────────────────────────────
    patientDateOfBirth: dto.patientDateOfBirth ?? null,
    patientGender:      dto.patientGender ?? null,
    patientAddress:     dto.patientAddress ?? null,
    patientMedicalHistory: dto.patientMedicalHistory ?? null,
    patientNotes:       dto.patientNotes ?? null,

    storageReference: dto.storageReference ?? null,
  };
}

// ── Service ─────────────────────────────────────────────────────────────────

export interface DashboardStats {
  avgTurnaround:  string;
  avgTrend:       string;
  urgentPending:  string;
  overdueCount:   string;   // NEW — requests past their dueDate
  reportsToday:   string;
  reportsTarget:  string;
  reportsPercent: number;
}

@Injectable({ providedIn: 'root' })
export class TechnicianDashboardService {

  constructor(private requestApi: ReportingRequestService) {}

  async loadRequests(): Promise<Request[]> {
    const dtos = await this.requestApi.getAll();
    return dtos.map(dtoToRequest);
  }

  async refreshRow(requests: Request[], uuid: string): Promise<void> {
    const dto = await this.requestApi.getById(uuid);
    const idx = requests.findIndex(r => r.uuid === uuid);
    if (idx === -1) return;

    const updated    = dtoToRequest(dto);
    updated.expanded = requests[idx].expanded;   // preserve local UI state
    requests[idx]    = updated;
  }

  deriveStats(requests: Request[]): DashboardStats {
    const now = new Date();

    // Urgent = PENDING or ESCALATED status, OR flagged as emergency
    const urgentCount = requests.filter(
      r => r.status === 'PENDING' || r.status === 'ESCALATED' || r.isEmergency
    ).length;

    // Overdue = has a dueDate that has already passed and isn't completed
    const overdueCount = requests.filter(r =>
      r.dueDateRaw &&
      new Date(r.dueDateRaw) < now &&
      r.status !== 'COMPLETED'
    ).length;

    const completedToday = requests.filter(
      r => r.status === 'COMPLETED' && r.submissionTime.startsWith('Today')
    ).length;

    return {
      avgTurnaround:  '— m — s',   // placeholder until backend exposes a stats endpoint
      avgTrend:       'N/A',
      urgentPending:  String(urgentCount),
      overdueCount:   String(overdueCount),
      reportsToday:   String(completedToday),
      reportsTarget:  '15',
      reportsPercent: Math.round((completedToday / 15) * 100),
    };
  }

  async openReportPdf(uuid: string): Promise<void> {
  await this.requestApi.getReportPdf(uuid);
}
}