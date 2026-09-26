## Docker

The ASP.NET Core API is containerized using Docker and can be started with Docker Compose.

### Architecture

Browser / API Client
        |
        | localhost:8080
        v
Docker Container
        |
        | ASP.NET Core (.NET 10)
        v
Entity Framework Core
        |
        v
PostgreSQL

### Run with Docker

Create a local `.env` file containing the required database configuration.

Then run:

docker compose up --build

The API will be available at:

http://localhost:8080

Example endpoint:

GET /api/dentalservices

### Docker Setup

- ASP.NET Core API runs inside a Linux container
- Container exposes port 8080
- Docker Compose maps host port 8080 to container port 8080
- Database configuration is supplied through environment variables
- PostgreSQL is accessed through Entity Framework Core
- Secrets are excluded from Git through `.gitignore`
