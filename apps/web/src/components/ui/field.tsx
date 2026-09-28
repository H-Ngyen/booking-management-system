import type { ReactNode } from 'react';
import { Label } from './label';

export function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return <p className="mt-1 text-[13px] text-destructive">{message}</p>;
}

export function Field({ label, htmlFor, error, children }: { label: string; htmlFor?: string; error?: string; children: ReactNode }) {
  return (
    <div>
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      <FieldError message={error} />
    </div>
  );
}
