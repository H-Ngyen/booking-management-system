import { apiClient } from '@/lib/client';
import type { Booking, BookingStatus, Paginated } from '@/lib/types';

export interface BookingListParams {
  date?: string; // YYYY-MM-DD
  status?: BookingStatus;
  page?: number;
  pageSize?: number;
}

export interface CreateBookingInput {
  serviceId: number;
  staffId: number;
  startTime: string; // ISO — backend computes EndTime
  customerNote?: string;
}

export interface AvailableSlotsParams {
  serviceId: number;
  staffId: number;
  date: string; // YYYY-MM-DD
}

// Backend PagedResult<T> serializes as { items, totalPages, totalItemsCount,
// itemsFrom, itemsTo } and paginates on `pageNumber` — map both sides here
// so pages keep using the Paginated<T> shape.
interface PagedResultDto<T> {
  items: T[];
  totalPages: number;
  totalItemsCount: number;
}

function toPaginated<T>(dto: PagedResultDto<T>, page: number, pageSize: number): Paginated<T> {
  return {
    items: dto.items ?? [],
    page,
    pageSize,
    total: dto.totalItemsCount ?? 0,
    totalPages: dto.totalPages ?? 0,
  };
}

function listParams(params: BookingListParams = {}) {
  const { page = 1, pageSize = 20, ...rest } = params;
  return { params: { ...rest, pageNumber: page, pageSize }, page, pageSize };
}

export const listMyBookings = (params: BookingListParams = {}) => {
  const { params: query, page, pageSize } = listParams(params);
  return apiClient
    .get<PagedResultDto<Booking>>('/v1/bookings/my-bookings', { params: query })
    .then((r) => toPaginated(r.data, page, pageSize));
};

export const listBookings = (params: BookingListParams = {}) => {
  const { params: query, page, pageSize } = listParams(params);
  return apiClient
    .get<PagedResultDto<Booking>>('/v1/bookings', { params: query })
    .then((r) => toPaginated(r.data, page, pageSize));
};

export const getAvailableSlots = (params: AvailableSlotsParams) =>
  apiClient.get<string[]>('/v1/bookings/available-slots', { params }).then((r) => r.data);

export const createBooking = (input: CreateBookingInput) =>
  apiClient.post<Booking>('/v1/bookings', input).then((r) => r.data);

export const updateBookingStatus = (id: number, status: BookingStatus) =>
  apiClient.patch<Booking>(`/v1/bookings/${id}/status`, { status }).then((r) => r.data);

export const cancelBooking = (id: number, reason: string) =>
  apiClient.post<Booking>(`/v1/bookings/${id}/cancel`, { reason }).then((r) => r.data);
