import { useQueryClient } from '@tanstack/react-query';
import * as apiModules from '@/lib/api/modules';
import type { StaffListParams } from '@/lib/api/modules';
import { staffKeys } from '@/lib/query-keys';
import { useApiMutation, useApiQuery } from './useApi';

export function useStaffs(activeOnly = true, params: StaffListParams = {}) {
  return useApiQuery({
    // Backend has no activeOnly param (it filters inactive server-side for
    // non-admins); filter client-side so the customer dropdown stays clean
    // while admin pages can still see locked staff.
    queryKey: staffKeys.list({ ...params, activeOnly }),
    queryFn: () =>
      apiModules
        .listStaffs({ search: params.search, page: params.page, pageSize: params.pageSize ?? 100 })
        .then((paged) => ({
          ...paged,
          items: activeOnly ? paged.items.filter((s) => s.isActive) : paged.items,
        })),
    staleTime: 60_000,
  });
}

export function useSchedules(staffId: number | null, from?: string, to?: string) {
  return useApiQuery({
    queryKey: staffKeys.schedules(staffId ?? 0, from, to),
    queryFn: () => apiModules.listSchedules(staffId as number, from, to),
    enabled: staffId != null,
  });
}

export function useCreateStaff() {
  const queryClient = useQueryClient();
  return useApiMutation((input: { fullName: string; email: string }) => apiModules.createStaff(input), {
    onSuccess: () => queryClient.invalidateQueries({ queryKey: staffKeys.all }),
  });
}

export function useUpdateStaff() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({ id, input }: { id: number; input: { fullName: string; email: string; isActive: boolean } }) =>
      apiModules.updateStaff(id, input),
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: staffKeys.all }),
    },
  );
}

export function useCreateSchedule() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({
      staffId,
      input,
    }: {
      staffId: number;
      input: { workDate: string; startTime: string; endTime: string };
    }) => apiModules.createSchedule(staffId, input),
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: staffKeys.all }),
    },
  );
}

export function useDeleteSchedule() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({ staffId, scheduleId }: { staffId: number; scheduleId: number }) =>
      apiModules.deleteSchedule(staffId, scheduleId),
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: staffKeys.all }),
    },
  );
}
