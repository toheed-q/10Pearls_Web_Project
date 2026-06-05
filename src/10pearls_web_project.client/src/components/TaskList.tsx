import type { Task } from '../types/task';
import { TaskCard } from './TaskCard';
import './TaskList.css';

interface Props {
  tasks: Task[];
  onEdit: (task: Task) => void;
  onDelete: (id: string) => void;
  showOwner?: boolean;
}

export function TaskList({ tasks, onEdit, onDelete, showOwner = false }: Props) {
  if (tasks.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <rect x="3" y="5" width="18" height="16" rx="2"/>
            <path d="M3 10h18"/><path d="M8 3v4"/><path d="M16 3v4"/>
            <path d="M9 15h6"/><path d="M9 18h4"/>
          </svg>
        </div>
        <p className="empty-state-title">No tasks found</p>
        <span className="empty-state-hint">Create a new task or adjust your filters to see results.</span>
      </div>
    );
  }

  return (
    <div className="task-grid">
      {tasks.map(task => (
        <TaskCard key={task.id} task={task} onEdit={onEdit} onDelete={onDelete} showOwner={showOwner} />
      ))}
    </div>
  );
}
