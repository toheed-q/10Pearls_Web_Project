import { Link } from 'react-router-dom';
import type { Task } from '../types/task';
import './TaskCard.css';

interface Props {
  task: Task;
  onEdit: (task: Task) => void;
  onDelete: (id: string) => void;
  showOwner?: boolean;
}

const STATUS_LABELS: Record<string, string> = {
  Pending:    'Pending',
  InProgress: 'In Progress',
  Completed:  'Completed',
};

// Safely converts any value to a lowercase string.
// Guards against numeric enums arriving from the backend if serializer is misconfigured.
const toStr = (v: unknown): string => String(v ?? '').toLowerCase();

export function TaskCard({ task, onEdit, onDelete, showOwner = false }: Props) {
  const status   = String(task.status   ?? '');
  const priority = String(task.priority ?? '');

  const isOverdue =
    status !== 'Completed' && new Date(task.dueDate) < new Date();

  return (
    <div className={`task-card ${toStr(status)} ${toStr(priority)}`}>
      <div className="task-card-header">
        <span className={`badge priority-${toStr(priority)}`}>
          {priority}
        </span>
        <span className={`badge status-${toStr(status)}`}>
          {STATUS_LABELS[status] ?? status}
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

      {showOwner && (
        <p className="task-owner">Owner: {task.ownerName || task.userId.slice(0, 8) + '…'}</p>
      )}

      <div className="task-actions">
        <Link className="btn-view" to={`/tasks/${task.id}`}>View</Link>
        <button className="btn-edit" onClick={() => onEdit(task)}>Edit</button>
        <button className="btn-delete" onClick={() => onDelete(task.id)}>Delete</button>
      </div>
    </div>
  );
}
