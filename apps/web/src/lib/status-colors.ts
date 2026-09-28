import type { BookingStatus } from './types';

// Binds booking status to the status-* tokens from docs/Design.md so a pill
// is identical on customer and admin screens.
export const BOOKING_STATUS_STYLES: Record<BookingStatus, { dot: string; pill: string }> = {
  Pending: { dot: 'bg-status-pending', pill: 'bg-status-pending-bg text-status-pending' },
  Confirmed: { dot: 'bg-status-confirmed', pill: 'bg-status-confirmed-bg text-status-confirmed' },
  Completed: { dot: 'bg-status-completed', pill: 'bg-status-completed-bg text-status-completed' },
  Cancelled: { dot: 'bg-status-cancelled', pill: 'bg-status-cancelled-bg text-status-cancelled' },
};

export const BOOKING_STATUS_LABELS: Record<BookingStatus, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Completed: 'Hoàn thành',
  Cancelled: 'Đã hủy',
};
