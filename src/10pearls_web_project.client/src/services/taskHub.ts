import * as signalR from '@microsoft/signalr';

// Relative URL — Vite proxy forwards /hubs/* to the ASP.NET backend (ws:true in vite.config.ts)
export const taskHub = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/tasks', {
    // JWT sent via query string — required for WebSocket transport
    accessTokenFactory: () => localStorage.getItem('auth_token') ?? '',
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .configureLogging(signalR.LogLevel.Warning)
  .build();
