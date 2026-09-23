import { ApiError } from './client';

const ERROR_MESSAGES: Record<string, string> = {
  BOOKING_CONFLICT: 'Khung giờ này vừa có người đặt. Vui lòng chọn khung giờ khác.',
  BOOKING_IN_PAST: 'Không thể đặt lịch trong quá khứ.',
  OUTSIDE_WORKING_HOURS: 'Khung giờ nằm ngoài giờ làm việc của nhân viên.',
  SERVICE_INACTIVE: 'Dịch vụ này hiện đang tạm khóa.',
  STAFF_INACTIVE: 'Nhân viên này hiện đang tạm nghỉ.',
  CANCEL_REASON_REQUIRED: 'Vui lòng nhập lý do hủy.',
  CANNOT_CANCEL_STARTED: 'Không thể hủy booking đã bắt đầu hoặc đã hoàn thành.',
  FORBIDDEN: 'Bạn không có quyền thực hiện thao tác này.',
  UNAUTHORIZED: 'Phiên đã hết hạn, vui lòng đăng nhập lại.',
  NOT_FOUND: 'Không tìm thấy dữ liệu.',
};

export function getErrorMessage(error: unknown, fallback = 'Đã xảy ra lỗi không mong muốn'): string {
  if (error instanceof ApiError) {
    // 409 from the booking overlap rule carries the server message when present.
    if (error.status === 409 && error.message) return error.message;
    if (error.code && ERROR_MESSAGES[error.code]) return ERROR_MESSAGES[error.code];
    if (error.message) return error.message;
  }
  if (error instanceof Error && error.message) return error.message;
  return fallback;
}
