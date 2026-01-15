using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SWEN_DMS.DAL;
using SWEN_DMS.DTOs;
using Testcontainers.PostgreSql;
using Xunit;

namespace SWEN_DMS.IntegrationTests;

public class DocumentUploadIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public DocumentUploadIntegrationTests()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("dms_test")
            .WithUsername("kendi")
            .WithPassword("kendi")
            .Build();
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task UploadDocument_ReturnsCreated_AndPersistsDocumentInDb()
    {

        await using var factory = new TestWebApplicationFactory(_postgres);
        using var client = factory.CreateClient();
        
        factory.FileStoreMock
            .Setup(s => s.SaveAsync(
                It.IsAny<string>(),          // bucket
                It.IsAny<string>(),          // objectKey
                It.IsAny<Stream>(),          // content
                It.IsAny<string>(),          // contentType
                It.IsAny<long>(),            // size
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Ensure DB schema exists (fast + stable for integration test)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        // Prepare multipart upload
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "test.pdf");
        form.Add(new StringContent("tag1,tag2"), "tags");

        // Act
        var response = await client.PostAsync("/api/Document/upload", form);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.Id);
        Assert.Equal("test.pdf", dto.FileName);

        // Assert DB persisted
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Documents.FindAsync(dto.Id);
            Assert.NotNull(entity);
            Assert.Equal("test.pdf", entity!.FileName);
            Assert.False(string.IsNullOrWhiteSpace(entity.FilePath)); // MinIO key stored
        }

        // Verify publisher called at least once (OCR request)
        factory.PublisherMock.Verify(p => p.PublishAsync(It.IsAny<object>(), null), Times.AtLeastOnce);
    }
}
