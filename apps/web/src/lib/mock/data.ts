// Demo seed data — satisfies the requirements minimum (§9):
// 1 Admin, 2 Customers, 2 Staffs, 5 Services, 7-day schedules, 10 bookings.
// Dates are generated relative to "today" so the demo never goes stale.

import type { AuthUser, Booking, BookingStatus, ServiceItem, Staff, WorkSchedule } from '../types';

export interface DemoAccount extends AuthUser {
  password: string;
}

function isoDate(offsetDays: number): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  return d.toISOString().slice(0, 10);
}

function at(date: string, time: string): string {
  return `${date}T${time}:00`;
}

export const DEMO_ACCOUNTS: DemoAccount[] = [
  { id: 1, userName: 'admin', email: 'admin@booking.local', role: 'Admin', password: 'admin123' },
  { id: 2, userName: 'customer1', email: 'customer1@booking.local', role: 'Customer', password: 'customer123' },
  { id: 3, userName: 'customer2', email: 'customer2@booking.local', role: 'Customer', password: 'customer123' },
];

export function seedServices(): ServiceItem[] {
  return [
    { id: 1, name: 'Cắt tóc nam', description: 'Tư vấn kiểu, cắt, gội và sấy tạo kiểu.', durationMinutes: 45, price: 120000, isActive: true },
    { id: 2, name: 'Gội đầu dưỡng sinh', description: 'Gội, massage đầu vai gáy với thảo dược.', durationMinutes: 30, price: 80000, isActive: true },
    { id: 3, name: 'Nhuộm tóc', description: 'Tư vấn màu, nhuộm và phục hồi sau nhuộm.', durationMinutes: 120, price: 450000, isActive: true },
    { id: 4, name: 'Massage body', description: 'Massage toàn thân với tinh dầu, giảm căng cơ.', durationMinutes: 60, price: 250000, isActive: true },
    { id: 5, name: 'Chăm sóc da mặt', description: 'Làm sạch sâu, đắp mặt nạ và dưỡng ẩm.', durationMinutes: 90, price: 300000, isActive: false },
  ];
}

export function seedStaffs(): Staff[] {
  return [
    { id: 1, fullName: 'Nguyễn Văn An', email: 'an.nv@booking.local', isActive: true },
    { id: 2, fullName: 'Trần Thị Bình', email: 'binh.tt@booking.local', isActive: true },
  ];
}

const SHIFTS: Array<[string, string]> = [
  ['08:00', '12:00'],
  ['13:30', '17:30'],
];

export function seedSchedules(): WorkSchedule[] {
  const rows: WorkSchedule[] = [];
  let id = 1;
  for (const staffId of [1, 2]) {
    for (let day = 0; day < 7; day += 1) {
      for (const [start, end] of SHIFTS) {
        rows.push({ id: id++, staffId, workDate: isoDate(day), startTime: start, endTime: end });
      }
    }
  }
  return rows;
}

interface BookingSeed {
  code: string;
  customerId: number;
  serviceId: number;
  staffId: number;
  dayOffset: number;
  start: string;
  status: BookingStatus;
  note?: string;
  reason?: string;
}

const BOOKING_SEEDS: BookingSeed[] = [
  { code: 'BK-PAST-0001', customerId: 2, serviceId: 1, staffId: 1, dayOffset: -3, start: '09:00', status: 'Completed', note: 'Cắt ngắn hai bên' },
  { code: 'BK-PAST-0002', customerId: 3, serviceId: 4, staffId: 2, dayOffset: -1, start: '14:00', status: 'Completed' },
  { code: 'BK-FUT-0003', customerId: 2, serviceId: 1, staffId: 1, dayOffset: 0, start: '10:30', status: 'Confirmed' },
  { code: 'BK-FUT-0004', customerId: 2, serviceId: 2, staffId: 2, dayOffset: 1, start: '09:00', status: 'Pending', note: 'Gội nhẹ nhàng' },
  { code: 'BK-FUT-0005', customerId: 3, serviceId: 3, staffId: 1, dayOffset: 1, start: '13:30', status: 'Pending' },
  { code: 'BK-FUT-0006', customerId: 2, serviceId: 4, staffId: 2, dayOffset: 2, start: '15:00', status: 'Confirmed' },
  { code: 'BK-FUT-0007', customerId: 3, serviceId: 1, staffId: 2, dayOffset: 3, start: '08:30', status: 'Pending' },
  { code: 'BK-CNCL-0008', customerId: 2, serviceId: 2, staffId: 1, dayOffset: 1, start: '15:30', status: 'Cancelled', reason: 'Bận đột xuất' },
  { code: 'BK-CNCL-0009', customerId: 3, serviceId: 4, staffId: 1, dayOffset: -2, start: '10:00', status: 'Cancelled', reason: 'Đổi sang hôm khác' },
  { code: 'BK-FUT-0010', customerId: 2, serviceId: 3, staffId: 2, dayOffset: 4, start: '13:30', status: 'Confirmed', note: 'Màu nâu hạt dẻ' },
];

const DURATION_BY_SERVICE: Record<number, number> = { 1: 45, 2: 30, 3: 120, 4: 60, 5: 90 };

export function seedBookings(): Booking[] {
  return BOOKING_SEEDS.map((s, i) => {
    const date = isoDate(s.dayOffset);
    const start = new Date(at(date, s.start));
    const end = new Date(start.getTime() + DURATION_BY_SERVICE[s.serviceId] * 60_000);
    return {
      id: i + 1,
      bookingCode: s.code,
      customerId: s.customerId,
      serviceId: s.serviceId,
      staffId: s.staffId,
      startTime: start.toISOString(),
      endTime: end.toISOString(),
      status: s.status,
      customerNote: s.note,
      cancellationReason: s.reason,
      createdAt: new Date(Date.now() - (i + 1) * 3600_000).toISOString(),
    };
  });
}
