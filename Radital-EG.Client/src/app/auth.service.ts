// ─────────────────────────────────────────────────────────────────────────────
// auth.service.ts  –  Token storage & retrieval used by every HTTP service
// ─────────────────────────────────────────────────────────────────────────────
import { Injectable } from '@angular/core';
import { LoginResponseDto } from './models';

const TOKEN_KEY = 'radital_token';
const USER_KEY  = 'radital_user';
const API_BASE_KEY = 'radital_api_base';

@Injectable({ providedIn: 'root' })
export class AuthService {

  // ── Storage helpers ─────────────────────────────────────────────────────

  saveSession(response: LoginResponseDto, apiBase: string): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY,  JSON.stringify(response));
    localStorage.setItem(API_BASE_KEY, apiBase);         // ← NEW
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getApiBase(): string {
  return localStorage.getItem(API_BASE_KEY) ?? 'https://localhost:7026';
}

getRadiologistApiBase(): string {
  return 'https://localhost:7168'; // RadiologistAPI port
}

  getUser(): LoginResponseDto | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as LoginResponseDto) : null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  getUserId(): string {
    return this.getUser()?.staffId ?? '';
  }

  // ── Header helper (used by every service) ───────────────────────────────

  /** Returns the Authorization header object ready to spread into fetch options. */
  authHeaders(): Record<string, string> {
    const token = this.getToken();
    return token
      ? { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }
      : { 'Content-Type': 'application/json' };
  }
}