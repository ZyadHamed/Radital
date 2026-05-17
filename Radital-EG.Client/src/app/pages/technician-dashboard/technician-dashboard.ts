// ─────────────────────────────────────────────────────────────────────────────
// technician-dashboard.ts  –  Updated component wired to TechnicianDashboardService
// ─────────────────────────────────────────────────────────────────────────────
import { Component, OnInit, ChangeDetectorRef  } from '@angular/core';
import { CommonModule }      from '@angular/common';
import { FormsModule }       from '@angular/forms';
import { Router } from '@angular/router';
import { TechnicianDashboardService, DashboardStats } from './technician-dashboard.service';
import { AuthService } from '../../auth.service';

export type RequestStatus = 'PENDING' | 'IN REVIEW' | 'COMPLETED' | 'ESCALATED';
export type ProgressStep  = 'SUBMITTED' | 'PROCESSING' | 'FINALIZING' | 'COMPLETED';

export interface Request {
  id:               string;
  patientInitials:  string;
  patientName:      string;
  modality:         string;
  submissionTime:   string;
  status:           RequestStatus;
  expanded:         boolean;
  assignedTo?:      string;
  progress:         ProgressStep;
  storageReference: string | null;
  /** Real UUID used for detail-panel API calls — not displayed. */
  uuid?:            string;

  statusLabel:          string;
  isEmergency:          boolean;
  priority:             number;
  priorityLabel:        string;
  dueDate:              string;          // formatted for display
  dueDateRaw:           string | null;   // raw ISO for logic/sorting
  submissionTimeRaw:    string | null;   // raw ISO for date-range filtering

  patientDateOfBirth:   string | null;
  patientGender:        number | null;
  patientAddress:       string | null;
  patientMedicalHistory: string | null;
  patientNotes:         string | null;
}

@Component({
  selector:    'app-technician-dashboard',
  standalone:  true,
  imports:     [CommonModule, FormsModule],
  templateUrl: './technician-dashboard.html',
  styleUrls:   ['./technician-dashboard.css'],
})
export class TechnicianDashboardComponent implements OnInit {

  // ── UI state ──────────────────────────────────────────────────────────
  activeNav:    string = 'current-cases';
  searchQuery:  string = '';
  dateRange:    string = 'last24h';
  statusFilter: string = 'all';

  isLoading:    boolean = true;
  pdfLoadingId: string | null = null;  // tracks which row is loading the PDF
  showSettingsDropdown: boolean = false;
  openActionMenuId: string | null = null;

  errorMessage: string  = '';

  navItems = [
    { id: 'dashboard',      label: 'Dashboard',      icon: 'grid'  },
    { id: 'emergency',      label: 'Emergency',      icon: 'alert' },
    { id: 'reports',        label: 'Reports',        icon: 'file'  },
  ];

  progressSteps: ProgressStep[] = ['SUBMITTED', 'PROCESSING', 'FINALIZING', 'COMPLETED'];

  // ── Data ──────────────────────────────────────────────────────────────
  allRequests: Request[] = [];

  stats: DashboardStats = {
    avgTurnaround:  '— m — s',
    avgTrend:       'N/A',
    urgentPending:  '0',
    overdueCount:   '0',
    reportsToday:   '0',
    reportsTarget:  '15',
    reportsPercent: 0,
  };

  constructor(private dashboardService: TechnicianDashboardService, private cdr: ChangeDetectorRef, private router: Router, private authService: AuthService) {}

  // ── Sign out ──────────────────────────────────────────────────────

  signOut(): void {
    this.authService.clearSession();
    this.router.navigate(['/login']);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────

  async ngOnInit(): Promise<void> {
    await this.loadData();
  }

  toggleSettings(): void {
    this.showSettingsDropdown = !this.showSettingsDropdown;
  }

  async loadData(): Promise<void> {
    this.isLoading    = true;
    this.errorMessage = '';

    try {
      this.allRequests = await this.dashboardService.loadRequests();
      this.stats       = this.dashboardService.deriveStats(this.allRequests);
    } catch (err: unknown) {
      this.errorMessage = err instanceof Error
        ? err.message
        : 'Failed to load requests. Please refresh.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  // ── Filtering ─────────────────────────────────────────────────────────

  get filteredRequests(): Request[] {
    const now = new Date();
    const cutoffMs: Record<string, number> = {
      last24h:  24 * 60 * 60 * 1000,
      last7d:   7  * 24 * 60 * 60 * 1000,
      last30d:  30 * 24 * 60 * 60 * 1000,
    };

    return this.allRequests.filter(r => {
      // ── Date-range filter ────────────────────────────────────────────────
      const matchDate = (() => {
        const ms = cutoffMs[this.dateRange];
        if (!ms || !r.submissionTimeRaw) return true;   // unknown range → show all
        return (now.getTime() - new Date(r.submissionTimeRaw).getTime()) <= ms;
      })();

      // ── Status filter ────────────────────────────────────────────────────
      const matchStatus = this.statusFilter === 'all' ||
        r.status.toLowerCase().replace(' ', '') === this.statusFilter;

      // ── Search filter ────────────────────────────────────────────────────
      const matchSearch = !this.searchQuery ||
        r.id.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        r.patientName.toLowerCase().includes(this.searchQuery.toLowerCase());

      return matchDate && matchStatus && matchSearch;
    });
  }

  resetFilters(): void {
    this.dateRange    = 'last24h';
    this.statusFilter = 'all';
    this.searchQuery  = '';
  }

  // ── Row expand with optional API refresh ──────────────────────────────

  async toggleExpand(req: Request): Promise<void> {
    req.expanded = !req.expanded;

    // When expanding, fetch fresh detail data if we have the UUID
    if (req.expanded && req.uuid) {
      try {
        await this.dashboardService.refreshRow(this.allRequests, req.uuid);
      } catch {
        // Non-fatal: the row stays expanded with the data we already have
      }
    }
  }

  navigateToImagingRequest(): void {
  this.router.navigate(['/imaging-request']);
}

viewImages(req: Request, event: MouseEvent): void {
  event.stopPropagation();
  if (!req.storageReference) {
    this.errorMessage = 'No image available for this request.';
    this.cdr.detectChanges();
    return;
  }
  window.open(req.storageReference, '_blank');
}

async viewInterimReport(req: Request, event: MouseEvent): Promise<void> {
  event.stopPropagation();
  if (!req.uuid) return;

  this.pdfLoadingId = req.uuid;
  this.errorMessage = '';
  this.cdr.detectChanges();

  try {
    await this.dashboardService.openReportPdf(req.uuid);
  } catch {
    this.errorMessage = 'No report available yet for this request.';
  } finally {
    this.pdfLoadingId = null;
    this.cdr.detectChanges();
  }
}

toggleActionMenu(event: MouseEvent, reqId: string): void {
  event.stopPropagation();
  this.openActionMenuId = (this.openActionMenuId === reqId) ? null : reqId;
}

editRequest(req: Request, event: MouseEvent): void {
  event.stopPropagation();
  this.openActionMenuId = null;
  // Navigate to imaging-request page (can be extended to pass ID for editing)
  this.router.navigate(['/imaging-request'], { queryParams: { edit: req.id } });
}

deleteRequest(req: Request, event: MouseEvent): void {
  event.stopPropagation();
  this.openActionMenuId = null;
  if (confirm(`Are you sure you want to delete request ${req.id}?`)) {
    // Locally remove from the list so the user sees immediate feedback
    this.allRequests = this.allRequests.filter(r => r.id !== req.id);
    this.cdr.detectChanges();
  }
}

  // ── Progress helpers ──────────────────────────────────────────────────

  getStepIndex(step: ProgressStep): number {
    return this.progressSteps.indexOf(step);
  }

  isStepDone(req: Request, step: ProgressStep): boolean {
    return this.getStepIndex(req.progress) >= this.getStepIndex(step);
  }

  isStepActive(req: Request, step: ProgressStep): boolean {
    return req.progress === step;
  }
}