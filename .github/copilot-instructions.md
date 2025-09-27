# Bloggit AI Agent Instructions

## Architecture Overview

This is a .NET 9 blogging platform implementing **Clean Architecture** with strict layer separation:

```
Domain (Core) -> Application (Use Cases) -> Infrastructure (Data) <- Api (Presentation)
```

### Key Architectural Patterns

- **Vertical Slice Architecture**: Each feature (Posts) has its own isolated stack
- **Tenant-Based Architecture**: Multi-tenant design using JWT `NameIdentifier` claim for data isolation
- **Entity Framework Primary Constructor Pattern**: `DataContext(DbContextOptions<DataContext> options) : DbContext(options)`
- **Repository with Dual Methods**: Both async (`AddAsync`) and sync (`Add`) versions for different usage patterns
- **Testcontainers Integration**: Real PostgreSQL database for integration tests
- **Minimal APIs**: Direct endpoint mapping in `Program.js` with grouped routes (`/api/admin/posts`)
- **Auth0 JWT Authentication**: Group-level authorization with `.RequireAuthorization()` on admin endpoints

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
    public string AuthorId { get; set; } = null!;  // Tenant identifier from JWT NameIdentifier
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;  // Default values
}
```

### Repository Pattern Implementation
```csharp
public class PostRepository(DataContext dataContext) : IPostRepository
{
    // Async operations include SaveChanges
    public async Task<PostEntity> AddAsync(PostEntity post) { /* Add + SaveChanges */ }
    
    // Tenant-aware methods for data isolation
    public async Task<PostEntity?> GetByIdAndAuthorAsync(Guid id, string authorId) { /* Filter by author */ }
    public async Task<IEnumerable<PostEntity>> GetByAuthorAsync(string authorId) { /* Get user's posts */ }
    
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

## Tenant-Based Architecture

### User Context Service Pattern
The platform implements multi-tenant data isolation using JWT claims:

```csharp
public interface IUserContextService
{
    string? GetCurrentUserId(); // Extracts NameIdentifier from JWT claims
}

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
```

### Tenant Data Isolation
- **AuthorId Field**: Every entity includes `AuthorId` populated from JWT `NameIdentifier` claim
- **Repository Filtering**: All queries filter by current user's ID for data isolation
- **Automatic Population**: `PostMappings.ToEntity()` requires `IUserContextService` to set AuthorId
- **API Endpoints**: All operations scope to current user's data only

### Tenant-Aware API Endpoints
```csharp
posts.MapGet("", async (IPostRepository repo, IUserContextService userContext) =>
{
    var userId = userContext.GetCurrentUserId();
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    
    var posts = await repo.GetByAuthorAsync(userId);
    return Results.Ok(posts.Select(p => p.ToResponse()));
});
```

## Authentication & Authorization

### Auth0 JWT Configuration
- **Group-Level Protection**: All `/api/admin/posts` endpoints protected via `.RequireAuthorization()`
- **Configuration-Based**: Auth0 Authority and Audience configured via `appsettings.json`
- **Enhanced Security**: Custom token validation parameters with zero clock skew
- **Environment Aware**: HTTPS metadata validation disabled in development

### Authentication Setup Pattern
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var posts = app.MapGroup("/api/admin/posts").RequireAuthorization(); // Protects all endpoints
```

### Test Authentication Bypass
- **TestAuthenticationHandler**: Custom handler that bypasses Auth0 for integration tests
- **Environment Detection**: Uses "Testing" environment to trigger test authentication
- **Fake Claims**: Generates test user claims (NameIdentifier, Name, Email)
- **Authorization Preserved**: Endpoints still require authorization but always pass in tests

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