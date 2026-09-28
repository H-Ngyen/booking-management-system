import { apiClient } from '@/lib/client';
import { clearAccessToken, setAccessToken } from '@/lib/api/token-store';
import type { AuthUser } from '@/lib/types';

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

// Backend POST /v1/auth/login returns the bare JWT string; the user profile
// comes from GET /v1/auth/me. Kept as one call so the login page is unchanged.
export const login = async (userName: string, password: string): Promise<LoginResponse> => {
  const { data: accessToken } = await apiClient.post<string>('/v1/auth/login', {
    userName,
    password,
  });
  setAccessToken(accessToken);
  const user = await getMe();
  return { accessToken, user };
};

export const logout = () => {
  clearAccessToken();
};

export const getMe = () => apiClient.get<AuthUser>('/v1/auth/me').then((r) => r.data);
