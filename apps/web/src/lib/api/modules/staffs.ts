import { apiClient } from '@/lib/client';
import type { Paginated, Staff, WorkSchedule } from '@/lib/types';

export interface StaffListParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface StaffInput {
  fullName: string;
  email: string;
}

export interface StaffUpdateInput extends StaffInput {
  isActive: boolean;
}

export interface ScheduleInput {
  workDate: string; // YYYY-MM-DD
  startTime: string; // HH:mm
  endTime: string; // HH:mm
}

// Backend PagedResult<T> serializes as { items, totalPages, totalItemsCount,
// itemsFrom, itemsTo } and paginates on `pageNumber` (+ `searchPhrase`);
// map both sides here so pages keep using the Paginated<T> shape.
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

export const listStaffs = (params: StaffListParams = {}) => {
  const { search, page = 1, pageSize = 100 } = params;
  return apiClient
    .get<PagedResultDto<Staff>>('/v1/staffs', {
      params: { searchPhrase: search || undefined, pageNumber: page, pageSize },
    })
    .then((r) => toPaginated(r.data, page, pageSize));
};

export const createStaff = (input: StaffInput) =>
  apiClient.post<number>('/v1/staffs', input).then((r) => r.data);

export const updateStaff = (id: number, input: StaffUpdateInput) =>
  apiClient.put<void>(`/v1/staffs/${id}`, input).then((r) => r.data);

export const listSchedules = (staffId: number, from?: string, to?: string) =>
  apiClient
    .get<WorkSchedule[]>(`/v1/staffs/${staffId}/schedules`, { params: { from, to } })
    .then((r) => r.data);

export const createSchedule = (staffId: number, input: ScheduleInput) =>
  apiClient.post<WorkSchedule>(`/v1/staffs/${staffId}/schedules`, input).then((r) => r.data);

export const deleteSchedule = (staffId: number, scheduleId: number) =>
  apiClient.delete<void>(`/v1/staffs/${staffId}/schedules/${scheduleId}`).then((r) => r.data);
