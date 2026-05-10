# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication

Always respond in Russian, even if the user writes or pastes text in English.

## Build & Run

```bash
# Build
dotnet build Osnovanie.sln

# Run tests
dotnet test

# Run a single test project
dotnet test src/<TestProject>/<TestProject>.csproj

# Start dev infrastructure (PostgreSQL + Seq + API)
docker compose -f docker-compose-dev.yml up -d

# Apply migrations
dotnet ef database update --project src/Osnovanie.Infrastructure --startup-project src/Osnovanie.Api

# Add a migration
dotnet ef migrations add <MigrationName> --project src/Osnovanie.Infrastructure --startup-project src/Osnovanie.Api
```

Dev database: `Server=localhost;Port=5435;Database=vdele_db;Username=postgres;Password=!Qq12345`  
Seq log UI: `http://localhost:8081`  
API: `http://localhost:9001`

## Architecture

.NET 10 ASP.NET Core API. Modular clean architecture — no traditional controllers.

**Projects:**
- `Osnovanie.Api` — entry point, wires everything together in `Program.cs`
- `Osnovanie.Infrastructure` — EF Core, repositories, migrations, `AppDbContext`
- `Osnovanie.Framework` — custom endpoint base, middleware (exception handler, correlation ID)
- `Osnovanie.Shared` — `Result<T,Error>`, `Envelope<T>`, `Error`, `ITransactionManager`
- `src/Modules/Osnovanie.Modules.Auth` — phone/email auth, JWT
- `src/Modules/Osnovanie.Modules.VDele` — services marketplace (customers + specialists)
- `src/Modules/Osnovanie.Modules.VLavke` — goods marketplace (sellers + customers)
- `src/Modules/Osnovanie.Modules.ReferenceData` — cities lookup

Each module has a `*Module.cs` static class with `Add*Module()` and `Map*Module()` extension methods registered in `Program.cs`.

## Endpoint Pattern

No controllers. Every feature is a set of 3–4 files:

```
Feature/
  RegisterRequest.cs          # record DTO
  RegisterValidator.cs        # FluentValidation AbstractValidator<T>
  RegisterEndpoint.cs         # implements IEndpoint, calls MapPost/MapGet
  RegisterHandler.cs          # business logic, injected into endpoint
```

Endpoints are auto-discovered via reflection in `EndpointExtensions.MapEndpoints()`. To add a new endpoint, implement `IEndpoint` — registration is automatic if the class is in a registered module assembly.

Handler structure always follows:
1. Validate request (`IValidator<T>`)
2. Begin transaction (`ITransactionManager`)
3. Business logic (repositories, services)
4. Commit or rollback on error
5. Return `Result<T, Errors>` or `UnitResult<Error>`

## Error Handling & Result Types

Uses `CSharpFunctionalExtensions` throughout. All handlers return `Result<TValue, Error>`.

```csharp
Error.Validation("Code", "Message")   // ErrorType.VALIDATION
Error.Failure("Code", "Message")       // ErrorType.FAILURE
Error.NotFound("Code", "Message")      // ErrorType.NOT_FOUND
Error.Conflict("Code", "Message")      // ErrorType.CONFLICT
```

All API responses are wrapped in `Envelope<T>`. Errors are returned as `Envelope.Error(errors)`. Unhandled exceptions are caught by `ExceptionMiddleware` and returned as 500 `Envelope.Error()`.

FluentValidation rules use `.WithError(Error.Validation(...))` extension to keep error format consistent.

## Database / CQRS-lite

`AppDbContext` inherits `IdentityDbContext<User, Guid>` and implements all module read context interfaces (e.g., `IVDeleCustomersReadDbContext`).

Read queries use `.AsNoTracking()` via the module's `IReadDbContext` interface. Write operations go through concrete repositories injected from `Osnovanie.Infrastructure`.

Entity configurations live in `src/Osnovanie.Infrastructure/Database/Configurations/` and are applied via `ApplyConfigurationsFromAssembly`.

## Module Contracts

Each module exposes a `*.Contracts` project containing interfaces other modules can depend on (e.g., `IAuthRegistrationService`). Modules must not reference each other's implementation projects — only `*.Contracts`.
