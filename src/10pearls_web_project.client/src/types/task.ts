export type AppTaskStatus = 'Pending' | 'InProgress' | 'Completed';
export type AppTaskPriority = 'Low' | 'Medium' | 'High';

export interface Task {
  id: string;
  title: string;
  description?: string;
  status: AppTaskStatus;
  priority: AppTaskPriority;
  dueDate: string;
  createdAt: string;
  updatedAt: string;
  userId: string;
}

export interface CreateTaskDTO {
  title: string;
  description?: string;
  dueDate: string;
  status: AppTaskStatus;
  priority: AppTaskPriority;
}

export interface UpdateTaskDTO {
  title?: string;
  description?: string;
  dueDate?: string;
  status?: AppTaskStatus;
  priority?: AppTaskPriority;
}
