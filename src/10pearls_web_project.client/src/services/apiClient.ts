// Centralized fetch wrapper — attaches JWT, handles errors consistently
export async function apiRequest<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  const token = localStorage.getItem('auth_token');

  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  // Token expired or invalid — clear auth and reload to trigger redirect
  if (res.status === 401) {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    window.location.href = '/signin';
    throw new Error('Session expired. Please sign in again.');
  }

  const text = await res.text();

  if (!res.ok) {
    try {
      const err = JSON.parse(text);
      throw new Error(err?.message ?? 'Request failed');
    } catch {
      throw new Error(text || 'Request failed');
    }
  }

  return (text ? JSON.parse(text) : null) as T;
}
