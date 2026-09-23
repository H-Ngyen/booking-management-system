export type UserRole = 'Customer' | 'Admin';

export interface AuthUser {
  id: number;
  userName: string;
  email: string;
  role: UserRole;
}

export interface ServiceItem {
  id: number;
  name: string;
  description?: string | null;
  durationMinutes: number;
  price: number;
  isActive: boolean;
}

export interface Staff {
  id: number;
  fullName: string;
  email: string;
  isActive: boolean;
}

export interface WorkSchedule {
  id: number;
  staffId: number;
  workDate: string; // YYYY-MM-DD
  startTime: string; // HH:mm
  endTime: string; // HH:mm
}

export type BookingStatus = 'Pending' | 'Confirmed' | 'Completed' | 'Cancelled';

export interface Booking {
  id: number;
  bookingCode: string;
  customerId: number;
  serviceId: number;
  staffId: number;
  startTime: string; // ISO
  endTime: string; // ISO
  status: BookingStatus;
  customerNote?: string | null;
  cancellationReason?: string | null;
  createdAt: string; // ISO
  service?: ServiceItem;
  staff?: Staff;
}

export interface Paginated<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}
