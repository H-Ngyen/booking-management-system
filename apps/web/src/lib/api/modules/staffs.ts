import { apiClient } from '@/lib/client';
import type { Staff, WorkSchedule } from '@/lib/types';

export interface ScheduleInput {
  workDate: string; // YYYY-MM-DD
  startTime: string; // HH:mm
  endTime: string; // HH:mm
}

export const listStaffs = (activeOnly = true) =>
  apiClient.get<Staff[]>('/staffs', { params: { activeOnly } }).then((r) => r.data);

export const listSchedules = (staffId: number, from?: string, to?: string) =>
  apiClient
    .get<WorkSchedule[]>(`/staffs/${staffId}/schedules`, { params: { from, to } })
    .then((r) => r.data);

export const createSchedule = (staffId: number, input: ScheduleInput) =>
  apiClient.post<WorkSchedule>(`/staffs/${staffId}/schedules`, input).then((r) => r.data);
