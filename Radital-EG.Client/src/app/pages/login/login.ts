// ─────────────────────────────────────────────────────────────────────────────
// login.ts  –  Login component wired to LoginService
// ─────────────────────────────────────────────────────────────────────────────
import { Component, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }  from '@angular/forms';
import { LoginService } from './login.service';

@Component({
  selector:    'app-login',
  standalone:  true,
  imports:     [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls:   ['./login.css'],
})
export class Login {
  @ViewChild('passwordInput') passwordInput!: ElementRef;

  employeeId:   string  = '';
  password:     string  = '';
  showPassword: boolean = false;
  idTouched:    boolean = false;

  isLoading:    boolean = false;
  errorMessage: string  = '';

  private wasIdValid = false;

  constructor(private loginService: LoginService) {}

  // ── Validation ─────────────────────────────────────────────────────────

  get isIdValid(): boolean {
    return /^(RAD|TEC)\d{7}$/i.test(this.employeeId);
  }

  get showIdError(): boolean {
    return this.idTouched && !this.isIdValid && this.employeeId.length > 0;
  }

  get isFormValid(): boolean {
    return this.isIdValid && this.password.length > 0 && !this.isLoading;
  }

  // ── Handlers ───────────────────────────────────────────────────────────

  onIdChange(value: string): void {
    this.idTouched  = true;
    this.employeeId = value.toUpperCase();
    this.errorMessage = '';

    const currentlyValid = this.isIdValid;
    if (currentlyValid && !this.wasIdValid) {
      setTimeout(() => this.passwordInput?.nativeElement?.focus(), 50);
    }
    this.wasIdValid = currentlyValid;
  }

  togglePassword(): void {
    if (this.isIdValid) this.showPassword = !this.showPassword;
  }

  async onSignIn(): Promise<void> {
    if (!this.isFormValid) return;

    this.isLoading    = true;
    this.errorMessage = '';

    try {
      // LoginService stores the token and navigates on success
      await this.loginService.login(this.employeeId, this.password);
    } catch (err: unknown) {
      this.errorMessage = err instanceof Error ? err.message : 'An unexpected error occurred.';
    } finally {
      this.isLoading = false;
    }
  }

  showForgotModal: boolean = false;

  onForgotPassword(): void {
    this.showForgotModal = true;
  }

  closeForgotModal(): void {
    this.showForgotModal = false;
  }
}