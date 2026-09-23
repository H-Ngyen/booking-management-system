// Backend base URL resolver. Browser requests go straight to the public API
// origin. NEXT_PUBLIC_API_URL is inlined at build time; SERVER_API_URL stays
// for server-side calls. No version prefix — the API serves under /api/... .

const PRODUCTION_API_URL = 'https://booking-api.example.com/api';
const DEV_API_URL = 'http://localhost:5000/api';

function normalize(raw: string | undefined, fallback: string): string {
  const trimmed = (raw || fallback).replace(/\/+$/, '');
  if (!trimmed.startsWith('http')) {
    throw new Error(
      `[base-url] API base URL must be absolute, got: ${trimmed} — set NEXT_PUBLIC_API_URL`,
    );
  }
  return trimmed;
}

export function getBaseUrl(): string {
  if (typeof window !== 'undefined') {
    return normalize(process.env.NEXT_PUBLIC_API_URL, DEV_API_URL);
  }
  const serverDefault = process.env.NODE_ENV === 'development' ? DEV_API_URL : PRODUCTION_API_URL;
  return normalize(process.env.SERVER_API_URL, serverDefault);
}
