'use client';

import { useState, type FormEvent } from 'react';
import { Pencil, Plus, Search } from 'lucide-react';
import { useCreateService, useServices, useUpdateService } from '@/hooks';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { getErrorMessage } from '@/lib/error-messages';
import type { ServiceItem } from '@/lib/types';
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
import { Textarea } from '@/components/ui/textarea';

const PAGE_SIZE = 10;

interface ServiceFormState {
  name: string;
  description: string;
  durationMinutes: string;
  price: string;
  isActive: boolean;
}

const EMPTY_FORM: ServiceFormState = {
  name: '',
  description: '',
  durationMinutes: '60',
  price: '0',
  isActive: true,
};

export default function AdminServicesPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<ServiceItem | null>(null);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState<ServiceFormState>(EMPTY_FORM);
  const [formError, setFormError] = useState('');

  const debouncedSearch = useDebouncedValue(search);
  const { data, isLoading, isError, error, refetch } = useServices({
    q: debouncedSearch || undefined,
    page,
    pageSize: PAGE_SIZE,
    activeOnly: false,
  });
  const createService = useCreateService();
  const updateService = useUpdateService();
  const saving = createService.isPending || updateService.isPending;

  const openCreate = () => {
    setForm(EMPTY_FORM);
    setFormError('');
    setCreating(true);
  };
  const openEdit = (service: ServiceItem) => {
    setForm({
      name: service.name,
      description: service.description ?? '',
      durationMinutes: String(service.durationMinutes),
      price: String(service.price),
      isActive: service.isActive,
    });
    setFormError('');
    setEditing(service);
  };
  const closeModal = () => {
    setCreating(false);
    setEditing(null);
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const duration = Number(form.durationMinutes);
    const price = Number(form.price);
    if (!form.name.trim()) return setFormError('Tên dịch vụ là bắt buộc.');
    if (!Number.isFinite(duration) || duration <= 0) return setFormError('Thời lượng phải lớn hơn 0.');
    if (!Number.isFinite(price) || price < 0) return setFormError('Giá không được âm.');
    setFormError('');
    const input = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      durationMinutes: duration,
      price,
      isActive: form.isActive,
    };
    if (editing) {
      updateService.mutate(
        { id: editing.id, patch: input },
        { onSuccess: () => setEditing(null), onError: (err) => setFormError(getErrorMessage(err)) },
      );
    } else {
      createService.mutate(input, {
        onSuccess: () => setCreating(false),
        onError: (err) => setFormError(getErrorMessage(err)),
      });
    }
  };

  const toggleActive = (service: ServiceItem) => {
    updateService.mutate({ id: service.id, patch: { isActive: !service.isActive } });
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-ink">Quản lý dịch vụ</h1>
          <p className="text-sm text-ink-3">Thêm, cập nhật, khóa hoặc mở lại dịch vụ.</p>
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
              aria-label="Tìm kiếm dịch vụ"
              className="w-56 pl-9"
            />
          </div>
          <Button onClick={openCreate}>
            <Plus />
            Thêm dịch vụ
          </Button>
        </div>
      </div>

      {isLoading && <TableSkeleton />}
      {isError && <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />}
      {data && data.items.length === 0 && <EmptyState title="Chưa có dịch vụ nào" />}
      {data && data.items.length > 0 && (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tên</TableHead>
                <TableHead>Thời lượng</TableHead>
                <TableHead>Giá</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Hành động</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((s) => (
                <TableRow key={s.id}>
                  <TableCell className="font-semibold">{s.name}</TableCell>
                  <TableCell>{s.durationMinutes} phút</TableCell>
                  <TableCell>{s.price.toLocaleString('vi-VN')}đ</TableCell>
                  <TableCell>
                    <Badge variant={s.isActive ? 'secondary' : 'outline'}>
                      {s.isActive ? 'Đang mở' : 'Đã khóa'}
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
        <DialogContent wide>
          <DialogHeader>
            <DialogTitle>{editing ? 'Cập nhật dịch vụ' : 'Thêm dịch vụ'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
            <Field label="Tên *" htmlFor="svc-name">
              <Input id="svc-name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            </Field>
            <Field label="Mô tả" htmlFor="svc-desc">
              <Textarea id="svc-desc" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} rows={3} />
            </Field>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Thời lượng (phút) *" htmlFor="svc-duration">
                <Input id="svc-duration" type="number" min={1} value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: e.target.value })} />
              </Field>
              <Field label="Giá *" htmlFor="svc-price">
                <Input id="svc-price" type="number" min={0} value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} />
              </Field>
            </div>
            <Field label="Trạng thái">
              <Select value={form.isActive ? '1' : '0'} onValueChange={(v) => setForm({ ...form, isActive: v === '1' })}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Đang mở</SelectItem>
                  <SelectItem value="0">Đã khóa</SelectItem>
                </SelectContent>
              </Select>
            </Field>
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
