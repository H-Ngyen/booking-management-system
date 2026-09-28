import { ApiError } from './client';

const ERROR_MESSAGES: Record<string, string> = {
  BOOKING_CONFLICT: 'Khung giờ này vừa có người đặt. Vui lòng chọn khung giờ khác.',
  BOOKING_IN_PAST: 'Không thể đặt lịch trong quá khứ.',
  BOOKING_CODE_FAILED: 'Không thể tạo mã booking, vui lòng thử lại.',
  BOOKING_NOT_FOUND: 'Không tìm thấy booking.',
  OUTSIDE_WORKING_HOURS: 'Khung giờ nằm ngoài giờ làm việc của nhân viên.',
  SERVICE_INACTIVE: 'Dịch vụ này hiện đang tạm khóa.',
  SERVICE_NOT_FOUND: 'Không tìm thấy dịch vụ.',
  STAFF_INACTIVE: 'Nhân viên này hiện đang tạm nghỉ.',
  STAFF_NOT_FOUND: 'Không tìm thấy nhân viên.',
  STAFF_EMAIL_EXISTS: 'Email nhân viên đã tồn tại.',
  SCHEDULE_NOT_FOUND: 'Không tìm thấy ca làm việc.',
  SCHEDULE_OVERLAP: 'Ca làm việc bị trùng với ca đã có của nhân viên này.',
  SCHEDULE_HAS_BOOKINGS: 'Không thể xóa ca làm việc đã có booking.',
  USER_NOT_FOUND: 'Không tìm thấy người dùng',
  INVALID_CREDENTIALS: 'Tên người dùng hoặc mật khẩu không hợp lệ',
  ALREADY_CANCELLED: 'Booking này đã bị hủy trước đó.',
  CANCEL_REASON_REQUIRED: 'Vui lòng nhập lý do hủy.',
  CANNOT_CANCEL_STARTED: 'Không thể hủy booking đã bắt đầu hoặc đã hoàn thành.',
  FORBIDDEN: 'Bạn không có quyền thực hiện thao tác này.',
  UNAUTHORIZED: 'Phiên đã hết hạn, vui lòng đăng nhập lại.',
  NOT_FOUND: 'Không tìm thấy dữ liệu.',
  BAD_REQUEST: 'Yêu cầu không hợp lệ.',
  INTERNAL_ERROR: 'Đã xảy ra lỗi không mong muốn, vui lòng thử lại sau.',
};
// NOTE: STATUS_TRANSITION is intentionally absent: its server message contains
// the dynamic from→to states, so it must flow through verbatim (see below).

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
