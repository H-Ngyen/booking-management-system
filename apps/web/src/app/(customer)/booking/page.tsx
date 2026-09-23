'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useMemo, useState, type FormEvent } from 'react';
import { useAvailableSlots, useCreateBooking, useServices, useStaffs } from '@/hooks';
import { getErrorMessage } from '@/lib/error-messages';
import { ApiError } from '@/lib/client';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/feedback';
import { Field, FieldError } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/cn';

function todayISO(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function BookingForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { data: services } = useServices({ pageSize: 100 });
  const { data: staffs } = useStaffs();
  const createBooking = useCreateBooking();

  const [serviceId, setServiceId] = useState<number | null>(() => {
    const raw = searchParams.get('serviceId');
    return raw ? Number(raw) : null;
  });
  const [staffId, setStaffId] = useState<number | null>(null);
  const [date, setDate] = useState(todayISO());
  const [slot, setSlot] = useState<string | null>(null);
  const [note, setNote] = useState('');
  const [formError, setFormError] = useState('');

  const { data: slots, isLoading: slotsLoading } = useAvailableSlots(serviceId, staffId, date);
  const selectedService = useMemo(
    () => services?.items.find((s) => s.id === serviceId),
    [services, serviceId],
  );

  const canSubmit = serviceId != null && staffId != null && slot != null && !createBooking.isPending;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit || !slot) return;
    setFormError('');
    createBooking.mutate(
      { serviceId, staffId: staffId as number, startTime: slot, customerNote: note || undefined },
      {
        onSuccess: () => router.push('/my-bookings'),
        onError: (error) => {
          // 409 overlap keeps the selected slot so the user can pick another.
          const message =
            error instanceof ApiError && error.status === 409
              ? error.message || 'Khung giờ này vừa có người đặt. Vui lòng chọn khung giờ khác.'
              : getErrorMessage(error);
          setFormError(message);
        },
      },
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4" noValidate>
      <Card>
        <CardHeader>
          <CardTitle>1. Chọn dịch vụ</CardTitle>
        </CardHeader>
        <CardContent>
          <Field label="Dịch vụ">
            <Select
              value={serviceId ? String(serviceId) : ''}
              onValueChange={(v) => {
                setServiceId(v ? Number(v) : null);
                setSlot(null);
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="— Chọn dịch vụ —" />
              </SelectTrigger>
              <SelectContent>
                {(services?.items ?? [])
                  .filter((s) => s.isActive)
                  .map((s) => (
                    <SelectItem key={s.id} value={String(s.id)}>
                      {s.name} · {s.durationMinutes} phút
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
          </Field>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>2. Chọn nhân viên, ngày và khung giờ</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Nhân viên">
              <Select
                value={staffId ? String(staffId) : ''}
                onValueChange={(v) => {
                  setStaffId(v ? Number(v) : null);
                  setSlot(null);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="— Chọn nhân viên —" />
                </SelectTrigger>
                <SelectContent>
                  {(staffs ?? []).map((s) => (
                    <SelectItem key={s.id} value={String(s.id)}>
                      {s.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
            <Field label="Ngày" htmlFor="date">
              <Input
                id="date"
                type="date"
                value={date}
                min={todayISO()}
                onChange={(e) => {
                  setDate(e.target.value);
                  setSlot(null);
                }}
              />
            </Field>
          </div>

          {slotsLoading && <Spinner label="Đang tải khung giờ trống…" />}
          {slots && slots.length === 0 && (
            <p className="text-sm text-ink-3">Ngày này không còn khung giờ trống. Hãy chọn ngày khác.</p>
          )}
          {slots && slots.length > 0 && (
            <div className="grid grid-cols-3 gap-2 sm:grid-cols-4" role="radiogroup" aria-label="Khung giờ trống">
              {slots.map((iso) => {
                const time = new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
                const selected = slot === iso;
                return (
                  <button
                    key={iso}
                    type="button"
                    role="radio"
                    aria-checked={selected}
                    onClick={() => setSlot(iso)}
                    className={cn(
                      'rounded-lg border px-2 py-2.5 text-sm font-semibold transition-colors',
                      selected
                        ? 'border-primary bg-surface-3 text-primary'
                        : 'border-line bg-surface text-ink hover:bg-surface-2',
                    )}
                  >
                    {time}
                  </button>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>3. Xác nhận</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {selectedService && slot && (
            <p className="rounded-lg bg-surface-2 px-3 py-2 text-sm text-ink-2">
              {selectedService.name} · Bắt đầu {new Date(slot).toLocaleString('vi-VN')} · Kết thúc
              dự kiến{' '}
              {new Date(
                new Date(slot).getTime() + selectedService.durationMinutes * 60_000,
              ).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
            </p>
          )}
          <Field label="Ghi chú (không bắt buộc)" htmlFor="note">
            <Textarea id="note" value={note} onChange={(e) => setNote(e.target.value)} rows={3} />
          </Field>
          <FieldError message={formError} />
          <Button type="submit" disabled={!canSubmit} loading={createBooking.isPending} className="w-full sm:w-auto">
            Xác nhận đặt lịch
          </Button>
        </CardContent>
      </Card>
    </form>
  );
}

export default function BookingPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-ink">Đặt lịch</h1>
        <p className="text-sm text-ink-3">Backend sẽ tự tính giờ kết thúc từ thời lượng dịch vụ.</p>
      </div>
      <Suspense fallback={<Spinner label="Đang tải…" />}>
        <BookingForm />
      </Suspense>
    </div>
  );
}
