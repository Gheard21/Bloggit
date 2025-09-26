# Bloggit - .NET 9 Blogging Platform

A modern blogging platform built with .NET 9, implementing Clean Architecture with vertical slice organization.

## Key Features

- **Clean Architecture**: Strict layer separation with Domain, Application, Infrastructure, and API layers
- **Vertical Slice Organization**: Each feature (Posts) has its own isolated stack
- **Minimal APIs**: Direct endpoint mapping with grouped routes
- **Entity Framework Core 9**: PostgreSQL database with code-first migrations  
- **Testcontainers Integration**: Real database testing with Docker
- **FluentValidation**: Request validation matching EF constraints
- **Primary Constructor Pattern**: Modern C# 12 syntax throughout

## Local Development Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL database)
- [Git](https://git-scm.com/)

### Quick Start

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Gheard21/Bloggit.git
   cd Bloggit
   ```

2. **Set Up Environment Variables**
   
   Create a `.env` file in the root directory:
   ```env
   POSTGRES_USER=bloggit
   POSTGRES_PASSWORD=your_secure_password
   POSTGRES_DB=bloggitdb
   ```

3. **Configure API Database Connection**
   
   Set up the database connection string as a user secret:
   ```bash
   cd App/Posts/Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=bloggitdb;Username=bloggit;Password=your_secure_password"
   ```
   
   > **Note:** Replace `your_secure_password` with the same password you used in the `.env` file above.

4. **Start the Database**
   ```bash
   docker-compose up -d
   ```

5. **Build the Solution**
   ```bash
   cd Solutions/Posts.sln
   dotnet build
   ```

6. **Run Database Migrations**
   ```bash
   cd ../../App/Posts/Infrastructure
   dotnet ef database update --project . --startup-project ../Api
   ```

7. **Start the API**
   ```bash
   cd ../Api
   dotnet run
   ```

   The API will be available at:
   - HTTP: `http://localhost:5180`
   - HTTPS: `https://localhost:7019`
   - OpenAPI spec: `https://localhost:7019/openapi/v1.json` (development only)

### Development Workflow

#### Database Operations

**Add a New Migration:**
```bash
cd App/Posts/Infrastructure
dotnet ef migrations add {MigrationName} --project . --startup-project ../Api
```

**Update Database:**
```bash
dotnet ef database update --project . --startup-project ../Api
```

**Remove Last Migration:**
```bash
dotnet ef migrations remove --project . --startup-project ../Api
```

#### Running Tests

**Unit Tests Only:**
```bash
cd Solutions/Posts.sln
dotnet test --filter "Category!=Integration"
```

**Integration Tests (requires Docker):**
```bash
dotnet test --filter "Category=Integration"
```

**All Tests:**
```bash
dotnet test
```

> **Note:** Integration tests automatically spin up PostgreSQL containers via Testcontainers and seed test data.

#### Project Structure

The solution follows Clean Architecture with vertical slices:

```
App/Posts/
├── Domain/          # Business entities and interfaces
├── Application/     # Use cases and business logic
├── Infrastructure/  # Data access and external services
└── Api/            # Web API endpoints

Tests/Posts/
├── Domain/          # Unit tests for domain logic
├── Application/     # Unit tests for application services
├── Infrastructure/  # Integration tests with real database
└── Api/            # Integration tests for API endpoints
```

### API Usage

The API provides RESTful endpoints for blog post management under `/api/admin/posts`:

- `GET /api/admin/posts/{postId}` - Retrieve a specific post
- `POST /api/admin/posts` - Create a new post
- `PATCH /api/admin/posts/{postId}` - Update an existing post
- `DELETE /api/admin/posts/{postId}` - Delete a post

You can test the endpoints using:

- **HTTP Client**: Use the VS Code REST Client extension (sample requests in `Bloggit.App.Posts.Api.http`)
- **OpenAPI Spec**: Download from `https://localhost:7019/openapi/v1.json` (development only)
- **Postman/Insomnia**: Import the OpenAPI specification
- **curl**: Direct HTTP requests to the endpoints

### Troubleshooting

**Database Connection Issues:**
- Ensure Docker is running and the PostgreSQL container is healthy: `docker ps`
- Check the `.env` file has correct database credentials
- Verify the connection string is set via user secrets: `dotnet user-secrets list`

**Migration Issues:**
- Ensure you're running migrations from the `Infrastructure` project directory
- Use full command: `dotnet ef database update --project . --startup-project ../Api`
- Check EF tools are installed: `dotnet tool list -g`

**Port Conflicts:**
- Default ports are 5180 (HTTP) and 7019 (HTTPS)
- Modify `Properties/launchSettings.json` if these ports are in use
- Check for running processes: `netstat -ano | findstr :5180`

**Integration Test Issues:**
- Ensure Docker Desktop is running (required for Testcontainers)
- Tests may take longer on first run while downloading PostgreSQL image

### Contributing

1. Create a feature branch from `main`
2. Follow the existing architectural patterns
3. Ensure all tests pass
4. Submit a pull request

For detailed architectural guidelines, see `.github/copilot-instructions.md`.