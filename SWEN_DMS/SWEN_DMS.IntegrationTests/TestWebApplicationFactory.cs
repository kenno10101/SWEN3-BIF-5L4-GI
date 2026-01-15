using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using SWEN_DMS.BLL.Interfaces;
using SWEN_DMS.DAL;
using Testcontainers.PostgreSql;

namespace SWEN_DMS.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres;

    public Mock<IMessagePublisher> PublisherMock { get; } = new();
    public Mock<IFileStore> FileStoreMock { get; } = new();

    public TestWebApplicationFactory(PostgreSqlContainer postgres)
    {
        _postgres = postgres;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace DbContext with Testcontainers Postgres
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(_postgres.GetConnectionString()));

            // Replace IMessagePublisher (RabbitMQ) with mock
            services.RemoveAll(typeof(IMessagePublisher));
            services.AddSingleton(PublisherMock.Object);

            // Replace IFileStore (MinIO abstraction) with mock
            services.RemoveAll(typeof(IFileStore));
            services.AddSingleton(FileStoreMock.Object);

            // Bucket string DI (your Program.cs injects bucket as string)
            services.RemoveAll(typeof(string));
            services.AddSingleton("documents");
        });
    }
}