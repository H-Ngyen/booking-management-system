import { BOOKING_STATUS_LABELS, BOOKING_STATUS_STYLES } from '@/lib/status-colors';
import type { BookingStatus } from '@/lib/types';
import { cn } from '@/lib/cn';
import { Badge } from './badge';

export function StatusBadge({ status }: { status: BookingStatus }) {
  const styles = BOOKING_STATUS_STYLES[status];
  return (
    <Badge className={cn('border-transparent', styles.pill)}>
      <span className={cn('size-1.5 rounded-full', styles.dot)} aria-hidden />
      {BOOKING_STATUS_LABELS[status]}
    </Badge>
  );
}
