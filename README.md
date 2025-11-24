# SWEN3_project
Document Management System a Document management system for archiving documents in a FileStore, with automatic OCR (queue for OC-recognition), automatic summary generation (using Gen-AI), tagging and full text search (ElasticSearch).

```
dotnet add package Microsoft.EntityFrameworkCore.InMemory

dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools

dotnet add package Microsoft.EntityFrameworkCore.Proxies

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite

dotnet add package FluentValidation.AspNetCore

dotnet tool uninstall -g dotnet-aspnet-codegenerator
dotnet tool install -g dotnet-aspnet-codegenerator
dotnet tool update -g dotnet-aspnet-codegenerator
```
added packages for solution
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design (manage migrations and to generate database schemas during development)
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL (enables EF Core to translate LINQ queries into PostgreSQL-compatible SQL and to interact with a PostgreSQL database)
dotnet add package Microsoft.EntityFrameworkCore.Tools (provides command-line interface (CLI) tools for Entity Framework Core)
dotnet tool install --global dotnet-ef (enables EF Core Commands to run from the terminal)
