'use client';

import { useState, type FormEvent } from 'react';
import { useCancelBooking, useMyBookings } from '@/hooks';
import { getErrorMessage } from '@/lib/error-messages';
import type { BookingStatus } from '@/lib/types';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { EmptyState, ErrorState, TableSkeleton } from '@/components/ui/feedback';
import { Input } from '@/components/ui/input';
import { Pagination } from '@/components/ui/pagination';
import { StatusBadge } from '@/components/ui/status-badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { Field } from '@/components/ui/field';

const PAGE_SIZE = 10;

const STATUS_FILTERS: Array<{ value: string; label: string }> = [
  { value: 'All', label: 'Tất cả' },
  { value: 'Pending', label: 'Chờ xác nhận' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'Completed', label: 'Hoàn thành' },
  { value: 'Cancelled', label: 'Đã hủy' },
];

const CANCELLABLE: BookingStatus[] = ['Pending', 'Confirmed'];

export default function MyBookingsPage() {
  const [status, setStatus] = useState('All');
  const [date, setDate] = useState('');
  const [page, setPage] = useState(1);
  const [cancelId, setCancelId] = useState<number | null>(null);
  const [reason, setReason] = useState('');
  const [reasonError, setReasonError] = useState('');

  const { data, isLoading, isError, error, refetch } = useMyBookings({
    status: status === 'All' ? undefined : (status as BookingStatus),
    date: date || undefined,
    page,
    pageSize: PAGE_SIZE,
  });
  const cancelBooking = useCancelBooking();

  const openCancel = (id: number) => {
    setCancelId(id);
    setReason('');
    setReasonError('');
  };

  const handleCancel = (e: FormEvent) => {
    e.preventDefault();
    if (cancelId == null) return;
    if (!reason.trim()) {
      setReasonError('Vui lòng nhập lý do hủy.');
      return;
    }
    cancelBooking.mutate(
      { id: cancelId, reason: reason.trim() },
      {
        onSuccess: () => setCancelId(null),
        onError: (err) => setReasonError(getErrorMessage(err)),
      },
    );
  };

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-ink">Lịch của tôi</h1>
        <p className="text-sm text-ink-3">Xem, lọc và hủy booking của bạn.</p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Tabs
          value={status}
          onValueChange={(v) => {
            setStatus(v);
            setPage(1);
          }}
        >
          <TabsList>
            {STATUS_FILTERS.map((f) => (
              <TabsTrigger key={f.value} value={f.value}>
                {f.label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>
        <Input
          type="date"
          value={date}
          aria-label="Lọc theo ngày"
          onChange={(e) => {
            setDate(e.target.value);
            setPage(1);
          }}
          className="max-w-[180px]"
        />
      </div>

      {isLoading && <TableSkeleton />}
      {isError && <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />}
      {data && data.items.length === 0 && (
        <EmptyState title="Chưa có booking nào" hint="Đặt lịch dịch vụ đầu tiên của bạn ngay." />
      )}
      {data && data.items.length > 0 && (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã</TableHead>
                <TableHead>Dịch vụ</TableHead>
                <TableHead>Bắt đầu</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Hành động</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((b) => (
                <TableRow key={b.id}>
                  <TableCell className="font-mono text-[13px]">{b.bookingCode}</TableCell>
                  <TableCell>{b.service?.name ?? `#${b.serviceId}`}</TableCell>
                  <TableCell>{new Date(b.startTime).toLocaleString('vi-VN')}</TableCell>
                  <TableCell>
                    <StatusBadge status={b.status} />
                  </TableCell>
                  <TableCell>
                    {CANCELLABLE.includes(b.status) ? (
                      <Button variant="destructive" size="sm" onClick={() => openCancel(b.id)}>
                        Hủy
                      </Button>
                    ) : (
                      <span className="text-sm text-ink-4">—</span>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
      {data && <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />}

      <Dialog open={cancelId != null} onOpenChange={(open) => !open && setCancelId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Hủy booking</DialogTitle>
            <DialogDescription>Booking đã hủy sẽ không còn chiếm khung giờ.</DialogDescription>
          </DialogHeader>
          <form onSubmit={handleCancel} className="space-y-4" noValidate>
            <Field label="Lý do hủy" htmlFor="cancel-reason" error={reasonError}>
              <Textarea
                id="cancel-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                placeholder="vd. Bận đột xuất…"
              />
            </Field>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCancelId(null)}>
                Đóng
              </Button>
              <Button type="submit" variant="destructive" loading={cancelBooking.isPending}>
                Xác nhận hủy
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
