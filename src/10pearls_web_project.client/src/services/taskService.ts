import type { CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import { apiRequest } from './apiClient';

const BASE = '/api/tasks';

export interface TaskStats {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
}

export interface PagedTasksResponse {
  items: Task[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TaskQueryParams {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sortOrder?: string;
}

export const taskService = {
  getStats: () =>
    apiRequest<TaskStats>(`${BASE}/stats`),

  getAll: (params: TaskQueryParams = {}) => {
    const qs = new URLSearchParams();
    if (params.page)      qs.set('page',      String(params.page));
    if (params.pageSize)  qs.set('pageSize',   String(params.pageSize));
    if (params.status)    qs.set('status',     params.status);
    if (params.search)    qs.set('search',     params.search);
    if (params.sortOrder) qs.set('sortOrder',  params.sortOrder);
    const query = qs.toString();
    return apiRequest<PagedTasksResponse>(`${BASE}${query ? '?' + query : ''}`);
  },

  getById: (id: string) =>
    apiRequest<Task>(`${BASE}/${id}`),

  create: (dto: CreateTaskDTO) =>
    apiRequest<Task>(BASE, { method: 'POST', body: JSON.stringify(dto) }),

  update: (id: string, dto: UpdateTaskDTO) =>
    apiRequest<Task>(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(dto) }),

  delete: (id: string) =>
    apiRequest<void>(`${BASE}/${id}`, { method: 'DELETE' }),

  exportCsv: async (): Promise<void> => {
    const res = await fetch(`${BASE}/export/csv`, {
      credentials: 'include',  // httpOnly cookie sent automatically
    });

    if (res.status === 401) {
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
