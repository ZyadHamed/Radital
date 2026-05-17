import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule }  from '@angular/common';
import { FormsModule }   from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';

import { ReportingService }           from './reporting.service';
import { WorkloadDto }                from '../radiologist-dashboard/radiologist-dashboard.service';

// ─────────────────────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────────────────────

interface Modality {
  id:          string;
  series:      string;
  description: string;
  imageUrl:    string;
}

export interface ReportForm {
  clinicalHistory: string;
  technique:       string;
  findings:        string;
  impression:      string;
  recommendation:  string;
}

type ToolName =
  | 'select'
  | 'zoom'
  | 'pan'
  | 'wl'
  | 'ruler'
  | 'angle'
  | 'roi-ellipse'
  | 'roi-rect'
  | 'roi-free'
  | 'cobb'
  | 'probe'
  | 'arrow'
  | 'label';

type WLPreset = 'lung' | 'bone' | 'soft' | 'brain' | 'abdomen';

// WL preset definitions  [windowWidth, windowLevel]
const WL_PRESETS: Record<WLPreset, [number, number]> = {
  lung:    [-600,  1500],
  bone:    [400,   1800],
  soft:    [40,    400 ],
  brain:   [40,    80  ],
  abdomen: [60,    400 ],
};

// ─────────────────────────────────────────────────────────────
// ViewerState — encapsulates all viewer tool logic
// ─────────────────────────────────────────────────────────────

export class ViewerState {

  // ── Active tool & preset ──────────────────────────────────
  activeTool:   ToolName = 'select';
  activePreset: WLPreset | null = null;

  // ── Transform state ───────────────────────────────────────
  zoom:      number  = 1;       // scale multiplier
  panX:      number  = 0;       // translate X in px
  panY:      number  = 0;       // translate Y in px
  rotation:  number  = 0;       // degrees
  flipH:     boolean = false;
  flipV:     boolean = false;
  inverted:  boolean = false;

  // ── Window / Level ────────────────────────────────────────
  windowLevel: number = 40;
  windowWidth: number = 400;

  // ── UI visibility flags ───────────────────────────────────
  overlayVisible:    boolean = true;
  annotationsVisible:boolean = true;
  referenceLines:    boolean = false;

  // ── Measurement readouts ──────────────────────────────────
  measurements: string[] = [];

  // ── Internal drag tracking ────────────────────────────────
  private isDragging = false;
  private dragStart  = { x: 0, y: 0 };
  private panStart   = { x: 0, y: 0 };
  private wlStart    = { wl: 0, ww: 0 };

  // ─────────────────────────────────────────────────────────
  // Computed styles
  // ─────────────────────────────────────────────────────────

  get zoomPercent(): number {
    return Math.round(this.zoom * 100);
  }

  get transformStyle(): string {
    const scaleX = this.flipH ? -this.zoom : this.zoom;
    const scaleY = this.flipV ? -this.zoom : this.zoom;
    return `translate(${this.panX}px, ${this.panY}px) rotate(${this.rotation}deg) scale(${scaleX}, ${scaleY})`;
  }

  get filterStyle(): string {
    if (this.inverted) return 'invert(1)';
    return 'none';
  }

  /** CSS cursor class to apply on the image-container */
  get cursorClass(): string {
    const map: Record<ToolName, string> = {
      select:      'cursor-default',
      zoom:        'cursor-zoom-in',
      pan:         'cursor-move',
      wl:          'cursor-crosshair',
      ruler:       'cursor-crosshair',
      angle:       'cursor-crosshair',
      'roi-ellipse':'cursor-crosshair',
      'roi-rect':  'cursor-crosshair',
      'roi-free':  'cursor-crosshair',
      cobb:        'cursor-crosshair',
      probe:       'cursor-cell',
      arrow:       'cursor-crosshair',
      label:       'cursor-crosshair',
    };
    return map[this.activeTool] ?? 'cursor-default';
  }

  // ─────────────────────────────────────────────────────────
  // Tool selection
  // ─────────────────────────────────────────────────────────

  setTool(tool: ToolName): void {
    this.activeTool = tool;
  }

  // ─────────────────────────────────────────────────────────
  // Zoom
  // ─────────────────────────────────────────────────────────

  zoomIn(): void {
    this.zoom = Math.min(this.zoom * 1.2, 8);
  }

  zoomOut(): void {
    this.zoom = Math.max(this.zoom / 1.2, 0.1);
  }

  fitToView(): void {
    this.zoom   = 1;
    this.panX   = 0;
    this.panY   = 0;
    this.rotation = 0;
    this.flipH  = false;
    this.flipV  = false;
  }

  resetView(): void {
    this.fitToView();
    this.inverted    = false;
    this.activePreset = null;
    this.windowLevel = 40;
    this.windowWidth = 400;
    this.measurements = [];
  }

  // ─────────────────────────────────────────────────────────
  // Rotation / flip
  // ─────────────────────────────────────────────────────────

  rotateCW(): void  { this.rotation = (this.rotation + 90) % 360; }
  rotateCCW(): void { this.rotation = (this.rotation - 90 + 360) % 360; }
  flipHorizontal(): void { this.flipH = !this.flipH; }
  flipVertical():   void { this.flipV = !this.flipV; }
  toggleInvert():   void { this.inverted = !this.inverted; }

  // ─────────────────────────────────────────────────────────
  // WL presets
  // ─────────────────────────────────────────────────────────

  applyPreset(preset: WLPreset): void {
    if (this.activePreset === preset) {
      this.activePreset = null;
      return;
    }
    this.activePreset  = preset;
    this.activeTool    = 'wl';
    const [level, width] = WL_PRESETS[preset];
    this.windowLevel   = level;
    this.windowWidth   = width;
    // NOTE: In a real DICOM viewer (e.g. Cornerstone.js), apply W/L to the
    // viewport here. For an <img> fallback we encode it into a CSS filter.
  }

  // ─────────────────────────────────────────────────────────
  // Annotations
  // ─────────────────────────────────────────────────────────

  clearAnnotations(): void {
    this.measurements = [];
  }

  toggleAnnotations(): void {
    this.annotationsVisible = !this.annotationsVisible;
  }

  // ─────────────────────────────────────────────────────────
  // Overlay / reference lines
  // ─────────────────────────────────────────────────────────

  toggleOverlay():        void { this.overlayVisible  = !this.overlayVisible; }
  toggleReferenceLines(): void { this.referenceLines  = !this.referenceLines; }

  // ─────────────────────────────────────────────────────────
  // Fullscreen
  // ─────────────────────────────────────────────────────────

  toggleFullscreen(): void {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen?.();
    } else {
      document.exitFullscreen?.();
    }
  }

  // ─────────────────────────────────────────────────────────
  // Series navigation (stub — wire to modality array externally)
  // ─────────────────────────────────────────────────────────

  previousSeries(): void { /* delegate to component */ }
  nextSeries():     void { /* delegate to component */ }

  // ─────────────────────────────────────────────────────────
  // Mouse event handlers
  // ─────────────────────────────────────────────────────────

  onMouseDown(event: MouseEvent): void {
    this.isDragging = true;
    this.dragStart  = { x: event.clientX, y: event.clientY };

    if (this.activeTool === 'pan') {
      this.panStart = { x: this.panX, y: this.panY };
    }
    if (this.activeTool === 'wl') {
      this.wlStart = { wl: this.windowLevel, ww: this.windowWidth };
    }
  }

  onMouseMove(event: MouseEvent): void {
    if (!this.isDragging) return;

    const dx = event.clientX - this.dragStart.x;
    const dy = event.clientY - this.dragStart.y;

    if (this.activeTool === 'pan') {
      this.panX = this.panStart.x + dx;
      this.panY = this.panStart.y + dy;
      return;
    }

    if (this.activeTool === 'wl') {
      // Horizontal drag → window width; vertical drag → window level
      this.windowWidth = Math.max(1, this.wlStart.ww + dx * 4);
      this.windowLevel = this.wlStart.wl + dy * 2;
      return;
    }

    if (this.activeTool === 'zoom') {
      // Vertical drag to zoom
      const factor = 1 + dy * 0.005;
      this.zoom = Math.min(Math.max(this.zoom * factor, 0.1), 8);
      this.dragStart = { x: event.clientX, y: event.clientY };
      return;
    }
  }

  onMouseUp(_event: MouseEvent): void {
    if (this.isDragging && this.activeTool === 'ruler') {
      // Stub: in a real viewer calculate pixel distance and push a measurement
      const dx = _event.clientX - this.dragStart.x;
      const dy = _event.clientY - this.dragStart.y;
      const px = Math.round(Math.sqrt(dx * dx + dy * dy));
      this.measurements.push(`Ruler: ~${px} px`);
    }
    this.isDragging = false;
  }

  onWheel(event: WheelEvent): void {
    event.preventDefault();
    const delta = event.deltaY > 0 ? 0.9 : 1.1;
    this.zoom = Math.min(Math.max(this.zoom * delta, 0.1), 8);
  }
}

// ─────────────────────────────────────────────────────────────
// Component
// ─────────────────────────────────────────────────────────────

@Component({
  selector:    'app-reporting',
  standalone:  true,
  imports:     [CommonModule, FormsModule],
  templateUrl: './reporting.html',
  styleUrls:   ['./reporting.css'],
})
export class ReportingComponent implements OnInit {

  // ── UI state ────────────────────────────────────────────
  bannerVisible: boolean = true;
  draftSaved:    boolean = false;
  isLoading:     boolean = true;
  isSubmitting:  boolean = false;
  errorMessage:  string  = '';

  // ── Case data ───────────────────────────────────────────
  caseUuid:         string       = '';
  reportId:         string | null = null;
  currentCase:      WorkloadDto  | null = null;
  selectedModality: Modality     | null = null;
  modalities:       Modality[]   = [];

  // ── Viewer state ────────────────────────────────────────
  viewer: ViewerState = new ViewerState();

  // ── Report form ─────────────────────────────────────────
  report: ReportForm = {
    clinicalHistory: '',
    technique:       '',
    findings:        '',
    impression:      '',
    recommendation:  '',
  };

  constructor(
    private router:   Router,
    private route:    ActivatedRoute,
    private service:  ReportingService,
    private cdr:      ChangeDetectorRef,
  ) {}

  async ngOnInit(): Promise<void> {
    this.caseUuid = this.route.snapshot.queryParams['id'] ?? '';

    if (!this.caseUuid) {
      this.errorMessage = 'No case ID provided. Please open a case from the dashboard.';
      this.isLoading    = false;
      this.cdr.detectChanges();
      return;
    }

    await this.loadCase();
    this.wireViewerNavigation();
  }

  async loadCase(): Promise<void> {
    this.isLoading    = true;
    this.errorMessage = '';
    try {
      this.currentCase = await this.service.getCase(this.caseUuid);
      this.report.clinicalHistory = this.currentCase.patientMedicalHistory ?? '';

      this.modalities = [
        {
          id:          'm1',
          series:      'Ser 1',
          description: this.currentCase.suggestedDepartment ?? 'Primary Study',
          imageUrl:    this.currentCase.storageReference,
        },
      ];
      this.selectedModality = this.modalities[0];

    } catch (err: unknown) {
      this.errorMessage = err instanceof Error
        ? err.message
        : 'Failed to load case. Please go back and try again.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  /** Wire viewer navigation stubs to the modalities array */
  private wireViewerNavigation(): void {
    this.viewer.previousSeries = () => {
      const idx = this.modalities.findIndex(m => m.id === this.selectedModality?.id);
      if (idx > 0) this.selectModality(this.modalities[idx - 1]);
    };
    this.viewer.nextSeries = () => {
      const idx = this.modalities.findIndex(m => m.id === this.selectedModality?.id);
      if (idx < this.modalities.length - 1) this.selectModality(this.modalities[idx + 1]);
    };
  }

  selectModality(modality: Modality): void {
    this.selectedModality = modality;
    this.viewer.fitToView();
  }

  confirmReceipt(): void {
    this.bannerVisible = false;
  }

  saveDraft(): void {
    this.draftSaved = true;
    setTimeout(() => { this.draftSaved = false; }, 2500);
    this.router.navigate(['/radiologist-dashboard'], { queryParams: { autosaved: 'true' } });
  }

  async submitReport(): Promise<void> {
    if (!this.report.findings.trim() || !this.report.impression.trim()) {
      alert('Please fill in at least Findings and Impression before finalizing.');
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    try {
      const created = await this.service.submitReport({
        reportingRequestId: this.caseUuid,
        clinicalHistory:    this.report.clinicalHistory,
        technique:          this.report.technique,
        findings:           this.report.findings,
        impression:         this.report.impression,
        recommendation:     this.report.recommendation,
      });

      this.reportId = created.id;
      this.router.navigate(['/radiologist-dashboard'], { queryParams: { finalized: 'true' } });

    } catch (err: unknown) {
      this.errorMessage = err instanceof Error
        ? err.message
        : 'Submission failed. Please try again.';
    } finally {
      this.isSubmitting = false;
      this.cdr.detectChanges();
    }
  }

  async downloadPdf(): Promise<void> {
    if (!this.reportId) return;
    try {
      await this.service.downloadPdf(this.reportId);
    } catch {
      this.errorMessage = 'Failed to download PDF.';
      this.cdr.detectChanges();
    }
  }

  goToCaseList(): void {
    this.router.navigate(['/radiologist-dashboard'], { queryParams: { autosaved: 'true' } });
  }
}