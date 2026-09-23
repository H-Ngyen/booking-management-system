'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { CalendarPlus, Clock, Search, Wallet } from 'lucide-react';
import { useServices } from '@/hooks';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { getErrorMessage } from '@/lib/error-messages';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { EmptyState, ErrorState, TableSkeleton } from '@/components/ui/feedback';
import { Input } from '@/components/ui/input';
import { Pagination } from '@/components/ui/pagination';

const PAGE_SIZE = 6;

export default function ServicesPage() {
  const router = useRouter();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search);
  const { data, isLoading, isError, error, refetch } = useServices({
    q: debouncedSearch || undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-ink">Dịch vụ</h1>
          <p className="text-sm text-ink-3">Chọn dịch vụ và đặt lịch với nhân viên bạn muốn.</p>
        </div>
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-ink-4" />
          <Input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Tìm kiếm dịch vụ…"
            aria-label="Tìm kiếm dịch vụ"
            className="w-64 pl-9"
          />
        </div>
      </div>

      {isLoading && <TableSkeleton rows={3} />}
      {isError && <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />}
      {data && data.items.length === 0 && (
        <EmptyState title="Không có dịch vụ nào" hint="Thử từ khóa khác hoặc quay lại sau." />
      )}
      {data && data.items.length > 0 && (
        <>
          <div className="grid gap-4 sm:grid-cols-2">
            {data.items.map((service) => (
              <Card key={service.id} className={!service.isActive ? 'opacity-70' : undefined}>
                <CardHeader>
                  <div className="flex items-start justify-between gap-3">
                    <CardTitle>{service.name}</CardTitle>
                    {!service.isActive && (
                      <span className="rounded-full bg-surface-2 px-2 py-0.5 text-xs font-semibold text-ink-3">
                        Tạm khóa
                      </span>
                    )}
                  </div>
                  {service.description && <CardDescription>{service.description}</CardDescription>}
                </CardHeader>
                <CardContent>
                  <div className="flex items-center gap-4 text-sm text-ink-2">
                    <span className="flex items-center gap-1.5">
                      <Clock className="size-4 text-ink-4" />
                      {service.durationMinutes} phút
                    </span>
                    <span className="flex items-center gap-1.5">
                      <Wallet className="size-4 text-ink-4" />
                      {service.price.toLocaleString('vi-VN')}đ
                    </span>
                  </div>
                  <div className="mt-4">
                    {service.isActive ? (
                      <Button
                        className="w-full sm:w-auto"
                        onClick={() => router.push(`/booking?serviceId=${service.id}`)}
                      >
                        <CalendarPlus />
                        Đặt lịch
                      </Button>
                    ) : (
                      <Button disabled className="w-full sm:w-auto">
                        Tạm khóa
                      </Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      )}
    </div>
  );
}
