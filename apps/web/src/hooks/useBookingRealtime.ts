'use client';

import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { HubConnectionState, type HubConnection } from '@microsoft/signalr';
import { toast } from 'sonner';
import { bookingKeys } from '@/lib/query-keys';
import { getBookingHub, stopBookingHub, type BookingChangedEvent } from '@/lib/realtime/booking-hub';
import { useAuthMe } from './useAuth';

const CHANGE_TOASTS: Record<BookingChangedEvent['changeType'], string> = {
  Created: 'Có booking mới.',
  StatusChanged: 'Một booking vừa đổi trạng thái.',
  Cancelled: 'Một booking vừa bị hủy.',
  AutoCancelled: 'Hệ thống vừa tự hủy booking quá hạn.',
};

// One shared in-flight start: React StrictMode mounts, unmounts and remounts
// effects in dev — without this, the first cleanup would stop() a connection
// that is still negotiating and log
// "The connection was stopped during negotiation".
let startPromise: Promise<void> | null = null;
let startedForUserId: number | null = null;

function ensureStarted(userId: number): Promise<void> {
  const connection = getBookingHub();
  if (connection.state === HubConnectionState.Connected && startedForUserId === userId) {
    return Promise.resolve();
  }
  if (!startPromise) {
    startPromise = connection
      .start()
      .then(() => {
        startedForUserId = userId;
      })
      .finally(() => {
        startPromise = null;
      });
  }
  return startPromise;
}

// Opens one SignalR connection per logged-in user. Any BookingChanged event
// (own actions, admin actions on another tab, Hangfire auto-cancel)
// invalidates all booking queries so the UI refetches fresh data.
export function useBookingRealtime(enabled = true) {
  const queryClient = useQueryClient();
  const { data: me } = useAuthMe();
  const userId = me?.id;

  useEffect(() => {
    if (!enabled || userId == null) return;
    let cancelled = false;
    let activeConnection: HubConnection | null = null;

    const onChanged = (event: BookingChangedEvent) => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      toast.info(CHANGE_TOASTS[event.changeType] ?? 'Booking vừa được cập nhật.');
    };

    const boot = async () => {
      // A *different* user took over this browser session: drop the old
      // authenticated connection so the new one negotiates with the new token.
      if (startedForUserId !== null && startedForUserId !== userId) {
        await stopBookingHub();
        startedForUserId = null;
      }
      if (cancelled) return;
      activeConnection = getBookingHub();
      activeConnection.on('BookingChanged', onChanged);
      // The API may still be starting (dotnet watch rebuild): retry with
      // backoff while this effect is alive, then give up quietly.
      for (let attempt = 0; attempt < 10 && !cancelled; attempt++) {
        try {
          await ensureStarted(userId);
          return;
        } catch {
          await new Promise((r) => setTimeout(r, 3000));
        }
      }
    };
    void boot();

    return () => {
      cancelled = true;
      // NOTE: intentionally no stop() here — a remount (StrictMode, navigation)
      // reuses the live connection. Teardown happens only on user change/logout.
      activeConnection?.off('BookingChanged', onChanged);
    };
  }, [enabled, userId, queryClient]);

  // Logout (or session expiry): tear the connection down for real.
  useEffect(() => {
    if (enabled && userId == null && startedForUserId !== null) {
      startedForUserId = null;
      void stopBookingHub();
    }
  }, [enabled, userId]);
}
