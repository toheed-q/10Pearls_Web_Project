/**
 * SignalR event name constants — must match HubEvents.cs on the backend exactly.
 * Import this wherever you call hub.on() or hub.invoke().
 */
export const HubEvents = {
  TaskCreated:       'TaskCreated',
  TaskUpdated:       'TaskUpdated',
  TaskDeleted:       'TaskDeleted',
  TaskStatusChanged: 'TaskStatusChanged',
  UserRoleChanged:   'UserRoleChanged',
} as const;

export type HubEventName = typeof HubEvents[keyof typeof HubEvents];
