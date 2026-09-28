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

export interface ServiceUpdateInput {
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive: boolean;
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

export const listServices = (params: ServiceListParams = {}) => {
  const { q, page = 1, pageSize = 20 } = params;
  return apiClient
    .get<PagedResultDto<ServiceItem>>('/v1/services', {
      params: { searchPhrase: q || undefined, pageNumber: page, pageSize },
    })
    .then((r) => toPaginated(r.data, page, pageSize));
};

// Backend create has no IsActive (new services start active).
export const createService = (input: ServiceInput) =>
  apiClient
    .post<number>('/v1/services', {
      name: input.name,
      description: input.description,
      durationMinutes: input.durationMinutes,
      price: input.price,
    })
    .then((r) => r.data);

// Backend update requires the FULL object (all fields validated).
export const updateService = (id: number, input: ServiceUpdateInput) =>
  apiClient.put<void>(`/v1/services/${id}`, input).then((r) => r.data);
