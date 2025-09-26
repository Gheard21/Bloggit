# Bloggit AI Agent Instructions

## Architecture Overview

This is a .NET 9 blogging platform implementing **Clean Architecture** with strict layer separation:

```
Domain (Core) -> Application (Use Cases) -> Infrastructure (Data) <- Api (Presentation)
```

### Key Architectural Patterns

- **Vertical Slice Architecture**: Each feature (Posts) has its own isolated stack
- **Entity Framework Primary Constructor Pattern**: `DataContext(DbContextOptions<DataContext> options) : DbContext(options)`
- **Repository with Dual Methods**: Both async (`AddAsync`) and sync (`Add`) versions for different usage patterns
- **Testcontainers Integration**: Real PostgreSQL database for integration tests
- **Minimal APIs**: Direct endpoint mapping in `Program.cs` with grouped routes (`/api/admin/posts`)

## Project Structure & Naming

### Directory Convention
```
App/{FeatureName}/{Layer}/
Tests/{FeatureName}/{Layer}/
Solutions/{FeatureName}.sln/
```

### Namespace Pattern
`Bloggit.{App|Tests}.{FeatureName}.{Layer}[.SubFolder]`

Example: `Bloggit.App.Posts.Infrastructure.Repositories`

## Critical Development Workflows

### Development Environment Options
- **Dev Container**: Recommended approach with `.devcontainer/` configuration providing isolated environment
- **Local Setup**: Traditional approach requiring local .NET 9 SDK and PostgreSQL via Docker Compose
- **Container Benefits**: Pre-configured PostgreSQL, EF tools, Docker-in-Docker for Testcontainers, VS Code extensions

### Building & Testing
- **Solution Location**: Use `Solutions/Posts.sln/Posts.sln` (note the nested directory structure)
- **Integration Tests**: Require Docker for Testcontainers PostgreSQL
- **Test Categories**: Use `[Trait("Category", "Integration")]` for database tests
- **Database**: PostgreSQL with Entity Framework migrations

### Database Operations
- **Connection**: PostgreSQL via Npgsql (`docker-compose.yaml` provides dev database)
- **Migrations**: Run from Infrastructure project: `dotnet ef migrations add {Name} --project . --startup-project ../Api`
- **Database Update**: `dotnet ef database update --project . --startup-project ../Api`
- **Test Database**: Automatically managed by PostgresServerFixture with seeded test data

## Entity & Repository Patterns

### Entity Design
```csharp
public class PostEntity  // Always suffix with "Entity"
{
    public Guid Id { get; set; }
    public string AuthorId { get; set; } = null!;  // Required strings use null!
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;  // Default values
}
```

### Repository Pattern Implementation
```csharp
public class PostRepository(DataContext dataContext) : IPostRepository
{
    // Async operations include SaveChanges
    public async Task<PostEntity> AddAsync(PostEntity post) { /* Add + SaveChanges */ }
    
    // Sync operations require manual SaveChanges call
    public void Add(PostEntity post) => _dataContext.Posts.Add(post);
    public async Task SaveChangesAsync() => await _dataContext.SaveChangesAsync();
}
```

### EF Configuration & Validation
- **Table Mapping**: Explicit in `OnModelCreating` with property constraints
- **String Fields**: Set `HasMaxLength()` for required strings (matches FluentValidation rules)
- **Required Fields**: Use `.IsRequired()` even when entity has `null!`
- **Validation**: FluentValidation in Application layer matches EF constraints (e.g., 200 char limit)

## Testing Patterns

### Integration Test Structure
```csharp
[Trait("Category", "Integration")]
public class PostRepositoryIntegrationTests(PostgresServerFixture fixture) 
    : IClassFixture<PostgresServerFixture>
{
    // Primary constructor pattern for test classes
}
```

### Test Database Management
- **Fixture**: `PostgresServerFixture` handles container lifecycle
- **Seeding**: Automatic test data seeding in fixture initialization
- **Isolation**: Each test uses `CreateContext()` for clean state

## Key Dependencies & Versions

- **.NET 9.0** with nullable reference types enabled
- **Entity Framework Core 9.0.9** with PostgreSQL provider
- **Testcontainers 4.7.0** for integration testing
- **Docker Compose** for local development database

## Layer Dependencies

- **Domain**: No external dependencies (pure business logic)
- **Infrastructure**: References Domain, includes EF Core and PostgreSQL
- **Application**: References Domain (business logic orchestration)
- **Api**: References Application and Infrastructure (composition root)
- **Tests**: Mirror the layer structure with appropriate test dependencies

## Common Operations

### Adding New Entities
1. Create entity in `Domain/Entities/{Name}Entity.cs`
2. Add interface in `Domain/Interfaces/I{Name}Repository.cs`
3. Implement repository in `Infrastructure/Repositories/{Name}Repository.cs`
4. Configure in `DataContext.OnModelCreating`
5. Generate migration: `dotnet ef migrations add Create{Name}Table`

### Integration Test Setup
- Inherit from `IClassFixture<PostgresServerFixture>`
- Use primary constructor to inject fixture
- Call `CreateContext()` within `await using` blocks
- Tests automatically have seeded data available

## Dev Container Configuration

### Container Stack
- **Base Image**: `mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm`
- **Database**: PostgreSQL 16 Alpine with volume persistence
- **Tools**: EF Core CLI, Docker Compose, PostgreSQL client pre-installed
- **VS Code**: Extensions for C# Dev Kit, Docker, REST Client automatically configured

### Container Database Setup
- **Dev Database**: `bloggit_dev` with `bloggit_user:bloggit_password`
- **Test Database**: `bloggit_test` (auto-created for integration tests)
- **Connection String**: Pre-configured via environment variables
- **Port Forwarding**: 5000/5001 (API), 5432 (PostgreSQL) automatically forwarded