import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { taskHub } from '../services/taskHub';
import { HubEvents } from '../types/hubEvents';
import type { Task } from '../types/task';

interface HubCallbacks {
  onTaskCreated:       (task: Task) => void;
  onTaskUpdated:       (task: Task) => void;
  onTaskDeleted:       (taskId: string) => void;
  onTaskStatusChanged: (task: Task) => void;
}

export function useTaskHub(callbacks: HubCallbacks) {
  const [connectionState, setConnectionState] =
    useState<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);

  useEffect(() => {
    // Register all event listeners using constants — no magic strings
    taskHub.on(HubEvents.TaskCreated,       callbacks.onTaskCreated);
    taskHub.on(HubEvents.TaskUpdated,       callbacks.onTaskUpdated);
    taskHub.on(HubEvents.TaskDeleted,       callbacks.onTaskDeleted);
    taskHub.on(HubEvents.TaskStatusChanged, callbacks.onTaskStatusChanged);

    // Track reconnecting / reconnected state for the UI indicator
    taskHub.onreconnecting(() =>
      setConnectionState(signalR.HubConnectionState.Reconnecting));

    taskHub.onreconnected(() =>
      setConnectionState(signalR.HubConnectionState.Connected));

    taskHub.onclose(() =>
      setConnectionState(signalR.HubConnectionState.Disconnected));

    // Start connection only if not already started
    if (taskHub.state === signalR.HubConnectionState.Disconnected) {
      taskHub
        .start()
        .then(() => setConnectionState(signalR.HubConnectionState.Connected))
        .catch(err => console.error('SignalR connection failed:', err));
    } else {
      setConnectionState(taskHub.state);
    }

    return () => {
      // Remove only the listeners registered in this hook — don't stop the connection
      taskHub.off(HubEvents.TaskCreated,       callbacks.onTaskCreated);
      taskHub.off(HubEvents.TaskUpdated,       callbacks.onTaskUpdated);
      taskHub.off(HubEvents.TaskDeleted,       callbacks.onTaskDeleted);
      taskHub.off(HubEvents.TaskStatusChanged, callbacks.onTaskStatusChanged);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return { connectionState };
}
