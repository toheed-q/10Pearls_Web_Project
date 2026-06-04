import type { UserSummary } from '../types/auth';
import { apiRequest } from './apiClient';

const BASE = '/api/admin';

export const adminService = {
  getUsers: () =>
    apiRequest<UserSummary[]>(`${BASE}/users`),

  getUser: (id: string) =>
    apiRequest<UserSummary>(`${BASE}/users/${id}`),

  updateRole: (id: string, role: string) =>
    apiRequest<{ message: string }>(`${BASE}/users/${id}/role`, {
      method: 'PUT',
      body: JSON.stringify({ role }),
    }),
};
