using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWEN_DMS.IndexingWorker.Services;
using SWEN_DMS.IndexingWorker.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SWEN_DMS.Tests.IndexingWorker
{
    [TestFixture]
    public class IndexingWorkerServiceTests
    {
        private Mock<ILogger<IndexingWorkerService>> _mockLogger = null!;
        private Mock<IElasticsearchService> _mockElasticsearchService = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private IndexingWorkerService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<IndexingWorkerService>>();
            _mockElasticsearchService = new Mock<IElasticsearchService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup default configuration values
            _mockConfiguration.Setup(c => c["RabbitMq:Host"]).Returns("localhost");
            _mockConfiguration.Setup(c => c["RabbitMq:Port"]).Returns("5672");
            _mockConfiguration.Setup(c => c["RabbitMq:User"]).Returns("guest");
            _mockConfiguration.Setup(c => c["RabbitMq:Password"]).Returns("guest");
            _mockConfiguration.Setup(c => c["RabbitMq:VirtualHost"]).Returns("/");
            _mockConfiguration.Setup(c => c["RabbitMq:Exchange"]).Returns("dms.exchange");
            _mockConfiguration.Setup(c => c["RabbitMq:Queue"]).Returns("indexing.requests");
            _mockConfiguration.Setup(c => c["RabbitMq:RoutingKey"]).Returns("indexing.request");

            _service = new IndexingWorkerService(
                _mockLogger.Object,
                _mockElasticsearchService.Object,
                _mockConfiguration.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public void Constructor_ShouldInitialize_WithValidParameters()
        {
            // Act
            var service = new IndexingWorkerService(
                _mockLogger.Object,
                _mockElasticsearchService.Object,
                _mockConfiguration.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act
            Action act = () => _service.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void Dispose_ShouldBeCallableMultipleTimes()
        {
            // Act
            _service.Dispose();
            Action act = () => _service.Dispose();

            // Assert
            act.Should().NotThrow();
        }
    }

    [TestFixture]
    public class IndexingRequestTests
    {
        [Test]
        public void IndexingRequest_ShouldInitializeWithDefaults()
        {
            // Act
            var request = new IndexingRequest();

            // Assert
            request.DocumentId.Should().Be(Guid.Empty);
            request.FileName.Should().Be(string.Empty);
            request.ExtractedText.Should().BeNull();
            request.Summary.Should().BeNull();
            request.Tags.Should().BeNull();
            request.UploadedAt.Should().Be(default(DateTime));
        }

        [Test]
        public void IndexingRequest_ShouldSetProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var uploadedAt = DateTime.UtcNow;

            // Act
            var request = new IndexingRequest
            {
                DocumentId = id,
                FileName = "test.pdf",
                ExtractedText = "Sample text",
                Summary = "A summary",
                Tags = "tag1,tag2,tag3",
                UploadedAt = uploadedAt
            };

            // Assert
            request.DocumentId.Should().Be(id);
            request.FileName.Should().Be("test.pdf");
            request.ExtractedText.Should().Be("Sample text");
            request.Summary.Should().Be("A summary");
            request.Tags.Should().Be("tag1,tag2,tag3");
            request.UploadedAt.Should().Be(uploadedAt);
        }
    }
}