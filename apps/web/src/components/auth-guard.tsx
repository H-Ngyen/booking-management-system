'use client';

import { useRouter } from 'next/navigation';
import { useEffect, type ReactNode } from 'react';
import { useAuthMe } from '@/hooks/useAuthMe';
import { Spinner } from '@/components/ui';

export function AuthGuard({ children }: { children: ReactNode }) {
  const router = useRouter();
  const { data, isLoading, isError } = useAuthMe();

  useEffect(() => {
    if (!isLoading && (isError || !data)) router.replace('/login');
  }, [isLoading, isError, data, router]);

  if (isLoading || isError || !data) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <Spinner label="Đang kiểm tra phiên đăng nhập…" />
      </div>
    );
  }
  return <>{children}</>;
}
