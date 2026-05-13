import { type FormEvent, useState } from 'react';
import type { AppTaskPriority, AppTaskStatus, CreateTaskDTO, Task } from '../types/task';
import './TaskForm.css';

interface Props {
  initial?: Task;
  onSubmit: (dto: CreateTaskDTO) => Promise<void>;
  onCancel: () => void;
  loading: boolean;
}

export function TaskForm({ initial, onSubmit, onCancel, loading }: Props) {
  const [title, setTitle] = useState(initial?.title ?? '');
  const [description, setDescription] = useState(initial?.description ?? '');
  const [dueDate, setDueDate] = useState(
    initial ? initial.dueDate.slice(0, 10) : ''
  );
  const [status, setStatus] = useState<AppTaskStatus>(initial?.status ?? 'Pending');
  const [priority, setPriority] = useState<AppTaskPriority>(initial?.priority ?? 'Medium');
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await onSubmit({ title, description: description || undefined, dueDate, status, priority });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Something went wrong');
    }
  }

  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal-card" onClick={e => e.stopPropagation()}>
        <h2>{initial ? 'Edit Task' : 'New Task'}</h2>
        {error && <p className="form-error">{error}</p>}
        <form onSubmit={handleSubmit}>
          <div className="form-field">
            <label>Title *</label>
            <input value={title} onChange={e => setTitle(e.target.value)} required maxLength={200} placeholder="Task title" />
          </div>
          <div className="form-field">
            <label>Description</label>
            <textarea value={description} onChange={e => setDescription(e.target.value)} maxLength={2000} rows={3} placeholder="Optional description" />
          </div>
          <div className="form-row">
            <div className="form-field">
              <label>Due Date *</label>
              <input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} required />
            </div>
            <div className="form-field">
              <label>Priority</label>
              <select value={priority} onChange={e => setPriority(e.target.value as AppTaskPriority)}>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
              </select>
            </div>
            <div className="form-field">
              <label>Status</label>
              <select value={status} onChange={e => setStatus(e.target.value as AppTaskStatus)}>
                <option value="Pending">Pending</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
              </select>
            </div>
          </div>
          <div className="form-actions">
            <button type="button" className="btn-secondary" onClick={onCancel}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={loading}>
              {loading ? 'Saving…' : initial ? 'Save Changes' : 'Create Task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
