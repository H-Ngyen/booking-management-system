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

export const listMyBookings = (params: BookingListParams = {}) =>
  apiClient.get<Paginated<Booking>>('/bookings/my-bookings', { params }).then((r) => r.data);

export const listBookings = (params: BookingListParams = {}) =>
  apiClient.get<Paginated<Booking>>('/bookings', { params }).then((r) => r.data);

export const getAvailableSlots = (params: AvailableSlotsParams) =>
  apiClient.get<string[]>('/bookings/available-slots', { params }).then((r) => r.data);

export const createBooking = (input: CreateBookingInput) =>
  apiClient.post<Booking>('/bookings', input).then((r) => r.data);

export const updateBookingStatus = (id: number, status: BookingStatus) =>
  apiClient.patch<Booking>(`/bookings/${id}/status`, { status }).then((r) => r.data);

export const cancelBooking = (id: number, reason: string) =>
  apiClient.post<Booking>(`/bookings/${id}/cancel`, { reason }).then((r) => r.data);
