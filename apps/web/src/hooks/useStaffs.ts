import { useQueryClient } from '@tanstack/react-query';
import {
  mockCreateSchedule,
  mockListSchedules,
  mockListStaffs,
  type MockScheduleInput,
} from '@/lib/mock/store';
import { staffKeys } from '@/lib/query-keys';
import { useApiMutation, useApiQuery } from './useApi';

export function useStaffs(activeOnly = true) {
  return useApiQuery({
    queryKey: staffKeys.list(),
    // TODO(api): queryFn: () => apiModules.listStaffs(activeOnly),
    queryFn: () => mockListStaffs(activeOnly),
    staleTime: 60_000,
  });
}

export function useSchedules(staffId: number | null, from?: string, to?: string) {
  return useApiQuery({
    queryKey: staffKeys.schedules(staffId ?? 0, from, to),
    // TODO(api): queryFn: () => apiModules.listSchedules(staffId as number, from, to),
    queryFn: () => mockListSchedules(staffId as number, from, to),
    enabled: staffId != null,
  });
}

export function useCreateSchedule() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({ staffId, input }: { staffId: number; input: MockScheduleInput }) =>
      mockCreateSchedule(staffId, input),
    {
      // TODO(api): mutationFn: ({ staffId, input }) => apiModules.createSchedule(staffId, input),
      onSuccess: () => queryClient.invalidateQueries({ queryKey: staffKeys.all }),
    },
  );
}
