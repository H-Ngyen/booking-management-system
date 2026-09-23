'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useState, type ReactNode } from 'react';
import { ArrowLeft, CalendarDays, ConciergeBell, LogOut, Menu, ReceiptText } from 'lucide-react';
import { ADMIN_NAV } from '@/lib/nav';
import { useAuthMe, useLogout } from '@/hooks/useAuth';
import { cn } from '@/lib/cn';
import { AuthGuard } from '@/components/auth-guard';
import { AdminGuard } from '@/components/admin-guard';
import { Button } from '@/components/ui/button';

const NAV_ICONS: Record<string, typeof ReceiptText> = {
  '/admin/bookings': ReceiptText,
  '/admin/services': ConciergeBell,
  '/admin/schedules': CalendarDays,
};

function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();
  return (
    <nav className="flex flex-col gap-1 p-4" aria-label="Điều hướng quản trị">
      <Link href="/services" className="mb-4 px-2 text-base font-bold text-white">
        Service Booking
      </Link>
      {ADMIN_NAV.map((item) => {
        const Icon = NAV_ICONS[item.href] ?? ReceiptText;
        const active = pathname === item.href;
        return (
          <Link
            key={item.href}
            href={item.href}
            onClick={onNavigate}
            aria-current={active ? 'page' : undefined}
            className={cn(
              'flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold',
              active ? 'bg-primary text-white' : 'text-sidebar-foreground hover:bg-white/10 hover:text-white',
            )}
          >
            <Icon className="size-4" />
            {item.label}
          </Link>
        );
      })}
      <Link
        href="/services"
        className="mt-2 flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold text-sidebar-foreground hover:bg-white/10 hover:text-white"
      >
        <ArrowLeft className="size-4" />
        Về trang khách hàng
      </Link>
    </nav>
  );
}

function Topbar({ onMenu }: { onMenu: () => void }) {
  const router = useRouter();
  const { data: me } = useAuthMe();
  const logout = useLogout();

  const handleLogout = () => {
    logout.mutate(undefined, { onSuccess: () => router.replace('/login') });
  };

  return (
    <header className="flex h-16 items-center justify-between border-b border-line bg-surface px-4">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={onMenu} aria-label="Mở menu" className="lg:hidden">
          <Menu />
        </Button>
        <p className="hidden text-sm font-semibold text-ink lg:block">Bảng quản trị</p>
      </div>
      <div className="flex items-center gap-2">
        <span className="text-sm text-ink-3">{me?.userName} (Admin)</span>
        <Button variant="ghost" size="sm" onClick={handleLogout}>
          <LogOut />
          Đăng xuất
        </Button>
      </div>
    </header>
  );
}

export default function AdminLayout({ children }: { children: ReactNode }) {
  const [menuOpen, setMenuOpen] = useState(false);
  return (
    <AuthGuard>
      <AdminGuard>
        <div className="grid min-h-dvh grid-cols-1 lg:grid-cols-[260px_1fr]">
          <aside className="hidden bg-sidebar lg:block">
            <div className="sticky top-0 h-dvh overflow-y-auto">
              <Sidebar />
            </div>
          </aside>
          {menuOpen && (
            <div className="fixed inset-0 z-50 lg:hidden" role="dialog" aria-label="Menu quản trị">
              <div className="absolute inset-0 bg-ink/40" onClick={() => setMenuOpen(false)} />
              <aside className="absolute inset-y-0 left-0 w-64 bg-sidebar">
                <Sidebar onNavigate={() => setMenuOpen(false)} />
              </aside>
            </div>
          )}
          <div className="flex min-w-0 flex-col bg-background">
            <Topbar onMenu={() => setMenuOpen(true)} />
            <main className="flex-1 p-4 md:p-6 xl:p-8">{children}</main>
          </div>
        </div>
      </AdminGuard>
    </AuthGuard>
  );
}
