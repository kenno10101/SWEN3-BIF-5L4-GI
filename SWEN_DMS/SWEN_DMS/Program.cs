using Microsoft.EntityFrameworkCore;
using SWEN_DMS.DAL;
using SWEN_DMS.DAL.Repositories;
using SWEN_DMS.BLL.Services;

var builder = WebApplication.CreateBuilder(args);

// Repository + Service registrieren
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<DocumentService>();

// Controller + Swagger aktivieren
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Datenbank (PostgreSQL via EF Core)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Hier kommt die Debug-Zeile hin:
Console.WriteLine("Connection String in use: " + builder.Configuration.GetConnectionString("DefaultConnection"));

// App erstellen
var app = builder.Build();

// Swagger nur in Development anzeigen
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS + Authorization + Controller aktivieren
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Anwendung starten
app.Run();