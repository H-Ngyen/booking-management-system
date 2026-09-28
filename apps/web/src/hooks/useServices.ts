import { useQueryClient } from '@tanstack/react-query';
import * as apiModules from '@/lib/api/modules';
import type { ServiceListParams } from '@/lib/api/modules';
import { serviceKeys } from '@/lib/query-keys';
import { useApiMutation, useApiQuery } from './useApi';

export function useServices(params: ServiceListParams = {}) {
  const { activeOnly = true, ...listParams } = params;
  return useApiQuery({
    queryKey: serviceKeys.list(params),
    // Backend has no activeOnly param (it filters inactive server-side for
    // non-admins); filter client-side so customer views stay clean while
    // admin pages can still see locked services.
    queryFn: () =>
      apiModules.listServices(listParams).then((paged) => ({
        ...paged,
        items: activeOnly ? paged.items.filter((s) => s.isActive) : paged.items,
      })),
  });
}

export function useCreateService() {
  const queryClient = useQueryClient();
  return useApiMutation(
    (input: { name: string; description?: string | null; durationMinutes: number; price: number }) =>
      apiModules.createService(input),
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: serviceKeys.all }),
    },
  );
}

export function useUpdateService() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({
      id,
      input,
    }: {
      id: number;
      input: {
        name: string;
        description?: string | null;
        durationMinutes: number;
        price: number;
        isActive: boolean;
      };
    }) => apiModules.updateService(id, input),
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: serviceKeys.all }),
    },
  );
}
