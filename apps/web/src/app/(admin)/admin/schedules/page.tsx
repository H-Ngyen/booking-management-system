'use client';

import { useMemo, useState, type FormEvent } from 'react';
import { useCreateSchedule, useSchedules, useStaffs } from '@/hooks';
import { getErrorMessage } from '@/lib/error-messages';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { EmptyState, ErrorState, Spinner } from '@/components/ui/feedback';
import { Field, FieldError } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

export default function AdminSchedulesPage() {
  const { data: staffs, isLoading: staffsLoading } = useStaffs(false);
  const [staffId, setStaffId] = useState<number | null>(null);
  const [workDate, setWorkDate] = useState('');
  const [startTime, setStartTime] = useState('08:00');
  const [endTime, setEndTime] = useState('12:00');
  const [formError, setFormError] = useState('');

  const { data: schedules, isLoading, isError, error, refetch } = useSchedules(staffId);
  const createSchedule = useCreateSchedule();

  const grouped = useMemo(() => {
    const map = new Map<string, NonNullable<typeof schedules>>();
    for (const s of schedules ?? []) {
      const list = map.get(s.workDate) ?? [];
      list.push(s);
      map.set(s.workDate, list);
    }
    return [...map.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [schedules]);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (staffId == null) return setFormError('Hãy chọn nhân viên.');
    if (!workDate) return setFormError('Hãy chọn ngày làm việc.');
    if (startTime >= endTime) return setFormError('Giờ bắt đầu phải nhỏ hơn giờ kết thúc.');
    setFormError('');
    createSchedule.mutate(
      { staffId, input: { workDate, startTime, endTime } },
      { onError: (err) => setFormError(getErrorMessage(err)) },
    );
  };

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold text-ink">Quản lý lịch làm việc</h1>
        <p className="text-sm text-ink-3">Thiết lập ca làm việc cho từng nhân viên. Booking phải nằm trong giờ làm việc.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Thêm ca làm việc</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5" noValidate>
            <Field label="Nhân viên">
              <Select value={staffId ? String(staffId) : ''} onValueChange={(v) => setStaffId(v ? Number(v) : null)}>
                <SelectTrigger>
                  <SelectValue placeholder="— Chọn —" />
                </SelectTrigger>
                <SelectContent>
                  {(staffs ?? []).map((s) => (
                    <SelectItem key={s.id} value={String(s.id)}>
                      {s.fullName} {!s.isActive && '(đã khóa)'}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
            <Field label="Ngày" htmlFor="sch-date">
              <Input id="sch-date" type="date" value={workDate} onChange={(e) => setWorkDate(e.target.value)} />
            </Field>
            <Field label="Bắt đầu" htmlFor="sch-start">
              <Input id="sch-start" type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
            </Field>
            <Field label="Kết thúc" htmlFor="sch-end">
              <Input id="sch-end" type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
            </Field>
            <div className="flex items-end">
              <Button type="submit" loading={createSchedule.isPending} className="w-full">
                Thêm ca
              </Button>
            </div>
          </form>
          <FieldError message={formError} />
        </CardContent>
      </Card>

      {staffsLoading && <Spinner label="Đang tải nhân viên…" />}
      {staffId == null && !staffsLoading && <EmptyState title="Chọn nhân viên để xem lịch làm việc" />}
      {staffId != null && isLoading && <Spinner label="Đang tải lịch…" />}
      {staffId != null && isError && <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />}
      {staffId != null && schedules && schedules.length === 0 && <EmptyState title="Chưa có ca làm việc nào" />}
      {staffId != null && grouped.length > 0 && (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ngày</TableHead>
                <TableHead>Ca làm việc</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {grouped.map(([date, shifts]) => (
                <TableRow key={date}>
                  <TableCell className="font-semibold whitespace-nowrap">
                    {new Date(`${date}T00:00:00`).toLocaleDateString('vi-VN', {
                      weekday: 'short',
                      day: '2-digit',
                      month: '2-digit',
                      year: 'numeric',
                    })}
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-2">
                      {shifts.map((s) => (
                        <span key={s.id} className="rounded-full bg-surface-3 px-3 py-1 text-[13px] font-semibold text-primary">
                          {s.startTime} – {s.endTime}
                        </span>
                      ))}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  );
}
