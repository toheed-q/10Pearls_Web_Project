import type { Task } from '../types/task';
import './TaskCard.css';

interface Props {
  task: Task;
  onEdit: (task: Task) => void;
  onDelete: (id: string) => void;
}

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pending',
  InProgress: 'In Progress',
  Completed: 'Completed',
};

export function TaskCard({ task, onEdit, onDelete }: Props) {
  const isOverdue =
    task.status !== 'Completed' && new Date(task.dueDate) < new Date();

  return (
    <div className={`task-card ${task.status.toLowerCase()}`}>
      <div className="task-card-header">
        <span className={`badge priority-${task.priority.toLowerCase()}`}>
          {task.priority}
        </span>
        <span className={`badge status-${task.status.toLowerCase()}`}>
          {STATUS_LABELS[task.status]}
        </span>
      </div>

      <h3 className="task-title">{task.title}</h3>

      {task.description && (
        <p className="task-description">{task.description}</p>
      )}

      <p className={`task-due ${isOverdue ? 'overdue' : ''}`}>
        Due: {new Date(task.dueDate).toLocaleDateString()}
        {isOverdue && ' — Overdue'}
      </p>

      <div className="task-actions">
        <button className="btn-edit" onClick={() => onEdit(task)}>Edit</button>
        <button className="btn-delete" onClick={() => onDelete(task.id)}>Delete</button>
      </div>
    </div>
  );
}
