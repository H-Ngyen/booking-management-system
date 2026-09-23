'use client';

import { useRouter } from 'next/navigation';
import { useEffect, type ReactNode } from 'react';
import { useAuthMe } from '@/hooks/useAuthMe';
import { Spinner } from '@/components/ui';

export function AdminGuard({ children }: { children: ReactNode }) {
  const router = useRouter();
  const { data, isLoading } = useAuthMe();

  useEffect(() => {
    if (!isLoading && data?.role !== 'Admin') router.replace('/services');
  }, [isLoading, data, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <Spinner label="Đang kiểm tra quyền…" />
      </div>
    );
  }
  if (data?.role !== 'Admin') return null;
  return <>{children}</>;
}
