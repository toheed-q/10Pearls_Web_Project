import * as signalR from '@microsoft/signalr';

// Relative URL — Vite proxy forwards /hubs/* to the ASP.NET backend (ws:true in vite.config.ts).
// The httpOnly auth cookie is sent automatically on the negotiate handshake.
export const taskHub = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/tasks', {
    withCredentials: true,  // ensures the httpOnly cookie is included in WS negotiation
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .configureLogging(signalR.LogLevel.Warning)
  .build();
