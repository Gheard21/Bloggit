# Bloggit - .NET 9 Blogging Platform

A modern blogging platform built with .NET 9, implementing Clean Architecture with vertical slice organization.

## Key Features

- **Clean Architecture**: Strict layer separation with Domain, Application, Infrastructure, and API layers
- **Vertical Slice Organization**: Each feature (Posts) has its own isolated stack
- **Minimal APIs**: Direct endpoint mapping with grouped routes
- **Entity Framework Core 9**: PostgreSQL database with code-first migrations  
- **Testcontainers Integration**: Real database testing with Docker
- **FluentValidation**: Request validation matching EF constraints
- **Auth0 JWT Authentication**: Secure API endpoints with JWT token validation
- **Primary Constructor Pattern**: Modern C# 12 syntax throughout

## Local Development Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL database and dev containers)
- [Git](https://git-scm.com/)
- [Visual Studio Code](https://code.visualstudio.com/) with [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) (optional but recommended)

## Development Options

You can develop this project either **locally** or in a **dev container**. The dev container provides a consistent, isolated environment with all dependencies pre-configured.

### Option 1: Dev Container Setup (Recommended)

The easiest way to get started is using the provided dev container configuration:

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Gheard21/Bloggit.git
   cd Bloggit
   ```

2. **Open in Dev Container**
   - Open the project in VS Code
   - When prompted, click "Reopen in Container"
   - Or use Command Palette: `Dev Containers: Reopen in Container`

3. **Configure Authentication** (one-time setup)
   ```bash
   # Set up Auth0 configuration via user secrets
   cd App/Posts/Api
   dotnet user-secrets set "Auth0:Authority" "https://your-auth0-domain.auth0.com/"
   dotnet user-secrets set "Auth0:Audience" "https://your-api-identifier"
   ```

4. **Start Development**
   ```bash
   # Database is already running in the container
   # Connection string is pre-configured via environment variables
   
   # Run migrations
   dotnet ef database update --project App/Posts/Infrastructure --startup-project App/Posts/Api
   
   # Start the API
   dotnet run --project App/Posts/Api
   ```
   
   **Note:** Database setup is automatic in dev containers, but Auth0 configuration needs to be set once per container.

The dev container includes:
- ✅ .NET 9 SDK pre-installed
- ✅ PostgreSQL database automatically configured
- ✅ EF Core tools ready to use
- ✅ Docker support for Testcontainers
- ✅ VS Code extensions pre-configured
- ✅ Automatic NuGet package caching
- ✅ Port forwarding (API: 5000/5001, PostgreSQL: 5432)

### Option 2: Local Setup

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

3. **Configure API Database Connection and Authentication**
   
   Set up the database connection string and Auth0 configuration as user secrets:
   ```bash
   cd App/Posts/Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=bloggitdb;Username=bloggit;Password=your_secure_password"
   dotnet user-secrets set "Auth0:Authority" "https://your-auth0-domain.auth0.com/"
   dotnet user-secrets set "Auth0:Audience" "https://your-api-identifier"
   ```
   
   > **Note:** Replace `your_secure_password` with the same password you used in the `.env` file above, and replace the Auth0 values with your actual Auth0 tenant configuration.

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

**🔒 Authentication Required**: All admin endpoints require a valid JWT token from Auth0. Include the token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

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

**Authentication Issues:**
- Verify Auth0 configuration is set via user secrets: `dotnet user-secrets list`
- Check Auth0 domain and audience values match your Auth0 application settings
- Ensure JWT tokens are valid and not expired when testing manually

**Dev Container Issues:**
- Ensure Docker Desktop is running before opening in VS Code
- If container fails to start, try rebuilding: Command Palette → `Dev Containers: Rebuild Container`
- Check Docker has sufficient resources allocated (8GB RAM recommended)

### Contributing

1. Create a feature branch from `main`
2. Follow the existing architectural patterns
3. Ensure all tests pass
4. Submit a pull request

For detailed architectural guidelines, see `.github/copilot-instructions.md`.