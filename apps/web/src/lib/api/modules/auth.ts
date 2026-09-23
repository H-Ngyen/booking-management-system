import { apiClient } from '@/lib/client';
import { clearAccessToken, setAccessToken } from '@/lib/api/token-store';
import type { AuthUser } from '@/lib/types';

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

export const login = (userName: string, password: string) =>
  apiClient
    .post<LoginResponse>('/auth/login', { userName, password })
    .then((r) => {
      setAccessToken(r.data.accessToken);
      return r.data;
    });

export const logout = () => {
  clearAccessToken();
};

export const getMe = () => apiClient.get<AuthUser>('/auth/me').then((r) => r.data);
