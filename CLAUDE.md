# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CodeGrade is an ASP.NET Core 9.0 MVC application for automated programming code evaluation in educational settings. It supports multi-language code execution (C#, Python, Java, JavaScript, C++, PHP, Ruby, Go, Rust, and more) using Judge0 API for secure execution without requiring Docker.

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

### Judge0 API Setup
```bash
# No Docker needed! Just configure the API key in appsettings.json
# 1. Register at RapidAPI.com
# 2. Subscribe to Judge0 CE API (free tier: 1000 requests/day)
# 3. Copy your API key
# 4. Add to appsettings.json:
#    "Judge0": {
#      "ApiKey": "your-rapidapi-key-here"
#    }
```

## Architecture Overview

### Core Components
- **Controllers**: MVC controllers handling web requests (Account, Admin, Assignments, Grades, Home, Submissions)
- **Models**: Domain entities using Entity Framework Core (User, Student, Teacher, Assignment, Submission, etc.)
- **Services**: Business logic layer (Judge0CodeExecutorService, GradeCalculationService)
- **ViewModels**: Data transfer objects for views
- **Data**: ApplicationDbContext and database migrations

### Security Model
- **Authentication**: ASP.NET Core Identity with role-based access (Admin, Teacher, Student)
- **Code Execution**: Secure execution via Judge0 API with resource limits and isolation
- **Input Validation**: Comprehensive validation throughout controllers and models

### Database Schema
Key relationships:
- ApplicationUser → Student/Teacher (1:1)
- Student → ClassGroup (N:1)
- Assignment → TestCase (1:N)
- Student → Submission → Assignment (N:1:N)
- Submission → ExecutionResult (1:N)

## Judge0 Code Execution System

The application uses Judge0 API for secure code execution with:
- **Resource Limits**: Configurable time (30s default) and memory (128MB default) constraints
- **Language Support**: C#, Python, Java, JavaScript, C++, PHP, Ruby, Go, Rust, Swift, Kotlin, Scala, R, Dart
- **Security**: Isolated execution environment provided by Judge0
- **No Docker Required**: Works in shared hosting environments like SmarterASP.NET

## Key Configuration

### Database
Default connection string uses SQL Server Express. Update in `appsettings.json` for different environments.

### Judge0 API Settings
Configure in `appsettings.json`:
```json
{
  "Judge0": {
    "ApiKey": "your-rapidapi-key-here",
    "ApiHost": "judge0-ce.p.rapidapi.com",
    "BaseUrl": "https://judge0-ce.p.rapidapi.com"
  },
  "ExecutionLimits": {
    "DefaultTimeLimit": 30,
    "DefaultMemoryLimit": 128,
    "MaxSubmissionsPerHour": 10
  }
}
```

## Development Notes

### Code Execution Flow
1. Student submits code via AssignmentsController
2. Judge0CodeExecutorService sends code to Judge0 API
3. Code runs against test cases defined in Assignment
4. Results returned from Judge0 API
5. Results stored as ExecutionResult entities
6. GradeCalculationService computes final grade

### Supported Languages and IDs
- C#: 51 (.NET Core)
- Python: 71 (3.8.1)
- Java: 62 (OpenJDK 13.0.1)
- JavaScript: 63 (Node.js 12.14.0)
- C++: 54 (GCC 9.2.0)
- C: 50 (GCC 9.2.0)
- PHP: 68 (7.4.1)
- Ruby: 72 (2.7.0)
- Go: 60 (1.13.5)
- Rust: 73 (1.40.0)
- Swift: 83 (5.2.3)
- Kotlin: 78 (1.3.70)
- Scala: 81 (2.13.2)
- R: 80 (4.0.0)
- Dart: 87 (2.7.2)

### Current Limitations
- Requires internet connection for Judge0 API
- Rate limited by Judge0 API (1000 requests/day free tier)
- No comprehensive test suite exists
- Email confirmation system not implemented

### Role-Based Views
- **Admin**: User management, system overview
- **Teacher**: Assignment creation, grade management, submission review
- **Student**: Assignment list, submission portal, grade viewing

## Database Seeding

The application includes SeedData.cs for initial data setup including:
- Default admin user (admin@admin.com / Admin123!)

## Deployment

### SmarterASP.NET (Shared Hosting)
This application is optimized for shared hosting environments:
- No Docker required
- Minimal server requirements
- Easy deployment process
- Works with Judge0 API

### Deployment Steps
1. Get Judge0 API key from RapidAPI
2. Upload files to SmarterASP.NET
3. Configure database connection
4. Add API key to appsettings.json
5. Start the application

## Cost Considerations

- **Judge0 API**: Free up to 1000 requests/day
- **Paid Plan**: $10/month for 100,000 requests
- **Application**: Free (no Docker requirements)

## Troubleshooting

### Common Issues
1. **API Key Issues**: Ensure Judge0 API key is correctly configured
2. **Rate Limiting**: Monitor API usage to avoid hitting limits
3. **Network Issues**: Check internet connectivity for API calls
4. **Language Support**: Verify language ID is correct for Judge0 API

### Debugging
- Check application logs for API request/response details
- Monitor Judge0 API dashboard for usage statistics
- Test API connectivity with simple requests