import { useQueryClient } from '@tanstack/react-query';
import { mockGetMe, mockLogin, mockLogout } from '@/lib/mock/store';
import { authKeys } from '@/lib/query-keys';
import { useApiMutation, useApiQuery } from './useApi';

export function useAuthMe(enabled = true) {
  return useApiQuery({
    queryKey: authKeys.me,
    queryFn: mockGetMe,
    retry: false,
    staleTime: 60_000,
    enabled,
    // TODO(api): queryFn: () => apiModules.getMe(),
  });
}

export function useLogin() {
  const queryClient = useQueryClient();
  return useApiMutation(
    ({ userName, password }: { userName: string; password: string }) =>
      mockLogin(userName, password),
    {
      // TODO(api): mutationFn: (input) => apiModules.login(input.userName, input.password),
      onSuccess: () => queryClient.invalidateQueries({ queryKey: authKeys.me }),
    },
  );
}

export function useLogout() {
  const queryClient = useQueryClient();
  return useApiMutation(() => mockLogout(), {
    // TODO(api): also clear server session if the backend uses one.
    onSuccess: () => queryClient.removeQueries({ queryKey: authKeys.me }),
    silent: true,
  });
}
