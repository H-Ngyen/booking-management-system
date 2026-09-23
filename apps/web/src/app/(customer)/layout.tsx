'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import type { ReactNode } from 'react';
import { CalendarPlus, LayoutGrid, LogOut, ReceiptText, ShieldCheck } from 'lucide-react';
import { CUSTOMER_NAV } from '@/lib/nav';
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

  const handleLogout = () => {
    logout.mutate(undefined, { onSuccess: () => router.replace('/login') });
  };

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-surface">
      <div className="mx-auto flex h-16 max-w-5xl items-center justify-between px-4">
        <nav className="flex items-center gap-1" aria-label="Điều hướng khách hàng">
          <Link href="/services" className="mr-4 text-base font-bold text-ink">
            Service Booking
          </Link>
          {CUSTOMER_NAV.map((item) => {
            const Icon = NAV_ICONS[item.href] ?? LayoutGrid;
            const active = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={active ? 'page' : undefined}
                className={cn(
                  'flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-semibold',
                  active ? 'bg-surface-3 text-primary' : 'text-ink-2 hover:bg-surface-2',
                )}
              >
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
          <Button variant="ghost" size="sm" onClick={handleLogout}>
            <LogOut />
            Đăng xuất
          </Button>
        </div>
      </div>
    </header>
  );
}

export default function CustomerLayout({ children }: { children: ReactNode }) {
  return (
    <AuthGuard>
      <TopNav />
      <main className="mx-auto w-full max-w-5xl flex-1 p-4 md:p-6">{children}</main>
    </AuthGuard>
  );
}
