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
```
---

## Swagger / API Documentation

The REST API exposes an OpenAPI/Swagger UI when running locally via Docker:

Swagger UI: http://localhost:8080/swagger/index.html

---

## Additional Use Case: Document Notes
### Goal
Each document can store multiple notes/annotations (e.g., comments, short remarks, audit hints).

### Business Rules
A note belongs to exactly one document (DocumentId).
Note content must not be empty.
CreatedAtUtc is set server-side.

---

## Batch Processing: Daily Access Statistics (XML)
### Purpose

A scheduled batch worker imports daily access statistics from external systems.
External systems drop XML files into an input folder. The batch worker reads and processes matching files, stores daily access counts per document in PostgreSQL, and then archives processed files to prevent duplicate processing.

### XML Format
The expected XML format is:
- root element: <accessStatistics>
- attribute dateUtc (YYYY-MM-DD)
  - multiple <document> elements with:
      -documentId (GUID)
      - count (integer)

```Example XML file (access_2026-01-16.xml):
<?xml version="1.0" encoding="utf-8"?>
<accessStatistics dateUtc="2026-01-16">
    <document documentId="ee8511c2-b0da-4a83-afe8-d3fefb4e2879" count="2" />
    <document documentId="d634393f-8e04-42cf-8f37-4a05c64eb85c" count="3" />
</accessStatistics>
```

### Processing Behavior
- The batch worker reads all files in InputFolder matching FilePattern.
- For each <document> entry it updates the daily access counter for the given documentId and dateUtc.
- After successful processing, the file is moved to ArchiveFolder (timestamped) to avoid redundant processing.
- If processing fails, the file remains in the input folder for retry after the issue is fixed.

---

## High-Level Architecture

- **REST API (ASP.NET Core)**  
  Handles document upload/retrieval, stores metadata in PostgreSQL and files in MinIO, publishes jobs to RabbitMQ, and exposes endpoints for search and document notes.

- **OCR Worker**  
  Reads jobs from RabbitMQ, fetches PDFs from MinIO, runs OCR (Tesseract / pdftoppm + tesseract) and sends extracted text back to the REST API. It also forwards extracted text to downstream workers.

- **GenAI Worker**  
  Reads OCR text from RabbitMQ, calls Google Gemini to generate a summary, and updates the document summary via the REST API.

- **Indexing Worker (Elasticsearch)**  
  Receives indexing requests via RabbitMQ and stores the document text content (OCR result + metadata such as filename/tags/summary) into Elasticsearch to enable full-text search.

- **Batch Worker (Daily Access Statistics)**  
  Scheduled service that reads daily XML access log files from an input folder, stores per-document daily access counts in PostgreSQL, and archives processed files to prevent duplicate processing.

- **Infrastructure**  
  PostgreSQL, MinIO, RabbitMQ, and Elasticsearch are run together with all services via `docker-compose`.



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

- `NEST`  
  Elasticsearch .NET client used for indexing and querying documents.

- `Microsoft.Extensions.Hosting`  
  Hosting abstractions for .NET applications (used for worker/background services).

- `Microsoft.Extensions.Http`  
  Adds `HttpClientFactory` support for typed/named HTTP clients.

- `Microsoft.Extensions.Logging`  
  Core logging abstractions used across the application.

- `Microsoft.Extensions.Logging.Console`  
  Console logging provider for structured logs in containers/local dev.

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
```
