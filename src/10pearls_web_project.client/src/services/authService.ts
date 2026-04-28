import type { AuthResponse, LoginDTO, RegisterDTO } from '../types/auth';

const BASE = '/api/auth';

async function post<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  const text = await res.text();

  if (!res.ok) {
    // try to parse as JSON { message }, fallback to raw text
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
  login: (dto: LoginDTO) => post<AuthResponse>(`${BASE}/login`, dto),
  register: (dto: RegisterDTO) => post<string>(`${BASE}/register`, dto),
};
