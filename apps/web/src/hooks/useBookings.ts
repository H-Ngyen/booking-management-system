import { useQueryClient } from '@tanstack/react-query';
import * as apiModules from '@/lib/api/modules';
import type { BookingListParams } from '@/lib/api/modules';
import { authKeys, bookingKeys } from '@/lib/query-keys';
import type { AuthUser, BookingStatus } from '@/lib/types';
import { useApiMutation, useApiQuery } from './useApi';

function readSession(queryClient: ReturnType<typeof useQueryClient>): AuthUser {
  // Mutations gate on the cached session so unauthenticated calls never fire;
  // the 401 interceptor redirects to /login as a backstop.
  const me = queryClient.getQueryData<AuthUser>(authKeys.me);
  if (!me) throw new Error('Phiên đã hết hạn, vui lòng đăng nhập lại.');
  return me;
}

export function useMyBookings(params: BookingListParams = {}) {
  const queryClient = useQueryClient();
  const me = queryClient.getQueryData<AuthUser>(authKeys.me);
  return useApiQuery({
    queryKey: bookingKeys.myList(params),
    queryFn: () => apiModules.listMyBookings(params),
    enabled: me != null,
  });
}

export function useBookings(params: BookingListParams = {}) {
  return useApiQuery({
    queryKey: bookingKeys.list(params),
    queryFn: () => apiModules.listBookings(params),
  });
}

export function useAvailableSlots(serviceId: number | null, staffId: number | null, date: string) {
  const enabled = serviceId != null && staffId != null && date.length > 0;
  return useApiQuery({
    queryKey: bookingKeys.slots(serviceId ?? 0, staffId ?? 0, date),
    queryFn: () =>
      apiModules.getAvailableSlots({ serviceId: serviceId as number, staffId: staffId as number, date }),
    enabled,
  });
}

export function useCreateBooking() {
  const queryClient = useQueryClient();
  return useApiMutation(
    (input: { serviceId: number; staffId: number; startTime: string; customerNote?: string }) => {
      // Backend derives the customer from the JWT; no customerId needed.
      readSession(queryClient);
      return apiModules.createBooking(input);
    },
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: bookingKeys.all }),
    },
  );
}

export function useUpdateBookingStatus() {
  const queryClient = useQueryClient();
  return useApiMutation(({ id, status }: { id: number; status: BookingStatus }) => {
    readSession(queryClient);
    return apiModules.updateBookingStatus(id, status);
  }, {
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bookingKeys.all }),
  });
}

export function useCancelBooking() {
  const queryClient = useQueryClient();
  return useApiMutation(({ id, reason }: { id: number; reason: string }) => {
    readSession(queryClient);
    return apiModules.cancelBooking(id, reason);
  }, {
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bookingKeys.all }),
  });
}
