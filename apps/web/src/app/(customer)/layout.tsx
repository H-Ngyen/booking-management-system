'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useState, type ReactNode } from 'react';
import { CalendarPlus, LayoutGrid, LogOut, Menu, ReceiptText, ShieldCheck, X } from 'lucide-react';
import { CUSTOMER_NAV } from '@/lib/nav';
import { useBookingRealtime } from '@/hooks';
import { useAuthMe, useLogout } from '@/hooks/useAuth';
import { cn } from '@/lib/cn';
import { AuthGuard } from '@/components/auth-guard';
import { Button } from '@/components/ui/button';

const NAV_ICONS: Record<string, typeof LayoutGrid> = {
  '/services': LayoutGrid,
  '/booking': CalendarPlus,
  '/my-bookings': ReceiptText,
};

function TopNav() {
  const pathname = usePathname();
  const router = useRouter();
  const { data: me } = useAuthMe();
  const logout = useLogout();
  const [menuOpen, setMenuOpen] = useState(false);

  const handleLogout = () => {
    setMenuOpen(false);
    logout.mutate(undefined, { onSuccess: () => router.replace('/login') });
  };

  const linkClass = (active: boolean) =>
    cn(
      'flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-semibold',
      active ? 'bg-surface-3 text-primary' : 'text-ink-2 hover:bg-surface-2',
    );

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-surface">
      <div className="mx-auto flex h-16 max-w-5xl items-center justify-between px-4">
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setMenuOpen(true)}
            aria-label="Mở menu"
            className="md:hidden"
          >
            <Menu />
          </Button>
          <Link href="/services" className="text-base font-bold text-ink">
            Service Booking
          </Link>
        </div>
        <nav className="hidden items-center gap-1 md:flex" aria-label="Điều hướng khách hàng">
          {CUSTOMER_NAV.map((item) => {
            const Icon = NAV_ICONS[item.href] ?? LayoutGrid;
            const active = pathname === item.href;
            return (
              <Link key={item.href} href={item.href} aria-current={active ? 'page' : undefined} className={linkClass(active)}>
                <Icon className="size-4" />
                {item.label}
              </Link>
            );
          })}
          {me?.role === 'Admin' && (
            <Link
              href="/admin/bookings"
              className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-semibold text-ink-2 hover:bg-surface-2"
            >
              <ShieldCheck className="size-4" />
              Quản trị
            </Link>
          )}
        </nav>
        <div className="flex items-center gap-2">
          <span className="hidden text-sm text-ink-3 sm:block">{me?.userName}</span>
          <Button variant="ghost" size="sm" onClick={handleLogout} aria-label="Đăng xuất">
            <LogOut />
            <span className="hidden sm:inline">Đăng xuất</span>
          </Button>
        </div>
      </div>
      {menuOpen && (
        <div className="fixed inset-0 z-50 md:hidden" role="dialog" aria-label="Menu khách hàng">
          <div className="absolute inset-0 bg-ink/40" onClick={() => setMenuOpen(false)} />
          <nav
            className="absolute inset-y-0 left-0 flex w-64 flex-col gap-1 bg-surface p-4"
            aria-label="Điều hướng khách hàng"
          >
            <div className="mb-2 flex items-center justify-between">
              <span className="px-2 text-base font-bold text-ink">Service Booking</span>
              <Button variant="ghost" size="icon" onClick={() => setMenuOpen(false)} aria-label="Đóng menu">
                <X />
              </Button>
            </div>
            {CUSTOMER_NAV.map((item) => {
              const Icon = NAV_ICONS[item.href] ?? LayoutGrid;
              const active = pathname === item.href;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => setMenuOpen(false)}
                  aria-current={active ? 'page' : undefined}
                  className={linkClass(active)}
                >
                  <Icon className="size-4" />
                  {item.label}
                </Link>
              );
            })}
            {me?.role === 'Admin' && (
              <Link
                href="/admin/bookings"
                onClick={() => setMenuOpen(false)}
                className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-semibold text-ink-2 hover:bg-surface-2"
              >
                <ShieldCheck className="size-4" />
                Quản trị
              </Link>
            )}
          </nav>
        </div>
      )}
    </header>
  );
}

export default function CustomerLayout({ children }: { children: ReactNode }) {
  useBookingRealtime();
  return (
    <AuthGuard>
      <TopNav />
      <main className="mx-auto w-full max-w-5xl flex-1 p-4 md:p-6">{children}</main>
    </AuthGuard>
  );
}
