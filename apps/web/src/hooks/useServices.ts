import { useQueryClient } from '@tanstack/react-query';
import {
  mockCreateService,
  mockListServices,
  mockUpdateService,
  type MockServiceInput,
  type MockServiceListParams,
} from '@/lib/mock/store';
import { serviceKeys } from '@/lib/query-keys';
import { useApiMutation, useApiQuery } from './useApi';

export function useServices(params: MockServiceListParams = {}) {
  return useApiQuery({
    queryKey: serviceKeys.list(params),
    // TODO(api): queryFn: () => apiModules.listServices(params),
    queryFn: () => mockListServices(params),
  });
}

export function useCreateService() {
  const queryClient = useQueryClient();
  return useApiMutation((input: MockServiceInput) => mockCreateService(input), {
    // TODO(api): mutationFn: (input) => apiModules.createService(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: serviceKeys.all }),
  });
}

export function useUpdateService() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({ id, patch }: { id: number; patch: Partial<MockServiceInput> }) =>
      mockUpdateService(id, patch),
    {
      // TODO(api): mutationFn: ({ id, patch }) => apiModules.updateService(id, patch),
      onSuccess: () => queryClient.invalidateQueries({ queryKey: serviceKeys.all }),
    },
  );
}
