'use client';

// JWT access-token store. Kept outside React state so the axios interceptor
// (a module singleton) can read/clear it without hook context.

const STORAGE_KEY = 'booking.accessToken';

let inMemoryToken: string | null = null;

export function getAccessToken(): string | null {
  if (inMemoryToken) return inMemoryToken;
  if (typeof window === 'undefined') return null;
  return window.localStorage.getItem(STORAGE_KEY);
}

export function setAccessToken(token: string): void {
  inMemoryToken = token;
  if (typeof window !== 'undefined') window.localStorage.setItem(STORAGE_KEY, token);
}

export function clearAccessToken(): void {
  inMemoryToken = null;
  if (typeof window !== 'undefined') window.localStorage.removeItem(STORAGE_KEY);
}
