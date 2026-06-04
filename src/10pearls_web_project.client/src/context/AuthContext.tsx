import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import type { AuthUser } from '../types/auth';

interface AuthContextType {
  user: AuthUser | null;
  token: string | null;
  isAdmin: boolean;
  setAuth: (user: AuthUser, token: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

/**
 * Decodes the role from a JWT payload.
 * ASP.NET Core serialises ClaimTypes.Role as the full Microsoft URI:
 *   "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
 * We check both that and the short key "role" as a fallback.
 */
function decodeRoleFromToken(token: string): string {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const ROLE_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    const raw = payload[ROLE_URI] ?? payload['role'] ?? '';
    // If multiple roles are present they come as an array — take first
    return Array.isArray(raw) ? raw[0] : raw;
  } catch {
    return '';
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const stored = localStorage.getItem('auth_user');
    return stored ? JSON.parse(stored) : null;
  });
  const [token, setToken] = useState<string | null>(
    () => localStorage.getItem('auth_token')
  );

  // Role is always decoded live from the JWT — never trust stale localStorage value
  const role    = token ? decodeRoleFromToken(token) : (user?.role ?? '');
  const isAdmin = role === 'Admin';

  const setAuth = useCallback((u: AuthUser, t: string) => {
    // Overwrite role in the user object with what the JWT actually says
    const liveRole = decodeRoleFromToken(t);
    const userWithRole: AuthUser = { ...u, role: liveRole };

    setUser(userWithRole);
    setToken(t);
    localStorage.setItem('auth_user',  JSON.stringify(userWithRole));
    localStorage.setItem('auth_token', t);
  }, []);

  const logout = useCallback(() => {
    setUser(null);
    setToken(null);
    localStorage.removeItem('auth_user');
    localStorage.removeItem('auth_token');
  }, []);

  return (
    <AuthContext.Provider value={{ user, token, isAdmin, setAuth, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
