// ─────────────────────────────────────────────────────────────────────────────
// radiologist-picker.component.ts
//
// Drop-in replacement for the plain "Assigned Radiologist ID" text input.
// Usage in imaging-request.component.html:
//
//   <app-radiologist-picker
//     [requestId]="lastCreatedRequestId"
//     [(selectedId)]="model.assignedRadiologistId">
//   </app-radiologist-picker>
//
// Because the match-score endpoint needs an existing request ID, the picker
// operates in two phases:
//   1. Before submit  → manual UUID input (fallback, same as before)
//   2. After submit   → fetch scores for the created request and show the
//                       smart picker (pass [requestId]="lastCreated?.id")
//
// For the common workflow (create request → pick radiologist → update), you
// can also pass a requestId from the edit flow.
// ─────────────────────────────────────────────────────────────────────────────
import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }  from '@angular/forms';
import { AuthService }  from '../../../auth.service';

export interface DoctorMatchScore {
  radiologistId:          string;
  radiologistName:        string;
  specialty:              string;
  currentQueueSize:       number;
  averageTurnaroundHours: number | null;
  specialtyScore:         number;
  queueScore:             number;
  turnaroundScore:        number;
  overallScore:           number;
}

@Component({
  selector:   'app-radiologist-picker',
  standalone: true,
  imports:    [CommonModule, FormsModule],
  templateUrl: './radiologist-picker.component.html',
  styleUrls:  ['./radiologist-picker.component.css'],
})
export class RadiologistPickerComponent implements OnChanges {
  /** Pass the UUID of the reporting request to load match scores for it. */
  @Input()  requestId:   string | null = null;
  /** Two-way binding: emits the chosen radiologist's UUID. */
  @Input()  selectedId:  string = '';
  @Output() selectedIdChange = new EventEmitter<string>();

  scores:       DoctorMatchScore[] = [];
  isLoading:    boolean = false;
  errorMessage: string  = '';
  /** Fallback manual input when no requestId is available yet. */
  manualId:     string  = '';
  showManual:   boolean = false;

  constructor(private auth: AuthService, private cdr: ChangeDetectorRef) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['requestId'] && this.requestId) {
      this.loadScores();
    }
    if (changes['selectedId']) {
      this.manualId = this.selectedId;
    }
  }

  // In radiologist-picker.component.ts
getInitials(name: string): string {
  return name.split(' ').filter(Boolean).slice(0, 2).map(w => w[0].toUpperCase()).join('');
}

  async loadScores(): Promise<void> {
    if (!this.requestId) return;
    this.isLoading    = true;
    this.errorMessage = '';
    try {
      const res = await fetch(
        `${this.auth.getRadiologistApiBase()}/api/workload/${this.requestId}/doctor-match-scores`,
        { headers: this.auth.authHeaders() }
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      this.scores = await res.json();
    } catch (e: any) {
      this.errorMessage = e?.message ?? 'Failed to load match scores.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  select(id: string): void {
    this.selectedId = id;
    this.selectedIdChange.emit(id);
    this.manualId = id;
  }

  onManualChange(): void {
    this.selectedId = this.manualId;
    this.selectedIdChange.emit(this.manualId);
  }

  isSelected(id: string): boolean {
    return this.selectedId === id;
  }

  scoreBar(value: number): number {
    return Math.max(0, Math.min(100, value));
  }

  scoreClass(value: number): string {
    if (value >= 70) return 'score-high';
    if (value >= 40) return 'score-mid';
    return 'score-low';
  }

  get topPick(): DoctorMatchScore | null {
    return this.scores[0] ?? null;
  }
}