import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule }  from '@angular/common';
import { FormsModule }   from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { RadiologistDashboardService } from './radiologist-dashboard.service';
import { AuthService } from '../../auth.service';
import { RealtimeService, EmergencyNotification } from './realtime.service';
import { Subscription } from 'rxjs';

export interface CaseCard {
  id:             number;
  uuid:           string;
  caseNumber:     string;
  patientId:      string;
  patientName:    string;
  modality:       'MRI' | 'CT Scan' | 'X-Ray';
  submissionTime: string;
  isStat:         boolean;
  action:         'OPEN CRITICAL STUDY' | 'REVIEW SERIES';

  // Extended from API
  priority:               number;
  isEmergency:            boolean;
  emergencyJustification: string;
  status:                 number;
  statusLabel:            string;
  dueDate:                string;
  storageReference:       string;
  suggestedDepartment:    string;
  reportId:               string;
}

@Component({
  selector:    'app-dashboard',
  standalone:  true,
  imports:     [CommonModule, FormsModule],
  templateUrl: './radiologist-dashboard.html',
  styleUrls:   ['./radiologist-dashboard.css'],
})
export class RadiologistDashboardComponent implements OnInit {
  activeTab:        'caselist' | 'worklist' = 'caselist';
  viewMode:         'grid' | 'list'         = 'list';
  searchQuery:      string  = '';
  showNotification: boolean = false;
  toastMessage:     string  = '';
  toastType:        'autosave' | 'success' | '' = '';
  showSettingsDropdown: boolean = false;

  isLoading:    boolean = true;
  errorMessage: string  = '';

  allCases: CaseCard[] = [];

  activeEmergency: EmergencyNotification | null = null;
  emergencySecondsLeft = 300;
  private emergencyTimer: any = null;
  private emergencySub?: Subscription;

  private readonly FILTERS_STORAGE_KEY = 'radiologist_dashboard_filters';

  filters = {
    modality: { MRI: true, CTScans: true, XRay: true },
    priority: { Urgent: true, Standard: true },
  };

  constructor(
    private route:     ActivatedRoute,
    private router:    Router,
    private service:   RadiologistDashboardService,
    private cdr:       ChangeDetectorRef,
    private authService: AuthService,
    private realtime:  RealtimeService,   // ← add this
  ) {}

  toggleSettings(): void {
    this.showSettingsDropdown = !this.showSettingsDropdown;
  }

  signOut(): void {
    this.authService.clearSession();
    this.router.navigate(['/login']);
  }

  private loadFiltersFromStorage(): void {
    try {
      const saved = localStorage.getItem(this.FILTERS_STORAGE_KEY);
      if (saved) {
        const parsed = JSON.parse(saved);
        this.filters = parsed;
      }
    } catch {
      // If parsing fails, keep defaults
    }
  }

  saveFilters(): void {
    try {
      localStorage.setItem(this.FILTERS_STORAGE_KEY, JSON.stringify(this.filters));
    } catch {
      // Storage unavailable — silently ignore
    }
  }

  async ngOnInit(): Promise<void> {
    this.loadFiltersFromStorage();
    // Handle toast query params
    this.route.queryParams.subscribe(params => {
      let message = '';
      let type: 'autosave' | 'success' | '' = '';
      if (params['autosaved'] === 'true')  { message = 'Report autosaved';              type = 'autosave'; }
      if (params['finalized'] === 'true')  { message = 'Report finalized & submitted';  type = 'success';  }
      if (message) {
        this.toastMessage = message;
        this.toastType    = type;
        this.router.navigate([], { queryParams: {}, replaceUrl: true });
        setTimeout(() => { this.toastMessage = ''; this.toastType = ''; }, 3500);
      }
    });

      // ── Connect SignalR FIRST before anything else ──
  await this.realtime.connect();
  this.emergencySub = this.realtime.emergency$.subscribe(notification => {
    this.activeEmergency  = notification;
    this.showNotification = true;
    this.startEmergencyCountdown(notification.deadlineUtc);
    this.service.loadWorkload()
      .then(fresh => { this.allCases = fresh; this.cdr.detectChanges(); });
    this.cdr.detectChanges();
  });

  // ── THEN load data ──
  this.route.queryParams.subscribe(params => { /* existing toast logic */ });
  await this.loadData();

  }


  private startEmergencyCountdown(deadlineUtc: string): void {
  clearInterval(this.emergencyTimer);
  this.emergencyTimer = setInterval(() => {
    this.emergencySecondsLeft = Math.max(
      0,
      Math.floor((new Date(deadlineUtc).getTime() - Date.now()) / 1000)
    );
    if (this.emergencySecondsLeft === 0) {
      clearInterval(this.emergencyTimer);
      this.showNotification = false;   // backend will escalate; just hide
      this.activeEmergency  = null;
    }
    this.cdr.detectChanges();
  }, 1000);
}

async acceptEmergency(): Promise<void> {
  if (!this.activeEmergency) return;
  const requestId = this.activeEmergency.requestId; // ← capture before nulling
  try {
    const response = await fetch(
      `${this.authService.getApiBase()}/api/workload/${requestId}/accept`,
      { method: 'POST', headers: this.authService.authHeaders() }
    );
    if (response.ok) {
      clearInterval(this.emergencyTimer);
      this.showNotification = false;
      this.activeEmergency  = null;
      this.allCases = await this.service.loadWorkload();
      this.cdr.detectChanges();
      this.router.navigate(['/reporting'], { queryParams: { id: requestId } });
    }
  } catch (err) {
    console.error('Failed to accept emergency', err);
  }
}

  async loadData(): Promise<void> {
    this.errorMessage = '';

    // Serve cached data immediately — no loading spinner on return visits
    const cached = this.service.getCachedCases();
    if (cached) {
      this.allCases  = cached;
      this.isLoading = false;
      this.cdr.detectChanges();
      // Silently refresh in the background to pick up any new cases
      this.service.loadWorkload()
        .then(fresh => { this.allCases = fresh; this.cdr.detectChanges(); })
        .catch(() => { /* background refresh failed — keep showing cached data */ });
      return;
    }

    // First load — show skeleton until data arrives
    this.isLoading = true;
    try {
      this.allCases = await this.service.loadWorkload();
    } catch (err: unknown) {
      this.errorMessage = err instanceof Error
        ? err.message
        : 'Failed to load workload. Please refresh.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  get statCount(): number {
    return this.allCases.filter(c => c.isStat).length;
  }

  get filteredCases(): CaseCard[] {
    return this.allCases
      .filter(c => {
        const modalityMatch =
          (c.modality === 'MRI'     && this.filters.modality.MRI)     ||
          (c.modality === 'CT Scan' && this.filters.modality.CTScans) ||
          (c.modality === 'X-Ray'   && this.filters.modality.XRay);
        const priorityMatch =
          (c.isStat  && this.filters.priority.Urgent)   ||
          (!c.isStat && this.filters.priority.Standard);
        const searchMatch = this.searchQuery
          ? c.caseNumber.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
            c.patientId.toLowerCase().includes(this.searchQuery.toLowerCase())
          : true;
        return modalityMatch && priorityMatch && searchMatch;
      })
      .sort((a, b) => (b.isStat ? 1 : 0) - (a.isStat ? 1 : 0));
  }

  getModalityIcon(modality: string): string {
    switch (modality) {
      case 'MRI':     return 'M';
      case 'CT Scan': return 'CT';
      case 'X-Ray':   return 'X';
      default:        return '?';
    }
  }

  acknowledgeNotification(): void {
  this.showNotification = false;
  this.activeEmergency  = null;
  clearInterval(this.emergencyTimer);
}

  openCase(c: CaseCard): void {
    this.router.navigate(['/reporting'], { queryParams: { id: c.uuid } });
  }

  goToWorklist(): void {
    this.router.navigate(['/reporting']);
  }

  ngOnDestroy(): void {
  this.emergencySub?.unsubscribe();
  clearInterval(this.emergencyTimer);
}
}