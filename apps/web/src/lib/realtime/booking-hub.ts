import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getServerOrigin } from '@/lib/api/base-url';
import { getAccessToken } from '@/lib/api/token-store';

export interface BookingChangedEvent {
  bookingId: number;
  customerId: number;
  changeType: 'Created' | 'StatusChanged' | 'Cancelled' | 'AutoCancelled';
}

export const BOOKING_CHANGED_METHOD = 'BookingChanged';

let connection: HubConnection | null = null;

// Singleton per session: the accessTokenFactory reads the current token
// lazily, but the *connection* authenticates once at start —
// useBookingRealtime restarts it whenever the logged-in user changes.
export function getBookingHub(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(`${getServerOrigin()}/hubs/bookings`, {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return connection;
}

export async function stopBookingHub(): Promise<void> {
  if (!connection) return;
  try {
    await connection.stop();
  } catch {
    // Idempotent: stopping an already-stopped connection is fine.
  }
  connection = null;
}
