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
};
