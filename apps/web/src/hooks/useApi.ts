'use client';

// Axios wrapper as custom hooks: every data hook in the app goes through
// useApiQuery / useApiMutation so fetching, caching and error mapping stay
// in one place. Domain logic lives in the domain hooks (useServices, ...);
// components only consume { data, isLoading, error }.

import {
  useMutation,
  useQuery,
  type UseMutationOptions,
  type UseQueryOptions,
} from '@tanstack/react-query';
import { toast } from 'sonner';
import { getErrorMessage } from '@/lib/error-messages';

export function useApiQuery<T>(
  options: UseQueryOptions<T> & { queryKey: readonly unknown[] },
) {
  return useQuery({
    staleTime: 30_000,
    retry: 1,
    refetchOnWindowFocus: false,
    placeholderData: (prev) => prev,
    ...options,
  });
}

interface ApiMutationOptions<TData, TVariables>
  extends Omit<UseMutationOptions<TData, unknown, TVariables>, 'mutationFn'> {
  silent?: boolean;
}

export function useApiMutation<TData, TVariables>(
  mutationFn: (variables: TVariables) => Promise<TData>,
  options: ApiMutationOptions<TData, TVariables> = {},
) {
  const { silent, ...rest } = options;
  return useMutation<TData, unknown, TVariables>({
    mutationFn,
    ...rest,
    onError: (...args) => {
      if (!silent) toast.error(getErrorMessage(args[0]));
      rest.onError?.(...args);
    },
  });
}
