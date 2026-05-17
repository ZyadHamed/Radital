// realtime.service.ts
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from '../../auth.service';

export interface EmergencyNotification {
  requestId:              string;
  patientName:            string;
  modality:               string;
  emergencyJustification: string;
  assignedAt:             string;
  deadlineUtc:            string;
  escalationRound:        number;
}

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  emergency$ = new Subject<EmergencyNotification>();

  private hub: signalR.HubConnection;

constructor(private auth: AuthService) {
  this.hub = new signalR.HubConnectionBuilder()
    .withUrl(`https://localhost:7168/hubs/radiologist`, {
      accessTokenFactory: () => this.auth.getToken() ?? ''
    })
    .withAutomaticReconnect()
    .build();

  console.log('[SignalR] Registering EmergencyAssigned handler');
  
  this.hub.on('EmergencyAssigned', (data: any) => {
    console.log('[SignalR] *** EmergencyAssigned FIRED ***', data);
    this.emergency$.next(data);
  });

  // Re-join group after reconnection — this is critical
  this.hub.onreconnected(async () => {
    console.log('[SignalR] Reconnected — rejoining group');
    const id = this.getIdFromToken();
    await this.hub.invoke('JoinRadiologistGroup', id);
  });
}

// In RealtimeService, replace the connect call approach entirely
async connect(radiologistId?: string): Promise<void> {
  const id = radiologistId || this.getIdFromToken();
  console.log('[SignalR] Joining group with radiologistId:', id);
  await this.hub.start();
  await this.hub.invoke('JoinRadiologistGroup', id);
  console.log('[SignalR] Joined group: radiologist-' + id);
}

private getIdFromToken(): string {
  const token = this.auth.getToken();
  if (!token) return '';
  try {
    // JWT payload is the middle part, base64 encoded
    const payload = JSON.parse(atob(token.split('.')[1]));
    console.log('[SignalR] Token payload:', payload); // see all claims
    return payload.sub ?? '';
  } catch {
    return '';
  }
}
}