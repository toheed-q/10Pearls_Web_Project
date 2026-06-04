import { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { taskService } from '../services/taskService';
import { taskHub } from '../services/taskHub';
import { HubEvents } from '../types/hubEvents';
import { TaskForm } from '../components/TaskForm';
import { ToastContainer } from '../components/ToastContainer';
import { useToast } from '../hooks/useToast';
import type { AppTaskStatus, CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import './TaskDetail.css';

// ── Label maps — no hardcoded display strings scattered in JSX ──
const STATUS_LABELS: Record<AppTaskStatus, string> = {
  Pending:    'Pending',
  InProgress: 'In Progress',
  Completed:  'Completed',
};

const ALL_STATUSES: AppTaskStatus[] = ['Pending', 'InProgress', 'Completed'];

export function TaskDetail() {
  const { id }   = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [task, setTask]         = useState<Task | null>(null);
  const [loading, setLoading]   = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [saving, setSaving]     = useState(false);

  // ── Initial fetch ────────────────────────────────────────────
  useEffect(() => {
    if (!id) return;
    fetchTask();
  }, [id]);

  async function fetchTask() {
    setLoading(true);
    try {
      const data = await taskService.getById(id!);
      setTask(data);
    } catch {
      setNotFound(true);
    } finally {
      setLoading(false);
    }
  }

  // ── SignalR — subscribe to events for THIS task only ─────────
  // useCallback so cleanup removes the exact same reference
  const onTaskUpdated = useCallback((updated: Task) => {
    if (updated.id === id) setTask(updated);
  }, [id]);

  const onTaskStatusChanged = useCallback((updated: Task) => {
    if (updated.id === id) setTask(updated);
  }, [id]);

  const onTaskDeleted = useCallback((deletedId: string) => {
    if (deletedId === id) {
      show('Task was deleted', 'error');
      setTimeout(() => navigate('/'), 1500);
    }
  }, [id, navigate, show]);

  useEffect(() => {
    taskHub.on(HubEvents.TaskUpdated,       onTaskUpdated);
    taskHub.on(HubEvents.TaskStatusChanged, onTaskStatusChanged);
    taskHub.on(HubEvents.TaskDeleted,       onTaskDeleted);

    return () => {
      taskHub.off(HubEvents.TaskUpdated,       onTaskUpdated);
      taskHub.off(HubEvents.TaskStatusChanged, onTaskStatusChanged);
      taskHub.off(HubEvents.TaskDeleted,       onTaskDeleted);
    };
  }, [onTaskUpdated, onTaskStatusChanged, onTaskDeleted]);

  // ── Status change ────────────────────────────────────────────
  async function handleStatusChange(newStatus: AppTaskStatus) {
    if (!task || newStatus === task.status) return;
    try {
      const updated = await taskService.update(task.id, { status: newStatus });
      setTask(updated);
      show('Status updated');
    } catch {
      show('Failed to update status', 'error');
    }
  }

  // ── Edit ─────────────────────────────────────────────────────
  async function handleEdit(dto: CreateTaskDTO) {
    if (!task) return;
    setSaving(true);
    try {
      const updateDto: UpdateTaskDTO = {
        title:       dto.title,
        description: dto.description,
        dueDate:     dto.dueDate,
        status:      dto.status,
        priority:    dto.priority,
      };
      const updated = await taskService.update(task.id, updateDto);
      setTask(updated);
      setShowEdit(false);
      show('Task updated');
    } finally {
      setSaving(false);
    }
  }

  // ── Delete ───────────────────────────────────────────────────
  async function handleDelete() {
    if (!task || !confirm('Delete this task? This cannot be undone.')) return;
    try {
      await taskService.delete(task.id);
      navigate('/');
    } catch {
      show('Failed to delete task', 'error');
    }
  }

  // ── Derived display helpers ──────────────────────────────────
  const isOverdue = task
    ? task.status !== 'Completed' && new Date(task.dueDate) < new Date()
    : false;

  // ── Render states ────────────────────────────────────────────
  if (loading) {
    return (
      <div className="td-page">
        <div className="td-loading">Loading task…</div>
      </div>
    );
  }

  if (notFound || !task) {
    return (
      <div className="td-page">
        <div className="td-not-found">
          <h2>Task not found</h2>
          <p>This task doesn't exist or you don't have access to it.</p>
          <Link to="/" className="td-back-link">← Back to Dashboard</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="td-page">

      {/* ── Back nav ── */}
      <Link to="/" className="td-back-link">← Back to Dashboard</Link>

      {/* ── Main card ── */}
      <div className="td-card">

        {/* ── Header ── */}
        <div className="td-header">
          <div className="td-badges">
            <span className={`badge priority-${task.priority.toLowerCase()}`}>
              {task.priority}
            </span>
            <span className={`badge status-${task.status.toLowerCase()}`}>
              {STATUS_LABELS[task.status]}
            </span>
            {isOverdue && <span className="badge badge-overdue">Overdue</span>}
          </div>
          <h1 className="td-title">{task.title}</h1>
        </div>

        {/* ── Description ── */}
        {task.description && (
          <div className="td-section">
            <span className="td-section-label">Description</span>
            <p className="td-description">{task.description}</p>
          </div>
        )}

        {/* ── Meta grid ── */}
        <div className="td-meta-grid">
          <div className="td-meta-item">
            <span className="td-meta-label">Due Date</span>
            <span className={`td-meta-value ${isOverdue ? 'overdue' : ''}`}>
              {new Date(task.dueDate).toLocaleDateString('en-US', {
                year: 'numeric', month: 'long', day: 'numeric',
              })}
            </span>
          </div>
          <div className="td-meta-item">
            <span className="td-meta-label">Created</span>
            <span className="td-meta-value">
              {new Date(task.createdAt).toLocaleString('en-US', {
                dateStyle: 'medium', timeStyle: 'short',
              })}
            </span>
          </div>
          <div className="td-meta-item">
            <span className="td-meta-label">Last Updated</span>
            <span className="td-meta-value">
              {new Date(task.updatedAt).toLocaleString('en-US', {
                dateStyle: 'medium', timeStyle: 'short',
              })}
            </span>
          </div>
        </div>

        {/* ── Status change ── */}
        <div className="td-section">
          <span className="td-section-label">Change Status</span>
          <div className="td-status-group">
            {ALL_STATUSES.map(s => (
              <button
                key={s}
                className={`td-status-btn ${task.status === s ? 'active' : ''} status-${s.toLowerCase()}`}
                onClick={() => handleStatusChange(s)}
                disabled={task.status === s}
              >
                {STATUS_LABELS[s]}
              </button>
            ))}
          </div>
        </div>

        {/* ── Actions ── */}
        <div className="td-actions">
          <button className="btn-primary" onClick={() => setShowEdit(true)}>
            ✏ Edit Task
          </button>
          <button className="td-btn-delete" onClick={handleDelete}>
            🗑 Delete Task
          </button>
        </div>

      </div>

      {/* ── Edit modal ── */}
      {showEdit && (
        <TaskForm
          initial={task}
          onSubmit={handleEdit}
          onCancel={() => setShowEdit(false)}
          loading={saving}
        />
      )}

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </div>
  );
}
