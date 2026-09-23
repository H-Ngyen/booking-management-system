import { useQueryClient } from '@tanstack/react-query';
import { ApiError } from '@/lib/client';
import {
  mockAvailableSlots,
  mockCancelBooking,
  mockCreateBooking,
  mockListBookings,
  mockListMyBookings,
  mockUpdateBookingStatus,
  type MockBookingListParams,
} from '@/lib/mock/store';
import { authKeys, bookingKeys } from '@/lib/query-keys';
import type { AuthUser, BookingStatus } from '@/lib/types';
import { useApiMutation, useApiQuery } from './useApi';

function readSession(queryClient: ReturnType<typeof useQueryClient>): AuthUser {
  const me = queryClient.getQueryData<AuthUser>(authKeys.me);
  if (!me) throw new ApiError(401, 'Phiên đã hết hạn, vui lòng đăng nhập lại.', 'UNAUTHORIZED');
  return me;
}

export function useMyBookings(params: MockBookingListParams = {}) {
  const queryClient = useQueryClient();
  const me = queryClient.getQueryData<AuthUser>(authKeys.me);
  return useApiQuery({
    queryKey: bookingKeys.myList(params),
    // TODO(api): queryFn: () => apiModules.listMyBookings(params),
    queryFn: () => mockListMyBookings(me?.id ?? 0, params),
    enabled: me != null,
  });
}

export function useBookings(params: MockBookingListParams = {}) {
  return useApiQuery({
    queryKey: bookingKeys.list(params),
    // TODO(api): queryFn: () => apiModules.listBookings(params),
    queryFn: () => mockListBookings(params),
  });
}

export function useAvailableSlots(serviceId: number | null, staffId: number | null, date: string) {
  const enabled = serviceId != null && staffId != null && date.length > 0;
  return useApiQuery({
    queryKey: bookingKeys.slots(serviceId ?? 0, staffId ?? 0, date),
    // TODO(api): queryFn: () => apiModules.getAvailableSlots({ serviceId, staffId, date }),
    queryFn: () =>
      mockAvailableSlots(serviceId as number, staffId as number, date),
    enabled,
  });
}

export function useCreateBooking() {
  const queryClient = useQueryClient();
  return useApiMutation(
    (input: { serviceId: number; staffId: number; startTime: string; customerNote?: string }) => {
      const me = readSession(queryClient);
      // TODO(api): mutationFn: (input) => apiModules.createBooking(input),
      return mockCreateBooking({ ...input, customerId: me.id });
    },
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: bookingKeys.all }),
    },
  );
}

export function useUpdateBookingStatus() {
  const queryClient = useQueryClient();
  return useApiMutation(({ id, status }: { id: number; status: BookingStatus }) => {
    const me = readSession(queryClient);
    // TODO(api): mutationFn: ({ id, status }) => apiModules.updateBookingStatus(id, status),
    return mockUpdateBookingStatus(id, status, me.role);
  }, {
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bookingKeys.all }),
  });
}

export function useCancelBooking() {
  const queryClient = useQueryClient();
  return useApiMutation(({ id, reason }: { id: number; reason: string }) => {
    const me = readSession(queryClient);
    // TODO(api): mutationFn: ({ id, reason }) => apiModules.cancelBooking(id, reason),
    return mockCancelBooking(id, reason, me.id, me.role);
  }, {
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bookingKeys.all }),
  });
}
