// ─────────────────────────────────────────────────────────────────────────────
// imaging-request.component.ts  –  Updated component wired to ImagingRequestService
// ─────────────────────────────────────────────────────────────────────────────
import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }  from '@angular/forms';
import { Router, ActivatedRoute }       from '@angular/router';

import { ImagingRequestService, ImagingRequestFormModel } from './imaging-request.service';
import { ReportingRequestResponseDto } from '../../models';
import { RadiologistPickerComponent } from './radiologist-picker/radiologist-picker.component';


@Component({
  selector:    'app-imaging-request',
  standalone:  true,
  imports:     [CommonModule, FormsModule, RadiologistPickerComponent],
  templateUrl: './imaging-request.component.html',
  styleUrls:   ['./imaging-request.component.css'],
})
export class ImagingRequestComponent {

  // ── Form model ────────────────────────────────────────────────────────
  // All fields the HTML template binds to via [(ngModel)].
  // The extra fields (beyond the original model) feed the API DTO.
  model: ImagingRequestFormModel = {
    patientName:            '',
    age:                    null,   
   patientId:              '',     
   notes:                  '', 
    gender:                 '',
    patientDateOfBirth:     '',
    patientPhoneNumber:     '',
    patientAddress:         '',
    patientMedicalHistory:  '',
    patientNotes:           '',
    scanType:               'CT',
    imageUrl:               '',
    suggestedDepartment:    '',
    priority:               'Routine',
    dueDate:                '',
    assignedRadiologistId:  '',
    isEmergency:            false,
    emergencyJustification: '',
  };

  // ── UI state ──────────────────────────────────────────────────────────
  isLoading:       boolean = false;
  errorMessage:    string  = '';
  successMessage:  string  = '';
  isEditMode:      boolean = false;
  editingId:       string | null = null;
  lastCreated:     ReportingRequestResponseDto | null = null;
  previewImageUrl: string = '';

  /** Today's date as YYYY-MM-DD — used as max for the DOB picker */
  get today(): string {
    return new Date().toISOString().split('T')[0];
  }

  constructor(
    private imagingRequestService: ImagingRequestService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  async ngOnInit(): Promise<void> {
    const editId = this.route.snapshot.queryParams['edit'];
    if (editId) {
      this.isEditMode = true;
      this.editingId = editId;
      this.loadEditingData(editId);
    }
  }

  async loadEditingData(id: string): Promise<void> {
    this.isLoading = true;
    try {
      this.model = await this.imagingRequestService.getById(id);
      if (this.model.imageUrl) {
        this.previewImageUrl = this.model.imageUrl;
      }
    } catch (err) {
      this.errorMessage = 'Failed to load request data for editing.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  // ── Navigation ───────────────────────────────────────────────────────

  goToDashboard(): void {
    this.router.navigate(['/technician-dashboard']);
  }

  // ── Submit ────────────────────────────────────────────────────────────

  async submitRequest(): Promise<void> {
    this.isLoading      = true;
    this.errorMessage   = '';
    this.successMessage = '';

    try {
      if (this.isEditMode && this.editingId) {
        await this.imagingRequestService.update(this.editingId, this.model);
        this.successMessage = `Request updated successfully.`;
      } else {
        this.lastCreated    = await this.imagingRequestService.submit(this.model);
        this.successMessage = `Request submitted successfully. ID: ${this.lastCreated.id}`;
      }
      // After success, maybe go back to dashboard after a delay?
      setTimeout(() => this.goToDashboard(), 2000);
    } catch (err: unknown) {
      this.errorMessage = err instanceof Error
        ? err.message
        : 'Submission failed. Please try again.';
    } finally {
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  // ── Delete (local clear — no DELETE endpoint in swagger) ───────────────

  deleteRequest(): void {
    if (!confirm('Are you sure you want to discard this request?')) return;
    this.reset();
    this.successMessage = '';
    this.errorMessage   = '';
    this.lastCreated    = null;
  }

  // ── Reset form ────────────────────────────────────────────────────────

  reset(): void {
      this.previewImageUrl = '';
    this.model = {
      patientName:            '',
      age:                    null,   
      patientId:              '',     
      notes:                  '',     
      gender:                 '',
      patientDateOfBirth:     '',
      patientPhoneNumber:     '',
      patientAddress:         '',
      patientMedicalHistory:  '',
      patientNotes:           '',
      scanType:               'CT',
      imageUrl:               '',
      suggestedDepartment:    '',
      priority:               'Routine',
      dueDate:                '',
      assignedRadiologistId:  '',
      isEmergency:            false,
      emergencyJustification: '',
    };
  }

  // ── Load image preview ────────────────────────────────────────────────

  loadImage(): void {
    if (!this.model.imageUrl) {
      this.errorMessage = 'Please enter an image URL first.';
      return;
    }
    this.previewImageUrl = this.model.imageUrl;
  }
}