import type { Task } from '../types/task';
import { TaskCard } from './TaskCard';
import './TaskList.css';

interface Props {
  tasks: Task[];
  onEdit: (task: Task) => void;
  onDelete: (id: string) => void;
}

export function TaskList({ tasks, onEdit, onDelete }: Props) {
  if (tasks.length === 0) {
    return (
      <div className="empty-state">
        <p>No tasks found.</p>
        <span>Create your first task to get started!</span>
      </div>
    );
  }

  return (
    <div className="task-grid">
      {tasks.map(task => (
        <TaskCard key={task.id} task={task} onEdit={onEdit} onDelete={onDelete} />
      ))}
    </div>
  );
}
