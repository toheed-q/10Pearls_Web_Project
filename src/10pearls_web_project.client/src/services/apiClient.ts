// Centralized fetch wrapper — sends httpOnly auth cookie automatically
export async function apiRequest<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(url, {
    ...options,
    credentials: 'include',   // browser sends httpOnly cookie on every request
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  // Cookie expired or invalid — clear user state and redirect to login
  if (res.status === 401) {
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
