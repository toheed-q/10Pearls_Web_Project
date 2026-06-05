# Task Management System - Enterprise Full-Stack Application

## 📋 Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture & Technology Stack](#architecture--technology-stack)
3. [Prerequisites](#prerequisites)
4. [Project Structure](#project-structure)
5. [Environment Setup & Configuration](#environment-setup--configuration)
6. [Build & Run Instructions](#build--run-instructions)
7. [Database Setup & Migrations](#database-setup--migrations)
8. [API Documentation](#api-documentation)
9. [Key Features](#key-features)
10. [Authentication & Authorization](#authentication--authorization)
11. [Real-Time Communication](#real-time-communication)
12. [Deployment Guide](#deployment-guide)
13. [Testing](#testing)
14. [Troubleshooting](#troubleshooting)
15. [Contributing Guidelines](#contributing-guidelines)
16. [License](#license)

---

## 🎯 Project Overview
A comprehensive, enterprise-grade Task Management System built with modern software development practices. This full-stack application enables teams to collaborate efficiently with real-time updates, role-based access control, and robust security implementations. The system supports task tracking, user management, and live notifications through SignalR, providing a seamless collaborative experience.

**Key Business Value:**
- Streamlined task workflows for team productivity
- Real-time collaboration capabilities
- Role-based security and access control
- Scalable architecture supporting enterprise growth
- Comprehensive logging and monitoring

---

## 🏗️ Architecture & Technology Stack

### Backend (.NET 10.0)
- **Framework:** ASP.NET Core Web API
- **Database:** SQL Server with Entity Framework Core 10.0.6
- **Authentication:** JWT Bearer Authentication with ASP.NET Core Identity
- **Real-Time:** ASP.NET Core SignalR for WebSocket communication
- **Logging:** Serilog with file and console sinks
- **API Documentation:** Swagger/OpenAPI with Swashbuckle
- **Architecture Pattern:** Clean Architecture with separation of concerns
- **ORM:** Entity Framework Core with Code-First migrations

### Frontend (React 19.2.5 + TypeScript)
- **Framework:** React 19 with TypeScript
- **Build Tool:** Vite 8.0.9 for fast development and optimized builds
- **Routing:** React Router DOM v7.14.2
- **Real-Time Client:** @microsoft/signalr v10.0.0
- **Linting:** ESLint with React hooks and refresh plugins
- **Styling:** Modern CSS with responsive design
- **State Management:** React Context API for authentication state

---

## 📋 Prerequisites

### Required Software
| Software | Version | Download Link |
|----------|---------|---------------|
| .NET SDK | 10.0 or later | [Download .NET](https://dotnet.microsoft.com/download) |
| Node.js | 20.x or later | [Download Node.js](https://nodejs.org/) |
| SQL Server | 2019 or later (Express/Developer/Enterprise) | [Download SQL Server](https://www.microsoft.com/sql-server) |
| Visual Studio 2022 | Version 17.10 or later | [Download Visual Studio](https://visualstudio.microsoft.com/) |
| Git | Latest | [Download Git](https://git-scm.com/) |

### Verify Installations
```bash
# Verify .NET SDK
dotnet --version

# Verify Node.js and npm
node --version
npm --version

# Verify SQL Server connection (via SSMS or sqlcmd)
sqlcmd -S your-server-name -E

## ⚙️ Environment Setup & Configuration

### 1. Clone the Repository
```bash
git clone <repository-url>
cd TaskManagementSystem
```

### 2. Backend Configuration (`appsettings.json`)

Update the connection string in `src/TaskManagement.Server/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=TaskManagement_db;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_STRONG_SECRET_KEY_MIN_32_CHARS_LONG_123!@#",
    "Issuer": "TaskManagementAPI",
    "Audience": "TaskManagementClient",
    "DurationInMinutes": 60
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### 3. Environment Variables for Production
Create `appsettings.Production.json` for production deployments:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD-SQL;Database=TaskManagement_prod;User Id=prod_user;Password=PROD_PASSWORD;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "PRODUCTION_SECRET_KEY_FROM_CLOUD_KEY_VAULT",
    "DurationInMinutes": 60
  }
}
```

### 4. Frontend Configuration
The Vite proxy is already configured in `vite.config.ts` to forward API and Hub requests to the backend. Default frontend URL: `https://localhost:7633`

---

## 🚀 Build & Run Instructions

### Option 1: Using Visual Studio 2022 (Recommended)
1. Open `src/TaskManagement.slnx` in Visual Studio
2. Set both **Server** and **Client** projects as startup projects
3. Ensure "Docker" is not enabled for this solution
4. Press **F5** to run with debugging, or **Ctrl+F5** to run without debugging
5. Browser will automatically open to the application URL

### Option 2: Command Line Execution

#### Step 1: Install Frontend Dependencies
```powershell
# From repository root
cd src/TaskManagement.Client
npm install
```

#### Step 2: Start the Backend Server
```powershell
# Open new terminal window
cd src/TaskManagement.Server
dotnet restore
dotnet build
dotnet run --launch-profile https
```
Backend will start on: `https://localhost:7063` and `http://localhost:5600`

#### Step 3: Start the Frontend Development Server
```powershell
# From the client directory
npm run dev
```
Frontend will be available at: `https://localhost:7633`

### Verify Both Services Running
- **Swagger UI:** https://localhost:7063/swagger
- **Frontend App:** https://localhost:7633
- **Backend Health:** https://localhost:7063/weatherforecast (test endpoint)

---

## 🗄️ Database Setup & Migrations

### 1. Create Database and Apply Migrations
```powershell
cd src/TaskManagement.Server

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migrations to database
dotnet ef database update
```

### 2. Database Seeding
The application automatically seeds:
- Default admin user (email: admin@example.com, password: Admin@123)
- Basic role structure (Admin, User)
- Sample tasks for demonstration

### 3. Entity Framework Core Tools Installation
If EF Core tools are not installed:
```powershell
dotnet tool install --global dotnet-ef
```

### 4. Common Database Commands
```powershell
# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove

# Revert all migrations
dotnet ef database update 0
```

---

## 📚 API Documentation

### Swagger/OpenAPI
Access the interactive API documentation at:
- **Development:** https://localhost:7063/swagger

### Core API Endpoints

#### Authentication (`/api/auth`)
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login (returns JWT token)
- `GET /api/auth/me` - Get current user profile
- `PUT /api/auth/me` - Update user profile

#### Tasks (`/api/tasks`)
- `GET /api/tasks` - Get all tasks (auth required)
- `POST /api/tasks` - Create new task
- `GET /api/tasks/{id}` - Get specific task
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task

#### Admin (`/api/admin`)
- `GET /api/admin/users` - Get all users (Admin only)
- `PUT /api/admin/users/{id}/role` - Update user role (Admin only)

### Complete API Specification
All endpoints include comprehensive request/response schemas in Swagger UI with:
- Authentication requirements
- Request body validation
- Response status codes
- Example requests and responses

---

## ✨ Key Features

### 📊 Task Management
- Create, read, update, and delete tasks
- Task status tracking: Pending → In Progress → Completed
- Priority levels and due date management
- Task assignment to team members
- Real-time task updates across all connected clients

### 👥 User Management
- Secure user registration and authentication
- Role-based access control (Admin/User roles)
- User profile management
- Administrative dashboard for user oversight
- Password security with industry-standard hashing

### 🔔 Real-Time Features (SignalR)
- Live task updates pushed to all clients
- Instant notifications for task changes
- WebSocket connection with automatic reconnection
- Fallback transports for network reliability
- Connection state management

### 🎨 Modern UI/UX
- Responsive design for all screen sizes
- Dark/light theme support
- Toast notifications for user feedback
- Intuitive navigation with React Router
- Clean, professional interface

---

## 🔐 Authentication & Authorization

### JWT Authentication Flow
1. User submits credentials to `/api/auth/login`
2. Server validates and returns JWT access token
3. Token is automatically included in subsequent API requests
4. Token expires after configured duration (default: 60 minutes)

### Authorization Policies
- **Authenticated Users:** Access to task management features
- **Admin Role:** Full system access including user management
- **Regular Users:** Access to their assigned tasks and profile
- Policy-based authorization on all protected endpoints

### Security Implemented
- HTTPS enforcement in production
- CORS policy configured for SignalR compatibility
- SQL injection protection via EF Core parameterization
- XSS protection through React's built-in escaping
- CSRF protection configured
- Password hashing with ASP.NET Core Identity

---

## 🔌 Real-Time Communication (SignalR)

### Hub Endpoint
- **Task Hub:** `/hubs/tasks` - Main real-time communication hub

### Supported Events
- `TaskUpdated` - Broadcast when any task is modified
- `TaskCreated` - Broadcast when new task is created
- `TaskDeleted` - Broadcast when task is removed
- Receive live updates without page refresh

### Client Reconnection Logic
The SignalR client implements exponential backoff:
- First retry: 0 seconds
- Second retry: 2 seconds
- Third retry: 5 seconds
- Fourth retry: 10 seconds
- Maximum retry: 30 seconds

---

## 🚢 Deployment Guide

### Production Build Commands

#### Build Frontend for Production
```powershell
cd src/TaskManagement.Client
npm run build
```
Outputs to `dist/` directory with optimized assets.

#### Publish .NET Backend
```powershell
cd src/TaskManagement.Server
dotnet publish -c Release -o ./publish
```

### IIS Deployment Steps
1. Install IIS with ASP.NET Core hosting bundle
2. Copy publish folder contents to IIS web root
3. Create website in IIS Manager pointing to publish folder
4. Configure SSL certificate for HTTPS
5. Update connection strings in `appsettings.production.json`
6. Ensure application pool is set to No Managed Code

### Cloud Deployment (Azure/AWS/GCP)
1. Create new application service in your cloud provider
2. Configure deployment from your repository
3. Set up managed SQL database service
4. Configure application settings in cloud portal
5. Enable application monitoring and logging
6. Configure custom domain and SSL certificate

---

## 🧪 Testing

### Run Unit Tests
```powershell
cd src/TaskManagement.Test
dotnet test
```

### Frontend Testing
```powershell
cd src/TaskManagement.Client
npm run lint       # Run ESLint
npm run build      # Verify production build succeeds
```

### Post-Deployment Verification Checklist
- [ ] Application loads successfully
- [ ] User registration and login work
- [ ] API endpoints respond correctly
- [ ] SignalR connections establish
- [ ] Database operations function
- [ ] HTTPS redirect works
- [ ] Static files load properly
- [ ] Error handling functions as expected

---

## ❗ Troubleshooting

### Common Issues & Solutions

#### 1. SQL Server Connection Failed
**Symptom:** `A network-related or instance-specific error occurred`
**Solution:**
- Verify SQL Server is running
- Check connection string server name matches
- Enable TCP/IP in SQL Server Configuration Manager
- Verify Windows authentication permissions

#### 2. Node Modules Not Found
**Symptom:** `npm install` fails or modules missing
**Solution:**
```powershell
rm -rf node_modules
rm package-lock.json
npm install
```

#### 3. SignalR Connection Failed
**Symptom:** WebSocket connection fails to establish
**Solution:**
- Verify CORS policy includes frontend URL
- Check that `ws: true` is set in vite.config.ts proxy
- Ensure JWT token is being sent correctly
- Verify backend CORS allows credentials

#### 4. JWT Token Validation Failed
**Symptom:** 401 Unauthorized responses
**Solution:**
- Verify JWT key matches on server
- Check token expiration settings
- Ensure clock synchronization between client/server
- Verify token is properly included in Authorization header

#### 5. SPA Proxy Not Working
**Symptom:** Frontend can't reach backend API
**Solution:**
- Verify both projects are set as startup projects
- Check ports in launchSettings.json match configuration
- Ensure SPA proxy assembly is correctly referenced
- Clear browser cache and restart debugging

### Logging & Debugging
- Server logs stored in `src/TaskManagement.Server/Logs/`
- Serilog captures all errors with context
- Browser developer tools for frontend debugging
- Enable detailed logging in development environment
