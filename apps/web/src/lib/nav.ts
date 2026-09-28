export interface NavItem {
  href: string;
  label: string;
}

export const CUSTOMER_NAV: NavItem[] = [
  { href: '/services', label: 'Dịch vụ' },
  { href: '/booking', label: 'Đặt lịch' },
  { href: '/my-bookings', label: 'Lịch của tôi' },
];

export const ADMIN_NAV: NavItem[] = [
  { href: '/admin/bookings', label: 'Bookings' },
  { href: '/admin/services', label: 'Dịch vụ' },
  { href: '/admin/staffs', label: 'Nhân viên' },
  { href: '/admin/schedules', label: 'Lịch làm việc' },
];
