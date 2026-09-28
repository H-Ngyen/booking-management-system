'use client';

import { useState, type FormEvent } from 'react';
import { Pencil, Plus, Search } from 'lucide-react';
import { useCreateStaff, useStaffs, useUpdateStaff } from '@/hooks';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { getErrorMessage } from '@/lib/error-messages';
import type { Staff } from '@/lib/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { EmptyState, ErrorState, TableSkeleton } from '@/components/ui/feedback';
import { Field, FieldError } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Pagination } from '@/components/ui/pagination';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

const PAGE_SIZE = 10;

interface StaffFormState {
  fullName: string;
  email: string;
  isActive: boolean;
}

const EMPTY_FORM: StaffFormState = {
  fullName: '',
  email: '',
  isActive: true,
};

function isValidEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

export default function AdminStaffsPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<Staff | null>(null);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState<StaffFormState>(EMPTY_FORM);
  const [formError, setFormError] = useState('');

  const debouncedSearch = useDebouncedValue(search);
  const { data, isLoading, isError, error, refetch } = useStaffs(false, {
    search: debouncedSearch || undefined,
    page,
    pageSize: PAGE_SIZE,
  });
  const createStaff = useCreateStaff();
  const updateStaff = useUpdateStaff();
  const saving = createStaff.isPending || updateStaff.isPending;

  const openCreate = () => {
    setForm(EMPTY_FORM);
    setFormError('');
    setCreating(true);
  };
  const openEdit = (staff: Staff) => {
    setForm({ fullName: staff.fullName, email: staff.email, isActive: staff.isActive });
    setFormError('');
    setEditing(staff);
  };
  const closeModal = () => {
    setCreating(false);
    setEditing(null);
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!form.fullName.trim()) return setFormError('Tên nhân viên là bắt buộc.');
    if (!form.email.trim()) return setFormError('Email là bắt buộc.');
    if (!isValidEmail(form.email.trim())) return setFormError('Email không hợp lệ.');
    setFormError('');
    if (editing) {
      updateStaff.mutate(
        {
          id: editing.id,
          input: { fullName: form.fullName.trim(), email: form.email.trim(), isActive: form.isActive },
        },
        { onSuccess: () => setEditing(null), onError: (err) => setFormError(getErrorMessage(err)) },
      );
    } else {
      createStaff.mutate(
        { fullName: form.fullName.trim(), email: form.email.trim() },
        {
          onSuccess: () => setCreating(false),
          onError: (err) => setFormError(getErrorMessage(err)),
        },
      );
    }
  };

  const toggleActive = (staff: Staff) => {
    updateStaff.mutate({
      id: staff.id,
      input: { fullName: staff.fullName, email: staff.email, isActive: !staff.isActive },
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-ink">Quản lý nhân viên</h1>
          <p className="text-sm text-ink-3">Thêm, cập nhật, khóa hoặc mở lại nhân viên. Nhân viên bị khóa không nhận booking mới.</p>
        </div>
        <div className="flex gap-2">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-ink-4" />
            <Input
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder="Tìm kiếm…"
              aria-label="Tìm kiếm nhân viên"
              className="w-56 pl-9"
            />
          </div>
          <Button onClick={openCreate}>
            <Plus />
            Thêm nhân viên
          </Button>
        </div>
      </div>

      {isLoading && <TableSkeleton />}
      {isError && <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />}
      {data && data.items.length === 0 && <EmptyState title="Chưa có nhân viên nào" />}
      {data && data.items.length > 0 && (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tên</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Hành động</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((s) => (
                <TableRow key={s.id}>
                  <TableCell className="font-semibold">{s.fullName}</TableCell>
                  <TableCell>{s.email}</TableCell>
                  <TableCell>
                    <Badge variant={s.isActive ? 'secondary' : 'outline'}>
                      {s.isActive ? 'Đang làm' : 'Đã khóa'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2">
                      <Button variant="outline" size="sm" onClick={() => openEdit(s)}>
                        <Pencil />
                        Sửa
                      </Button>
                      <Button variant="outline" size="sm" onClick={() => toggleActive(s)}>
                        {s.isActive ? 'Khóa' : 'Mở lại'}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
      {data && <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />}

      <Dialog open={creating || editing != null} onOpenChange={(open) => !open && closeModal()}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? 'Cập nhật nhân viên' : 'Thêm nhân viên'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
            <Field label="Họ tên *" htmlFor="staff-name">
              <Input id="staff-name" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
            </Field>
            <Field label="Email *" htmlFor="staff-email">
              <Input
                id="staff-email"
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </Field>
            {editing && (
              <Field label="Trạng thái">
                <Select value={form.isActive ? '1' : '0'} onValueChange={(v) => setForm({ ...form, isActive: v === '1' })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">Đang làm</SelectItem>
                    <SelectItem value="0">Đã khóa</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            )}
            <FieldError message={formError} />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={closeModal}>
                Đóng
              </Button>
              <Button type="submit" loading={saving}>
                Lưu
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
