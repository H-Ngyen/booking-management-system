import type { ReactNode } from 'react';
import { Button } from './button';
import { Skeleton } from './skeleton';

export function Spinner({ label = 'Đang tải…' }: { label?: string }) {
  return (
    <span className="inline-flex items-center gap-2 text-sm text-ink-3" role="status">
      <span className="size-4 animate-spin rounded-full border-2 border-line border-t-primary" aria-hidden />
      {label}
    </span>
  );
}

export function TableSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="space-y-2" aria-label="Đang tải">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} className="h-12 w-full" />
      ))}
    </div>
  );
}

export function EmptyState({ title, hint, action }: { title: string; hint?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-line-2 bg-surface px-6 py-12 text-center">
      <p className="text-sm font-semibold text-ink">{title}</p>
      {hint && <p className="max-w-md text-sm text-ink-3">{hint}</p>}
      {action}
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-2xl border border-line bg-destructive-bg px-6 py-10 text-center">
      <p className="text-sm font-semibold text-destructive">{message}</p>
      {onRetry && (
        <Button variant="outline" onClick={onRetry}>
          Thử lại
        </Button>
      )}
    </div>
  );
}
