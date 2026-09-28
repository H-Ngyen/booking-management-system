import { useQueryClient } from '@tanstack/react-query';
import * as apiModules from '@/lib/api/modules';
import { authKeys } from '@/lib/query-keys';
import { useApiMutation, useApiQuery } from './useApi';

export function useAuthMe(enabled = true) {
  return useApiQuery({
    queryKey: authKeys.me,
    queryFn: () => apiModules.getMe(),
    retry: false,
    staleTime: 60_000,
    enabled,
  });
}

export function useLogin() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({ userName, password }: { userName: string; password: string }) =>
      apiModules.login(userName, password),
    {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: authKeys.me }),
    },
  );
}

export function useLogout() {
  const queryClient = useQueryClient();
  return useApiMutation(() => Promise.resolve(apiModules.logout()), {
    // Token is client-side only (stateless JWT); logout = drop it + session cache.
    onSuccess: () => queryClient.removeQueries({ queryKey: authKeys.me }),
    silent: true,
  });
}
