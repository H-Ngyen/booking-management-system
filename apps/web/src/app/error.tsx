'use client';

import { useEffect } from 'react';
import { Button } from '@/components/ui';
import { getErrorMessage } from '@/lib/error-messages';

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="mx-auto flex min-h-dvh max-w-md flex-col items-center justify-center gap-3 p-6 text-center">
      <h1 className="text-xl font-bold text-ink">Đã xảy ra lỗi</h1>
      <p className="text-sm text-ink-3">{getErrorMessage(error)}</p>
      <Button onClick={reset}>Thử lại</Button>
    </div>
  );
}
