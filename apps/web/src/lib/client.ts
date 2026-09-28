import axios, { AxiosError } from 'axios';
import { getBaseUrl } from './api/base-url';
import { clearAccessToken, getAccessToken } from './api/token-store';

export type ApiRequestConfig = {
  params?: object;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  data?: unknown;
};

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly details?: unknown;

  constructor(status: number, message: string, code?: string, details?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

function toApiError(error: AxiosError): ApiError {
  const status = error.response?.status ?? 0;
  const data = error.response?.data as Record<string, unknown> | undefined;
  const message =
    (typeof data?.message === 'string' && data.message) ||
    (typeof data?.error === 'string' && data.error) ||
    error.message ||
    `Request failed with status ${status}`;
  const code = typeof data?.code === 'string' ? data.code : undefined;
  return new ApiError(status, message, code, data);
}

const axiosInstance = axios.create();

axiosInstance.interceptors.request.use((config) => {
  config.baseURL = getBaseUrl();
  const token = getAccessToken();
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401 && typeof window !== 'undefined') {
      clearAccessToken();
      if (!window.location.pathname.startsWith('/login')) {
        // Full reload is intentional: the axios interceptor runs outside React,
        // so useRouter() is unavailable here.
        // eslint-disable-next-line @next/next/no-location-assign-relative-destination
        window.location.href = '/login';
      }
    }
    return Promise.reject(toApiError(error));
  },
);

export const apiClient = {
  get: <T>(path: string, config?: ApiRequestConfig) =>
    axiosInstance.get<T>(path, config).then((r) => ({ data: r.data })),
  post: <T>(path: string, body?: unknown, config?: ApiRequestConfig) =>
    axiosInstance.post<T>(path, body, config).then((r) => ({ data: r.data })),
  put: <T>(path: string, body?: unknown, config?: ApiRequestConfig) =>
    axiosInstance.put<T>(path, body, config).then((r) => ({ data: r.data })),
  patch: <T>(path: string, body?: unknown, config?: ApiRequestConfig) =>
    axiosInstance.patch<T>(path, body, config).then((r) => ({ data: r.data })),
  delete: <T>(path: string, config?: ApiRequestConfig) =>
    axiosInstance.delete<T>(path, config).then((r) => ({ data: r.data })),
};
