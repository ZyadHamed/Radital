// ─────────────────────────────────────────────────────────────────────────────
// login.service.ts  –  Auth API calls for the Login component
//
// Endpoints used:
//   POST /api/Auth/login     → LoginRequestDto  → LoginResponseDto (token)
//   POST /api/Auth/register  → RegisterStaffMemberDto → 200 OK
//
// API base is selected by employee ID prefix:
//   TEC*** → https://localhost:7026  → /technician-dashboard
//   RAD*** → https://localhost:7168  → /radiologist-dashboard
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';
import { Router }     from '@angular/router';

import { AuthService }            from '../../auth.service';
import {
  LoginRequestDto,
  LoginResponseDto,
  RegisterStaffMemberDto,
  RolesEnum,
} from '../../models';

const API_BASES: Record<string, string> = {
  TEC: 'https://localhost:7026',
  RAD: 'https://localhost:7168',
};

const DEFAULT_API_BASE = 'https://localhost:7026';

@Injectable({ providedIn: 'root' })
export class LoginService {

  constructor(
    private auth:   AuthService,
    private router: Router,
  ) {}

  // ── login ─────────────────────────────────────────────────────────────────

  /**
   * Authenticates the user.
   * The API base is resolved from the first 3 characters of the employee ID.
   * On success the JWT is stored in localStorage via AuthService and the user
   * is redirected to the correct dashboard based on their ID prefix.
   *
   * @throws Error with a user-facing message on failure
   */
  async login(employeeId: string, password: string): Promise<void> {
    const apiBase = this.resolveApiBase(employeeId);
    const body: LoginRequestDto = { loginId: employeeId, password };

    const response = await fetch(`${apiBase}/api/Auth/login`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify(body),
    });

    if (!response.ok) {
      const detail = await this.extractErrorMessage(response);
      throw new Error(detail ?? 'Invalid credentials. Please try again.');
    }

    const data: LoginResponseDto = await response.json();

    // Persist token + user info
    this.auth.saveSession(data, apiBase);

    // Route based on ID prefix (source of truth) rather than role from token
    this.redirectByEmployeeId(employeeId);
  }

  // ── register (Request System Access) ──────────────────────────────────────

  /**
   * Registers a new staff member account.
   * Uses the same API base resolution logic as login.
   *
   * @throws Error with a user-facing message on failure
   */
  async register(dto: RegisterStaffMemberDto): Promise<void> {
    const apiBase = this.resolveApiBase(dto.loginId);

    const response = await fetch(`${apiBase}/api/Auth/register`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify(dto),
    });

    if (!response.ok) {
      const detail = await this.extractErrorMessage(response);
      throw new Error(detail ?? 'Registration failed. Please contact IT support.');
    }
    // Server returns 200 OK with no body — nothing to parse
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  /**
   * Resolves the correct API base URL from the first 3 characters of the
   * employee ID (case-insensitive). Falls back to the default base if the
   * prefix is unrecognised.
   */
  private resolveApiBase(employeeId: string): string {
    const prefix = employeeId?.substring(0, 3).toUpperCase();
    return API_BASES[prefix] ?? DEFAULT_API_BASE;
  }

  private redirectByEmployeeId(employeeId: string): void {
    const prefix = employeeId?.substring(0, 3).toUpperCase();

    switch (prefix) {
      case 'TEC':
        this.router.navigate(['/technician-dashboard']);
        break;
      case 'RAD':
        this.router.navigate(['/radiologist-dashboard']);
        break;
      default:
        this.router.navigate(['/dashboard']);
    }
  }

  private redirectByRole(role: RolesEnum): void {
    switch (role) {
      case RolesEnum.Technician:
        this.router.navigate(['/technician-dashboard']);
        break;
      case RolesEnum.Radiologist:
        this.router.navigate(['/radiologist-dashboard']);
        break;
      default:
        this.router.navigate(['/dashboard']);
    }
  }

  private async extractErrorMessage(response: Response): Promise<string | null> {
    try {
      const problem = await response.json();
      return problem?.detail ?? problem?.title ?? null;
    } catch {
      return null;
    }
  }
}