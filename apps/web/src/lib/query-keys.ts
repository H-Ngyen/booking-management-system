import type { BookingListParams } from '@/lib/api/modules/bookings';
import type { ServiceListParams } from '@/lib/api/modules/services';
import type { BookingStatus } from '@/lib/types';

export const authKeys = {
  me: ['auth', 'me'] as const,
};

export const serviceKeys = {
  all: ['services'] as const,
  list: (params?: ServiceListParams) => [...serviceKeys.all, 'list', params ?? {}] as const,
};

export const staffKeys = {
  all: ['staffs'] as const,
  list: () => [...staffKeys.all, 'list'] as const,
  schedules: (staffId: number, from?: string, to?: string) =>
    [...staffKeys.all, staffId, 'schedules', from, to] as const,
};

export const bookingKeys = {
  all: ['bookings'] as const,
  myList: (params?: BookingListParams) => [...bookingKeys.all, 'my', 'list', params ?? {}] as const,
  list: (params?: BookingListParams) => [...bookingKeys.all, 'list', params ?? {}] as const,
  slots: (serviceId: number, staffId: number, date: string) =>
    [...bookingKeys.all, 'slots', serviceId, staffId, date] as const,
};

export type { BookingStatus };
