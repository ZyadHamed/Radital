// ─────────────────────────────────────────────────────────────────────────────
// reporting.service.ts
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';
import { AuthService } from '../../auth.service';
import { WorkloadDto } from '../radiologist-dashboard/radiologist-dashboard.service';

export interface CreateReportDto {
  reportingRequestId: string;
  clinicalHistory:    string;
  technique:          string;
  findings:           string;
  impression:         string;
  recommendation:     string;
}

export interface ReportResponseDto {
  id:                 string;
  reportingRequestId: string;
  clinicalHistory:    string;
  technique:          string;
  findings:           string;
  impression:         string;
  recommendation:     string;
}

@Injectable({ providedIn: 'root' })
export class ReportingService {

  constructor(private auth: AuthService) {}

  async getCase(id: string): Promise<WorkloadDto> {
    const response = await fetch(`${this.auth.getApiBase()}/api/workload/${id}`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });
    await this.assertOk(response);
    return response.json();
  }

  async getAllReports(): Promise<ReportResponseDto[]> {
    const response = await fetch(`${this.auth.getApiBase()}/api/reports`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });
    await this.assertOk(response);
    return response.json();
  }

  async getReport(id: string): Promise<ReportResponseDto> {
    const response = await fetch(`${this.auth.getApiBase()}/api/reports/${id}`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });
    await this.assertOk(response);
    return response.json();
  }

  async submitReport(dto: CreateReportDto): Promise<ReportResponseDto> {
    const response = await fetch(`${this.auth.getApiBase()}/api/reports`, {
      method:  'POST',
      headers: this.auth.authHeaders(),
      body:    JSON.stringify(dto),
    });
    await this.assertOk(response, [200, 201]);
    return response.json();
  }

  async downloadPdf(reportId: string): Promise<void> {
    const response = await fetch(`${this.auth.getApiBase()}/api/reports/${reportId}/pdf`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });
    await this.assertOk(response);
    const blob = await response.blob();
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `report-${reportId}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  }

  private async assertOk(response: Response, allowed: number[] = [200]): Promise<void> {
    if (allowed.includes(response.status)) return;
    let message = `HTTP ${response.status}`;
    try {
      const problem = await response.json();
      message = problem?.detail ?? problem?.title ?? message;
    } catch { /* not JSON */ }
    throw new Error(message);
  }
}