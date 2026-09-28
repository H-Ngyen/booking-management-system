import { Button } from './button';

export function Pagination({
  page,
  totalPages,
  onChange,
}: {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}) {
  if (totalPages <= 1) return null;
  return (
    <nav className="flex items-center justify-center gap-2" aria-label="Phân trang">
      <Button variant="outline" size="sm" onClick={() => onChange(page - 1)} disabled={page <= 1}>
        Trước
      </Button>
      <span className="text-sm text-ink-3" aria-current="page">
        Trang {page} / {totalPages}
      </span>
      <Button variant="outline" size="sm" onClick={() => onChange(page + 1)} disabled={page >= totalPages}>
        Sau
      </Button>
    </nav>
  );
}
