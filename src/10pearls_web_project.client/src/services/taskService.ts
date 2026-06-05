import type { CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import { apiRequest } from './apiClient';

const BASE = '/api/tasks';

export interface TaskStats {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
}

export const taskService = {
  getStats: () =>
    apiRequest<TaskStats>(`${BASE}/stats`),

  getAll: () =>
    apiRequest<Task[]>(BASE),

  getById: (id: string) =>
    apiRequest<Task>(`${BASE}/${id}`),

  create: (dto: CreateTaskDTO) =>
    apiRequest<Task>(BASE, { method: 'POST', body: JSON.stringify(dto) }),

  update: (id: string, dto: UpdateTaskDTO) =>
    apiRequest<Task>(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(dto) }),

  delete: (id: string) =>
    apiRequest<void>(`${BASE}/${id}`, { method: 'DELETE' }),

  exportCsv: async (): Promise<void> => {
    const token = localStorage.getItem('auth_token');
    const res = await fetch(`${BASE}/export/csv`, {
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    });

    if (res.status === 401) {
      localStorage.removeItem('auth_token');
      localStorage.removeItem('auth_user');
      window.location.href = '/signin';
      throw new Error('Session expired.');
    }

    if (!res.ok) throw new Error('Failed to export tasks');

    const blob = await res.blob();
    const disposition = res.headers.get('content-disposition') ?? '';
    const match = disposition.match(/filename[^;=\n]*=(['"]?)([^'"\n;]*)\1/);
    const fileName = match?.[2] ?? `tasks-export-${new Date().toISOString().slice(0, 10)}.csv`;

    const url = URL.createObjectURL(blob);
    const a   = document.createElement('a');
    a.href     = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  },
};
