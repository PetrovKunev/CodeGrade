# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CodeGrade is an ASP.NET Core 9.0 MVC application for automated programming code evaluation in educational settings. It supports multi-language code execution (C#, Python, Java, JavaScript) using Docker containers for secure isolation.

## Common Commands

### Development
```bash
# Restore packages
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run

# Run with specific environment
dotnet run --environment Development
```

### Database Operations
```bash
# Update database with latest migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Reset database (drop and recreate)
dotnet ef database drop
dotnet ef database update
```

### Docker Code Runner
```bash
# Build the code runner Docker image
docker build -f Infrastructure/Docker/Dockerfile.CodeRunner -t codegrade/code-runner:latest .

# Test Docker container manually
docker run --rm -v "$(pwd)/test-code:/code" codegrade/code-runner:latest python /code/solution.py
```

## Architecture Overview

### Core Components
- **Controllers**: MVC controllers handling web requests (Account, Admin, Assignments, Grades, Home, Submissions)
- **Models**: Domain entities using Entity Framework Core (User, Student, Teacher, Assignment, Submission, etc.)
- **Services**: Business logic layer (DockerCodeExecutorService, GradeCalculationService)
- **ViewModels**: Data transfer objects for views
- **Data**: ApplicationDbContext and database migrations

### Security Model
- **Authentication**: ASP.NET Core Identity with role-based access (Admin, Teacher, Student)
- **Code Execution**: Isolated Docker containers with resource limits and non-root execution
- **Input Validation**: Comprehensive validation throughout controllers and models

### Database Schema
Key relationships:
- ApplicationUser → Student/Teacher (1:1)
- Student → ClassGroup (N:1)
- Assignment → TestCase (1:N)
- Student → Submission → Assignment (N:1:N)
- Submission → ExecutionResult (1:N)

## Docker Code Execution System

The application uses Docker containers for secure code execution with:
- **Resource Limits**: Configurable time (30s default) and memory (128MB default) constraints
- **Language Support**: C#, Python, Java, JavaScript with unified execution scripts
- **Security**: Non-root user, read-only filesystem, network isolation
- **Scripts Location**: `Infrastructure/Docker/scripts/run-{language}.sh`

## Key Configuration

### Database
Default connection string uses SQL Server Express. Update in `appsettings.json` for different environments.

### Docker Settings
Configure in `appsettings.json`:
```json
{
  "Docker": {
    "ImageName": "codegrade/code-runner:latest",
    "NetworkName": "bridge"
  },
  "ExecutionLimits": {
    "TimeoutSeconds": 30,
    "MemoryLimitMB": 128,
    "MaxSubmissionsPerMinute": 10
  }
}
```

## Development Notes

### Code Execution Flow
1. Student submits code via AssignmentsController
2. DockerCodeExecutorService creates isolated container
3. Code runs against test cases defined in Assignment
4. Results stored as ExecutionResult entities
5. GradeCalculationService computes final grade

### Current Limitations
- Docker execution integration is marked as TODO in AssignmentsController:732
- No comprehensive test suite exists
- Email confirmation system not implemented

### Role-Based Views
- **Admin**: User management, system overview
- **Teacher**: Assignment creation, grade management, submission review
- **Student**: Assignment list, submission portal, grade viewing

## Database Seeding

The application includes SeedData.cs for initial data setup including:
- Default admin user (admin@admin.com / Admin123!)
- Sample class groups and subject modules
- Test teacher and student accounts

## Testing Strategy

Currently no automated tests exist. When implementing tests:
- Use in-memory database for integration tests
- Mock DockerCodeExecutorService for unit tests
- Test Docker containers separately from web application logic