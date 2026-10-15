# SmartGym Management System

A web-based gym management application developed as an academic project using **ASP.NET Core MVC** and **SQL Server**. It brings together day-to-day gym administration and member-facing features in one application.

## Project Overview

SmartGym Management supports common gym operations such as managing members, trainers, membership packages, rooms, and class schedules. It also includes member self-service features for class booking and fitness tracking.

## Technology Stack

| Area | Technologies |
| --- | --- |
| Language | C# |
| Web framework | ASP.NET Core MVC (.NET 10) |
| User interface | Razor Views, HTML, CSS, JavaScript |
| Data access | Entity Framework Core |
| Database | Microsoft SQL Server / SQL Server LocalDB |
| Authentication and roles | ASP.NET Core Identity |
| Development environment | Visual Studio |

The project also references **ClosedXML** (Excel document handling) and **QRCoder** (QR code generation).

## Features

### Administration
- Manage gym members, trainers, membership packages, and training rooms through CRUD operations.
- Manage class schedules and trainer assignments.
- Apply input validation and scheduling rules to help prevent invalid data and overlapping room or trainer assignments.

### Member Portal
- Member login and self-service access.
- Class booking and booking management.
- BMI tracking and fitness data visualization.

### Application Infrastructure
- Role-based authentication using ASP.NET Core Identity.
- Database access and migrations using Entity Framework Core.
- Separation of concerns through controllers, models, views, services, and validation components.

> **Note:** This repository is an academic project. Some modules may be under ongoing development. Consult the source code for exact implementation details.

## Repository Structure

```text
SmartGym-Management/
├── Areas/           # Area-specific pages and controllers
├── Controllers/     # MVC controllers
├── Data/            # Database context and configuration
├── Enums/           # Enumerations
├── Migrations/      # Entity Framework Core migrations
├── Models/          # Domain and data models
├── Security/        # Security-related components
├── Services/        # Application services and business logic
├── Tests/           # Test-related source files
├── Validation/      # Validation logic
├── Views/           # Razor views
├── ViewsModels/     # View-model-related files
├── docs/            # Project documentation
├── wwwroot/         # Static assets
├── Program.cs       # Application setup and service registration
└── SmartGymManagement.csproj
```

## Getting Started

### Prerequisites

- **.NET 10 SDK**
- **SQL Server or SQL Server LocalDB**
- Visual Studio with ASP.NET and web development tools, or another compatible .NET development environment

### Local setup

1. Clone the repository:

   ```bash
   git clone https://github.com/phulocccc/SmartGym-Management.git
   cd SmartGym-Management
   ```

2. Restore NuGet packages:

   ```bash
   dotnet restore
   ```

3. Configure a **local database connection** using the application's supported configuration method. Review `appsettings.json`, `appsettings.Development.json`, and the code under `Data/` before running. Do **not** commit production credentials or secrets.

4. Apply Entity Framework Core migrations once the database connection has been configured:

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef database update
   ```

   If `dotnet-ef` is already installed, skip the installation command.

5. Start the application:

   ```bash
   dotnet run
   ```

6. Open the local URL printed in the terminal. Depending on the configured authorization rules, some pages may require an account with the relevant role.

> These are general .NET setup steps. Database configuration, seed data, and role provisioning can vary by environment and may require additional local configuration.

## Development Notes

- Business rules and validation are implemented in dedicated project components rather than only in the user interface.
- ASP.NET Core Identity is configured for account management and roles.
- EF Core migrations are included to manage schema changes.
- Do not publish real member data, access tokens, passwords, or connection strings in issues, screenshots, or commits.

## Author

**Nguyen Phu Loc**  
GitHub: [@phulocccc](https://github.com/phulocccc)

---

*Academic project for demonstrating web application development, database integration, and gym management workflows.*
