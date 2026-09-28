import { cn } from '@/lib/cn';

function Skeleton({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('animate-pulse rounded-lg bg-surface-2', className)} aria-hidden {...props} />;
}

export { Skeleton };
