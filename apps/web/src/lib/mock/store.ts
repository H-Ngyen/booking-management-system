// In-memory mock store implementing the PDF business rules (§5) so the UI
// behaves like the real backend: overlap → 409, working-hours checks,
// role rules, cancel rules. Swap point for real API is documented in
// src/hooks/* (TODO(api)) — signatures here mirror lib/api/modules/*.

import { ApiError } from '../client';
import type {
  AuthUser,
  Booking,
  BookingStatus,
  Paginated,
  ServiceItem,
  Staff,
  WorkSchedule,
} from '../types';
import { DEMO_ACCOUNTS, seedBookings, seedSchedules, seedServices, seedStaffs } from './data';

const LATENCY_MS = 350;
const delay = () => new Promise((r) => setTimeout(r, LATENCY_MS));

function paginate<T>(rows: T[], page: number, pageSize: number): Paginated<T> {
  const total = rows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(Math.max(1, page), totalPages);
  return {
    items: rows.slice((safePage - 1) * pageSize, safePage * pageSize),
    page: safePage,
    pageSize,
    total,
    totalPages,
  };
}

function overlaps(aStart: number, aEnd: number, bStart: number, bEnd: number): boolean {
  return aStart < bEnd && aEnd > bStart;
}

function toMinutes(t: string): number {
  const [h, m] = t.split(':').map(Number);
  return h * 60 + m;
}

// ---------------------------------------------------------------- state ---

let services = seedServices();
const staffs = seedStaffs();
let schedules = seedSchedules();
let bookings = seedBookings();
let nextServiceId = 100;
let nextScheduleId = 1000;
let nextBookingId = 100;
let sessionUserId: number | null = null;

if (typeof window !== 'undefined') {
  const saved = window.localStorage.getItem('booking.mockSession');
  if (saved) sessionUserId = Number(saved) || null;
}

function persistSession() {
  if (typeof window === 'undefined') return;
  if (sessionUserId == null) window.localStorage.removeItem('booking.mockSession');
  else window.localStorage.setItem('booking.mockSession', String(sessionUserId));
}

function enrich(b: Booking): Booking {
  return {
    ...b,
    service: services.find((s) => s.id === b.serviceId),
    staff: staffs.find((s) => s.id === b.staffId),
  };
}

// ----------------------------------------------------------------- auth ---

export async function mockLogin(
  userName: string,
  password: string,
): Promise<{ accessToken: string; user: AuthUser }> {
  await delay();
  const account = DEMO_ACCOUNTS.find(
    (a) => a.userName.toLowerCase() === userName.trim().toLowerCase(),
  );
  if (!account || account.password !== password) {
    throw new ApiError(401, 'Tên đăng nhập hoặc mật khẩu không đúng.', 'UNAUTHORIZED');
  }
  sessionUserId = account.id;
  persistSession();
  return { accessToken: `mock-token-${account.id}`, user: toAuthUser(account) };
}

function toAuthUser(account: { id: number; userName: string; email: string; role: AuthUser['role'] }): AuthUser {
  return { id: account.id, userName: account.userName, email: account.email, role: account.role };
}

export async function mockLogout(): Promise<void> {
  await delay();
  sessionUserId = null;
  persistSession();
}

export async function mockGetMe(): Promise<AuthUser> {
  await delay();
  const account = DEMO_ACCOUNTS.find((a) => a.id === sessionUserId);
  if (!account) throw new ApiError(401, 'Phiên đã hết hạn, vui lòng đăng nhập lại.', 'UNAUTHORIZED');
  return toAuthUser(account);
}

// ------------------------------------------------------------- services ---

export interface MockServiceListParams {
  q?: string;
  page?: number;
  pageSize?: number;
  activeOnly?: boolean;
}

export async function mockListServices(
  params: MockServiceListParams = {},
): Promise<Paginated<ServiceItem>> {
  await delay();
  const q = (params.q ?? '').trim().toLowerCase();
  let rows = [...services];
  if (params.activeOnly) rows = rows.filter((s) => s.isActive);
  if (q) {
    rows = rows.filter(
      (s) => s.name.toLowerCase().includes(q) || (s.description ?? '').toLowerCase().includes(q),
    );
  }
  return paginate(rows, params.page ?? 1, params.pageSize ?? 10);
}

export interface MockServiceInput {
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive?: boolean;
}

function validateServiceInput(input: MockServiceInput) {
  if (!input.name.trim()) throw new ApiError(400, 'Tên dịch vụ là bắt buộc.', 'NAME_REQUIRED');
  if (!Number.isFinite(input.durationMinutes) || input.durationMinutes <= 0) {
    throw new ApiError(400, 'Thời lượng phải lớn hơn 0.', 'INVALID_DURATION');
  }
  if (!Number.isFinite(input.price) || input.price < 0) {
    throw new ApiError(400, 'Giá không được âm.', 'INVALID_PRICE');
  }
}

export async function mockCreateService(input: MockServiceInput): Promise<ServiceItem> {
  await delay();
  validateServiceInput(input);
  const created: ServiceItem = {
    id: nextServiceId++,
    name: input.name.trim(),
    description: input.description?.trim() || null,
    durationMinutes: input.durationMinutes,
    price: input.price,
    isActive: input.isActive ?? true,
  };
  services = [created, ...services];
  return created;
}

export async function mockUpdateService(
  id: number,
  patch: Partial<MockServiceInput>,
): Promise<ServiceItem> {
  await delay();
  const found = services.find((s) => s.id === id);
  if (!found) throw new ApiError(404, 'Không tìm thấy dịch vụ.', 'NOT_FOUND');
  const merged: MockServiceInput = {
    name: patch.name ?? found.name,
    description: patch.description ?? found.description,
    durationMinutes: patch.durationMinutes ?? found.durationMinutes,
    price: patch.price ?? found.price,
    isActive: patch.isActive ?? found.isActive,
  };
  validateServiceInput(merged);
  const updated: ServiceItem = { ...found, ...merged, name: merged.name.trim() };
  services = services.map((s) => (s.id === id ? updated : s));
  return updated;
}

// --------------------------------------------------------------- staffs ---

export async function mockListStaffs(activeOnly = true): Promise<Staff[]> {
  await delay();
  return activeOnly ? staffs.filter((s) => s.isActive) : [...staffs];
}

export async function mockListSchedules(
  staffId: number,
  from?: string,
  to?: string,
): Promise<WorkSchedule[]> {
  await delay();
  return schedules
    .filter((s) => s.staffId === staffId)
    .filter((s) => (from ? s.workDate >= from : true) && (to ? s.workDate <= to : true))
    .sort((a, b) => a.workDate.localeCompare(b.workDate) || a.startTime.localeCompare(b.startTime));
}

export interface MockScheduleInput {
  workDate: string;
  startTime: string;
  endTime: string;
}

export async function mockCreateSchedule(
  staffId: number,
  input: MockScheduleInput,
): Promise<WorkSchedule> {
  await delay();
  if (input.startTime >= input.endTime) {
    throw new ApiError(400, 'Giờ bắt đầu phải nhỏ hơn giờ kết thúc.', 'INVALID_SHIFT');
  }
  const start = toMinutes(input.startTime);
  const end = toMinutes(input.endTime);
  const clash = schedules.some(
    (s) =>
      s.staffId === staffId &&
      s.workDate === input.workDate &&
      overlaps(start, end, toMinutes(s.startTime), toMinutes(s.endTime)),
  );
  if (clash) {
    throw new ApiError(409, 'Ca làm việc bị trùng với ca đã có của nhân viên này.', 'SHIFT_CONFLICT');
  }
  const created: WorkSchedule = { id: nextScheduleId++, staffId, ...input };
  schedules = [...schedules, created];
  return created;
}

// -------------------------------------------------------------- bookings ---

export interface MockBookingListParams {
  date?: string;
  status?: BookingStatus;
  page?: number;
  pageSize?: number;
}

function filterBookings(rows: Booking[], params: MockBookingListParams): Booking[] {
  let out = [...rows].sort((a, b) => b.startTime.localeCompare(a.startTime));
  if (params.status) out = out.filter((b) => b.status === params.status);
  if (params.date) out = out.filter((b) => b.startTime.slice(0, 10) === params.date);
  return out.map(enrich);
}

export async function mockListMyBookings(
  customerId: number,
  params: MockBookingListParams = {},
): Promise<Paginated<Booking>> {
  await delay();
  return paginate(
    filterBookings(bookings.filter((b) => b.customerId === customerId), params),
    params.page ?? 1,
    params.pageSize ?? 10,
  );
}

export async function mockListBookings(
  params: MockBookingListParams = {},
): Promise<Paginated<Booking>> {
  await delay();
  return paginate(filterBookings(bookings, params), params.page ?? 1, params.pageSize ?? 10);
}

export interface MockCreateBookingInput {
  customerId: number;
  serviceId: number;
  staffId: number;
  startTime: string; // ISO — backend computes EndTime
  customerNote?: string;
}

export async function mockAvailableSlots(
  serviceId: number,
  staffId: number,
  date: string,
): Promise<string[]> {
  await delay();
  const service = services.find((s) => s.id === serviceId);
  if (!service) return [];
  const daySchedules = schedules.filter((s) => s.staffId === staffId && s.workDate === date);
  const active = bookings.filter(
    (b) => b.staffId === staffId && b.status !== 'Cancelled' && b.startTime.slice(0, 10) === date,
  );
  const slots: string[] = [];
  for (const shift of daySchedules) {
    let cursor = toMinutes(shift.startTime);
    const shiftEnd = toMinutes(shift.endTime);
    while (cursor + service.durationMinutes <= shiftEnd) {
      const h = String(Math.floor(cursor / 60)).padStart(2, '0');
      const m = String(cursor % 60).padStart(2, '0');
      const start = new Date(`${date}T${h}:${m}:00`).getTime();
      const end = start + service.durationMinutes * 60_000;
      const clash = active.some((b) =>
        overlaps(start, end, new Date(b.startTime).getTime(), new Date(b.endTime).getTime()),
      );
      if (!clash && start > Date.now()) slots.push(new Date(start).toISOString());
      cursor += 30;
    }
  }
  return slots;
}

export async function mockCreateBooking(input: MockCreateBookingInput): Promise<Booking> {
  await delay();
  const service = services.find((s) => s.id === input.serviceId);
  const staff = staffs.find((s) => s.id === input.staffId);
  if (!service || !service.isActive) {
    throw new ApiError(400, 'Dịch vụ này hiện đang tạm khóa.', 'SERVICE_INACTIVE');
  }
  if (!staff || !staff.isActive) {
    throw new ApiError(400, 'Nhân viên này hiện đang tạm nghỉ.', 'STAFF_INACTIVE');
  }
  const start = new Date(input.startTime).getTime();
  if (!Number.isFinite(start)) throw new ApiError(400, 'Thời gian bắt đầu không hợp lệ.', 'INVALID_TIME');
  if (start <= Date.now()) {
    throw new ApiError(400, 'Không thể đặt lịch trong quá khứ.', 'BOOKING_IN_PAST');
  }
  const end = start + service.durationMinutes * 60_000;
  const date = new Date(start);
  const yyyyMmDd = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  const startMin = date.getHours() * 60 + date.getMinutes();
  const endDate = new Date(end);
  const endMin = endDate.getHours() * 60 + endDate.getMinutes();
  const withinShift = schedules.some(
    (s) =>
      s.staffId === input.staffId &&
      s.workDate === yyyyMmDd &&
      toMinutes(s.startTime) <= startMin &&
      endMin <= toMinutes(s.endTime),
  );
  if (!withinShift) {
    throw new ApiError(400, 'Khung giờ nằm ngoài giờ làm việc của nhân viên.', 'OUTSIDE_WORKING_HOURS');
  }
  const clash = bookings.some(
    (b) =>
      b.staffId === input.staffId &&
      b.status !== 'Cancelled' &&
      overlaps(start, end, new Date(b.startTime).getTime(), new Date(b.endTime).getTime()),
  );
  if (clash) {
    throw new ApiError(
      409,
      'Khung giờ này vừa có người đặt. Vui lòng chọn khung giờ khác.',
      'BOOKING_CONFLICT',
    );
  }
  const created: Booking = {
    id: nextBookingId++,
    bookingCode: `BK-${yyyyMmDd.replaceAll('-', '')}-${String(nextBookingId).padStart(4, '0')}`,
    customerId: input.customerId,
    serviceId: input.serviceId,
    staffId: input.staffId,
    startTime: new Date(start).toISOString(),
    endTime: new Date(end).toISOString(),
    status: 'Pending',
    customerNote: input.customerNote?.trim() || null,
    cancellationReason: null,
    createdAt: new Date().toISOString(),
  };
  bookings = [created, ...bookings];
  return enrich(created);
}

const ADMIN_TRANSITIONS: Record<string, BookingStatus[]> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Completed', 'Cancelled'],
  Completed: [],
  Cancelled: [],
};

export async function mockUpdateBookingStatus(
  id: number,
  status: BookingStatus,
  actorRole: string,
): Promise<Booking> {
  await delay();
  if (actorRole !== 'Admin') {
    throw new ApiError(403, 'Bạn không có quyền thực hiện thao tác này.', 'FORBIDDEN');
  }
  const found = bookings.find((b) => b.id === id);
  if (!found) throw new ApiError(404, 'Không tìm thấy booking.', 'NOT_FOUND');
  if (!ADMIN_TRANSITIONS[found.status].includes(status)) {
    throw new ApiError(400, `Không thể chuyển từ ${found.status} sang ${status}.`, 'INVALID_TRANSITION');
  }
  const updated = { ...found, status };
  bookings = bookings.map((b) => (b.id === id ? updated : b));
  return enrich(updated);
}

export async function mockCancelBooking(
  id: number,
  reason: string,
  actorId: number,
  actorRole: string,
): Promise<Booking> {
  await delay();
  const found = bookings.find((b) => b.id === id);
  if (!found) throw new ApiError(404, 'Không tìm thấy booking.', 'NOT_FOUND');
  if (actorRole !== 'Admin' && found.customerId !== actorId) {
    throw new ApiError(403, 'Bạn chỉ được hủy booking của mình.', 'FORBIDDEN');
  }
  if (!reason.trim()) {
    throw new ApiError(400, 'Vui lòng nhập lý do hủy.', 'CANCEL_REASON_REQUIRED');
  }
  if (found.status === 'Completed' || new Date(found.startTime).getTime() <= Date.now()) {
    throw new ApiError(400, 'Không thể hủy booking đã hoàn thành hoặc đã bắt đầu.', 'CANNOT_CANCEL_STARTED');
  }
  if (found.status === 'Cancelled') {
    throw new ApiError(400, 'Booking này đã bị hủy trước đó.', 'ALREADY_CANCELLED');
  }
  const updated: Booking = { ...found, status: 'Cancelled', cancellationReason: reason.trim() };
  bookings = bookings.map((b) => (b.id === id ? updated : b));
  return enrich(updated);
}
