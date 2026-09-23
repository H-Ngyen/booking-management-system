import { apiClient } from '@/lib/client';
import type { Paginated, ServiceItem } from '@/lib/types';

export interface ServiceListParams {
  q?: string;
  page?: number;
  pageSize?: number;
  activeOnly?: boolean;
}

export interface ServiceInput {
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive?: boolean;
}

export const listServices = (params: ServiceListParams = {}) =>
  apiClient
    .get<Paginated<ServiceItem>>('/services', { params })
    .then((r) => r.data);

export const createService = (input: ServiceInput) =>
  apiClient.post<ServiceItem>('/services', input).then((r) => r.data);

export const updateService = (id: number, patch: Partial<ServiceInput>) =>
  apiClient.put<ServiceItem>(`/services/${id}`, patch).then((r) => r.data);
