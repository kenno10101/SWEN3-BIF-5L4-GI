# SWEN3 – Document Management System

A Document Management System for archiving PDF documents in an object store, with

- upload and storage of PDF documents
- metadata stored in a PostgreSQL database (via Entity Framework Core)
- file storage in MinIO
- automatic OCR processing (queue-based worker using Tesseract/Ghostscript.NET)
- automatic summary generation using Generative AI (Google Gemini, dedicated GenAI worker)
- REST API built with ASP.NET Core
- Web UI served from a separate container
- full text search via ElasticSearch

---

## Environment (.env)

The repository does **not** include a `.env` file (it is gitignored because the GenAI API key would be visible there).  
To run the project locally, you must create a `.env` file in the solution root with at least:

```env
GENAI_API_KEY=<your-api-key>
GENAI_MODEL=gemini-2.0-flash

---

## High-Level Architecture

- **REST API (ASP.NET Core)**  
  Handles document upload/retrieval, stores metadata in PostgreSQL and files in MinIO, and publishes jobs to RabbitMQ.

- **OCR Worker**  
  Reads jobs from RabbitMQ, fetches PDFs from MinIO, runs OCR (Tesseract/Ghostscript.NET) and sends extracted text back to RabbitMQ.

- **GenAI Worker**  
  Reads OCR text from RabbitMQ, calls Google Gemini to generate a summary, and updates the document via the REST API.

- **Infrastructure**  
  PostgreSQL, MinIO and RabbitMQ are run together with all services via `docker-compose`.


---

## NuGet Packages Used

### Persistence & Infrastructure

- `Microsoft.EntityFrameworkCore`  
  Core Entity Framework library used as ORM.

- `Microsoft.EntityFrameworkCore.Design`  
  Design-time support (migrations, schema generation, etc.).

- `Microsoft.EntityFrameworkCore.Tools`  
  Enables EF Core CLI commands (`dotnet ef ...`).

- `Npgsql.EntityFrameworkCore.PostgreSQL`  
  Entity Framework Core provider for PostgreSQL.

- `Microsoft.AspNetCore.Http.Abstractions`  
  Shared HTTP abstractions for ASP.NET Core (e.g. `HttpContext`, request/response types).

- `RabbitMQ.Client`  
  .NET client for RabbitMQ, used for OCR and GenAI queues.

- `Minio`  
  .NET client library to interact with MinIO (S3-compatible object storage).

- `Swashbuckle.AspNetCore`  
  Generates Swagger/OpenAPI docs and provides a Swagger UI for the REST API.

- `FluentValidation.AspNetCore`  
  Integrates FluentValidation with ASP.NET Core model binding and validation.

- `Ghostscript.NET`  
  Helpers around Ghostscript to process PDF pages as images for OCR.

- `Tesseract`  
  .NET wrapper for the Tesseract OCR engine.

### Testing, Quality & Coverage

- `NUnit`  
  Test framework for unit tests.

- `NUnit3TestAdapter`  
  Adapter so test runners (e.g. Rider, Visual Studio) can discover and execute NUnit tests.

- `NUnit.Analyzers`  
  Static analyzers that detect typical issues in NUnit-based tests.

- `Microsoft.NET.Test.Sdk`  
  Required infrastructure for running .NET test projects.

- `FluentAssertions`  
  Fluent, expressive assertion library for unit tests.

- `Moq`  
  Mocking framework used to fake dependencies (e.g. repositories, services) in tests.

- `coverlet.collector`  
  Collects code coverage information during test runs.

---

## Useful dotnet CLI Commands

Some packages/tools were added or are used via the dotnet CLI during development:

```bash
# EF Core packages (already referenced in the project)
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# FluentValidation (model validation)
dotnet add package FluentValidation.AspNetCore

# Global EF Core tool for migrations and CLI commands
dotnet tool install --global dotnet-ef
# later updates
dotnet tool update --global dotnet-ef
