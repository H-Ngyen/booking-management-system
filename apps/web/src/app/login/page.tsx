'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useState, type FormEvent } from 'react';
import { useAuthMe, useLogin } from '@/hooks/useAuth';
import { getErrorMessage } from '@/lib/error-messages';
import { DEMO_ACCOUNTS } from '@/lib/mock/data';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Field, FieldError } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Spinner } from '@/components/ui/feedback';

export default function LoginPage() {
  const router = useRouter();
  const { data: me, isLoading: checking } = useAuthMe();
  const login = useLogin();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [formError, setFormError] = useState('');

  useEffect(() => {
    if (me) router.replace(me.role === 'Admin' ? '/admin/bookings' : '/services');
  }, [me, router]);

  if (checking) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <Spinner />
      </div>
    );
  }
  if (me) return null;

  const canSubmit = userName.trim().length > 0 && password.length > 0 && !login.isPending;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    setFormError('');
    login.mutate(
      { userName: userName.trim(), password },
      {
        onSuccess: ({ user }) => router.replace(user.role === 'Admin' ? '/admin/bookings' : '/services'),
        onError: (error) => setFormError(getErrorMessage(error, 'Đăng nhập thất bại.')),
      },
    );
  };

  const fillDemo = (name: string, pw: string) => {
    setUserName(name);
    setPassword(pw);
    setFormError('');
  };

  return (
    <div className="flex min-h-dvh items-center justify-center bg-background p-4">
      <div className="w-full max-w-md space-y-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-xl">Đăng nhập</CardTitle>
            <CardDescription>Service Booking Management System</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4" noValidate>
              <Field label="Tên đăng nhập" htmlFor="username">
                <Input
                  id="username"
                  value={userName}
                  onChange={(e) => setUserName(e.target.value)}
                  autoComplete="username"
                  placeholder="vd. customer1"
                />
              </Field>
              <Field label="Mật khẩu" htmlFor="password">
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                />
              </Field>
              <FieldError message={formError} />
              <Button type="submit" disabled={!canSubmit} loading={login.isPending} className="w-full">
                Đăng nhập
              </Button>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Tài khoản demo</CardTitle>
            <CardDescription>Bấm để điền nhanh (mock, chưa đấu API).</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            {DEMO_ACCOUNTS.map((a) => (
              <button
                key={a.id}
                type="button"
                onClick={() => fillDemo(a.userName, a.password)}
                className="flex w-full items-center justify-between rounded-lg border border-line bg-surface px-3 py-2 text-left text-sm hover:bg-surface-2"
              >
                <span>
                  <span className="font-mono font-semibold text-ink">{a.userName}</span>
                  <span className="text-ink-3"> / {a.password}</span>
                </span>
                <span className="rounded-full bg-surface-3 px-2 py-0.5 text-xs font-semibold text-primary">
                  {a.role}
                </span>
              </button>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
