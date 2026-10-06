# Local development

## Prerequisites

- .NET 10 SDK
- Docker Desktop with the Linux container engine
- The .NET EF Core CLI (`dotnet tool install --global dotnet-ef --version 10.0.12`) if it is not already installed

## Start SQL Server

Copy `.env.example` to `.env` and set a unique local SQL Server password. `.env` is ignored by Git.

```powershell
Copy-Item .env.example .env
docker compose up -d sqlserver
```

The Compose file starts SQL Server only. The two databases are created by their EF migrations below; no schema is applied automatically at API startup.

## Configure application connection strings

Set each connection string with .NET User Secrets from the repository root. Replace the placeholder password with the value set in `.env`:

```powershell
dotnet user-secrets init --project src/Services/Catalog/Catalog.Api/Catalog.Api.csproj
dotnet user-secrets set "ConnectionStrings:CatalogDb" "Server=localhost,1433;Database=ContosoCatalog;User Id=sa;Password=<local-password>;TrustServerCertificate=True" --project src/Services/Catalog/Catalog.Api/Catalog.Api.csproj

dotnet user-secrets init --project src/Services/Orders/Orders.Api/Orders.Api.csproj
dotnet user-secrets set "ConnectionStrings:OrdersDb" "Server=localhost,1433;Database=ContosoOrders;User Id=sa;Password=<local-password>;TrustServerCertificate=True" --project src/Services/Orders/Orders.Api/Orders.Api.csproj
```

These `sa` connection strings are for local development only. Do not reuse them in deployed environments.

## Configure Orders JWT validation

Orders requires bearer tokens from a trusted OpenID Connect issuer. No token issuer or Identity service is included in this repository. Configure the issuer and API audience supplied by your identity provider:

```powershell
dotnet user-secrets set "Authentication:Authority" "https://<trusted-issuer>" --project src/Services/Orders/Orders.Api/Orders.Api.csproj
dotnet user-secrets set "Authentication:Audience" "<orders-api-audience>" --project src/Services/Orders/Orders.Api/Orders.Api.csproj
```

Tokens must contain a GUID `sub` claim and the relevant `permission` claim (`orders.read`, `orders.create`, `orders.approve`, `orders.reject`, `orders.cancel`, or `orders.start-preparing`). No development authentication bypass is enabled.

## Apply migrations

Run each service's migration against its own database. These commands change the target database; inspect the target connection string before running them.

```powershell
dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj --startup-project src/Services/Catalog/Catalog.Api/Catalog.Api.csproj --context CatalogDbContext
dotnet ef database update --project src/Services/Orders/Orders.Infrastructure/Orders.Infrastructure.csproj --startup-project src/Services/Orders/Orders.Api/Orders.Api.csproj --context OrdersDbContext
```

Orders includes a corrective migration for the duplicate order-item relationship. The initial migration is retained for databases that may already have applied it. The corrective migration is not applied by the application automatically.

## Run the APIs

In separate terminals:

```powershell
dotnet run --project src/Services/Catalog/Catalog.Api/Catalog.Api.csproj
dotnet run --project src/Services/Orders/Orders.Api/Orders.Api.csproj
```

Swagger is enabled in the Development environment. Catalog listens on port 5276 and Orders on port 5126 under the checked-in launch profiles.
