import type { AuthUser, LoginDTO, RegisterDTO } from '../types/auth';

const BASE = '/api/auth';

async function post<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'include',   // required so the Set-Cookie response header is accepted
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  const text = await res.text();

  if (!res.ok) {
    try {
      const err = JSON.parse(text);
      throw new Error(err?.message ?? err ?? 'Request failed');
    } catch {
      throw new Error(text || 'Request failed');
    }
  }

  return (text ? JSON.parse(text) : null) as T;
}

export const authService = {
  // Server sets the httpOnly cookie; response body contains user info only (no token)
  login: (dto: LoginDTO) => post<AuthUser>(`${BASE}/login`, dto),
  register: (dto: RegisterDTO) => post<string>(`${BASE}/register`, dto),
};
