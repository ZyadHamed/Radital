// ─────────────────────────────────────────────────────────────────────────────
// reporting-request.service.ts  –  ReportingRequests API for ALL components
//
// Endpoints used:
//   POST   /api/ReportingRequests       → create a new request
//   GET    /api/ReportingRequests       → get all requests (technician list)
//   GET    /api/ReportingRequests/{id}  → get single request
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';

import { AuthService } from '../../auth.service'; 
import {
  CreateReportingRequestDto,
  ReportingRequestResponseDto,
} from '../../models'; 
const API_BASE = 'http://localhost:5000';

@Injectable({ providedIn: 'root' })
export class ReportingRequestService {

  constructor(private auth: AuthService) {}

  // ── GET all ──────────────────────────────────────────────────────────────

  /**
   * Fetches every reporting request the current user is authorised to see.
   * Used by the technician dashboard to populate the table.
   */
  async getAll(): Promise<ReportingRequestResponseDto[]> {
  const response = await fetch(`${this.auth.getApiBase()}/api/ReportingRequests`, {
    method:  'GET',
    headers: this.auth.authHeaders(),
  });

    await this.assertOk(response);
    return response.json() as Promise<ReportingRequestResponseDto[]>;
  }

  // ── GET by id ─────────────────────────────────────────────────────────────

  /**
   * Fetches a single request by UUID.
   * Useful when the technician expands a row and needs fresh data.
   */
  async getById(id: string): Promise<ReportingRequestResponseDto> {
  const response = await fetch(`${this.auth.getApiBase()}/api/ReportingRequests/${id}`, {
    method:  'GET',
    headers: this.auth.authHeaders(),
  });

    await this.assertOk(response);
    return response.json() as Promise<ReportingRequestResponseDto>;
  }

  // ── POST (create) ────────────────────────────────────────────────────────

  /**
   * Submits a new imaging request.
   * Used by ImagingRequestComponent on "Submit Request".
   * Returns the created resource (201 Created).
   */
  async create(dto: CreateReportingRequestDto): Promise<ReportingRequestResponseDto> {
  const response = await fetch(`${this.auth.getApiBase()}/api/ReportingRequests`, {
    method:  'POST',
    headers: this.auth.authHeaders(),
    body:    JSON.stringify(dto),
  });

    await this.assertOk(response, [201]);
    return response.json() as Promise<ReportingRequestResponseDto>;
  }

  // ── PUT (update) ────────────────────────────────────────────────────────
  
  /**
   * Updates an existing imaging request.
   */
  async update(id: string, dto: CreateReportingRequestDto): Promise<ReportingRequestResponseDto> {
    const response = await fetch(`${this.auth.getApiBase()}/api/ReportingRequests/${id}`, {
      method:  'PUT',
      headers: this.auth.authHeaders(),
      body:    JSON.stringify(dto),
    });

    await this.assertOk(response, [200, 204]);
    // The API might return the updated object or just 204.
    if (response.status === 204) {
      // If 204, we don't have a body to parse.
      return {} as ReportingRequestResponseDto; 
    }
    return response.json() as Promise<ReportingRequestResponseDto>;
  }

async getReportPdf(id: string): Promise<void> {
  const newTab = window.open('', '_blank');
  if (!newTab) {
    throw new Error('Popup blocked. Please allow popups for this site.');
  }

  // Show a loading page inside the tab while the PDF fetches
  newTab.document.write(`
    <!DOCTYPE html>
    <html>
      <head>
        <title>Loading Report...</title>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 100vh;
            background: #0f172a;
            color: #94a3b8;
            font-family: 'Segoe UI', sans-serif;
            gap: 20px;
          }
          .spinner {
            width: 48px; height: 48px;
            border: 3px solid #1e293b;
            border-top-color: #3b82f6;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
          }
          @keyframes spin { to { transform: rotate(360deg); } }
          .label { font-size: 13px; letter-spacing: 0.1em; }
          .sub   { font-size: 11px; color: #475569; }
        </style>
      </head>
      <body>
        <div class="spinner"></div>
        <span class="label">LOADING REPORT</span>
        <span class="sub">Fetching PDF from server...</span>
      </body>
    </html>
  `);
  newTab.document.close();

  try {
    const response = await fetch(`${this.auth.getApiBase()}/api/reportingrequests/${id}/report/pdf`, {
      method:  'GET',
      headers: this.auth.authHeaders(),
    });

    await this.assertOk(response);
    const blob = await response.blob();
    const url  = URL.createObjectURL(blob);

    // Replace the loading page with the actual PDF
    newTab.location.href = url;
    setTimeout(() => URL.revokeObjectURL(url), 10000);

  } catch (err) {
    // Show an error page in the tab instead of leaving it blank
    newTab.document.open();
    newTab.document.write(`
      <!DOCTYPE html>
      <html>
        <head>
          <title>Report Unavailable</title>
          <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body {
              display: flex;
              flex-direction: column;
              align-items: center;
              justify-content: center;
              height: 100vh;
              background: #0f172a;
              color: #94a3b8;
              font-family: 'Segoe UI', sans-serif;
              gap: 16px;
            }
            .icon  { font-size: 32px; }
            .label { font-size: 13px; letter-spacing: 0.1em; color: #ef4444; }
            .sub   { font-size: 11px; color: #475569; }
            button {
              margin-top: 8px;
              padding: 8px 20px;
              background: transparent;
              border: 1px solid #334155;
              color: #94a3b8;
              border-radius: 6px;
              cursor: pointer;
              font-size: 12px;
              letter-spacing: 0.05em;
            }
            button:hover { background: #1e293b; }
          </style>
        </head>
        <body>
          <span class="icon">⚠</span>
          <span class="label">REPORT UNAVAILABLE</span>
          <span class="sub">No report has been filed for this request yet.</span>
          <button onclick="window.close()">CLOSE TAB</button>
        </body>
      </html>
    `);
    newTab.document.close();
    throw err;
  }
}

  // ── Helpers ───────────────────────────────────────────────────────────────

  /**
   * Throws a descriptive Error when the HTTP status is not in `allowed`.
   * Tries to parse ProblemDetails from the body for a user-friendly message.
   */
  private async assertOk(
    response: Response,
    allowed: number[] = [200],
  ): Promise<void> {
    if (allowed.includes(response.status)) return;

    let message = `HTTP ${response.status}`;
    try {
      const problem = await response.json();
      message = problem?.detail ?? problem?.title ?? message;
    } catch { /* body was not JSON */ }

    throw new Error(message);
  }
}