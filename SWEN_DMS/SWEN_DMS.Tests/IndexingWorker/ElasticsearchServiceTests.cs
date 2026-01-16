using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Elasticsearch.Net;
using SWEN_DMS.IndexingWorker.Services;
using SWEN_DMS.IndexingWorker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWEN_DMS.Tests.IndexingWorker
{
    [TestFixture]
    public class ElasticsearchServiceTests
    {
        private Mock<IElasticClient> _mockClient = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<ILogger<ElasticsearchService>> _mockLogger = null!;
        private ElasticsearchService _service = null!;
        private const string TestIndexName = "test-documents";

        [SetUp]
        public void Setup()
        {
            _mockClient = new Mock<IElasticClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<ElasticsearchService>>();

            _mockConfiguration.Setup(c => c["Elasticsearch:IndexName"]).Returns(TestIndexName);

            _service = new ElasticsearchService(
                _mockClient.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task IndexDocumentAsync_ShouldIndexDocument_WhenValid()
        {
            // Arrange
            var document = new DocumentIndexModel
            {
                DocumentId = Guid.NewGuid(),
                FileName = "test.txt",
                ExtractedText = "Test content",
                Summary = "Test summary",
                Tags = new List<string> { "tag1", "tag2" },
                UploadedAt = DateTime.UtcNow
            };

            var indexResponse = new Mock<IndexResponse>();
            indexResponse.Setup(r => r.IsValid).Returns(true);

            _mockClient.Setup(c => c.IndexDocumentAsync(document, default))
                .ReturnsAsync(indexResponse.Object);

            // Act
            await _service.IndexDocumentAsync(document);

            // Assert
            _mockClient.Verify(c => c.IndexDocumentAsync(document, default), Times.Once);
        }

        [Test]
        public void IndexDocumentAsync_ShouldThrowException_WhenIndexingFails()
        {
            // Arrange
            var document = new DocumentIndexModel
            {
                DocumentId = Guid.NewGuid(),
                FileName = "test.txt"
            };

            var indexResponse = new Mock<IndexResponse>();
            indexResponse.Setup(r => r.IsValid).Returns(false);

            _mockClient.Setup(c => c.IndexDocumentAsync(document, default))
                .ReturnsAsync(indexResponse.Object);

            // Act
            Func<Task> act = async () => await _service.IndexDocumentAsync(document);

            // Assert
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task SearchDocumentsAsync_ShouldReturnResults_WhenSearchSucceeds()
        {
            // Arrange
            var query = "test query";
            var documentId = Guid.NewGuid();
            
            var searchResponse = new Mock<ISearchResponse<DocumentIndexModel>>();
            searchResponse.Setup(r => r.IsValid).Returns(true);
            searchResponse.Setup(r => r.Total).Returns(1);
            
            var hit = new Mock<IHit<DocumentIndexModel>>();
            hit.Setup(h => h.Source).Returns(new DocumentIndexModel
            {
                DocumentId = documentId,
                FileName = "test.txt",
                ExtractedText = "Test content",
                Tags = new List<string> { "tag1" },
                UploadedAt = DateTime.UtcNow
            });
            hit.Setup(h => h.Score).Returns(1.5);
            hit.Setup(h => h.Highlight).Returns(new Dictionary<string, IReadOnlyCollection<string>>());

            searchResponse.Setup(r => r.Hits).Returns(new List<IHit<DocumentIndexModel>> { hit.Object });

            _mockClient.Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>>(),
                    default))
                .ReturnsAsync(searchResponse.Object);

            // Act
            var result = await _service.SearchDocumentsAsync(query, 0, 10);

            // Assert
            result.Should().NotBeNull();
            result.TotalHits.Should().Be(1);
            result.Documents.Should().HaveCount(1);
            result.Documents.First().DocumentId.Should().Be(documentId);
            result.Documents.First().FileName.Should().Be("test.txt");
            result.Documents.First().Score.Should().Be(1.5);
        }

        [Test]
        public void SearchDocumentsAsync_ShouldThrowException_WhenSearchFails()
        {
            // Arrange
            var searchResponse = new Mock<ISearchResponse<DocumentIndexModel>>();
            searchResponse.Setup(r => r.IsValid).Returns(false);

            _mockClient.Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>>(),
                    default))
                .ReturnsAsync(searchResponse.Object);

            // Act
            Func<Task> act = async () => await _service.SearchDocumentsAsync("query");

            // Assert
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task SearchDocumentsAsync_ShouldUseCorrectPagination()
        {
            // Arrange
            var searchResponse = new Mock<ISearchResponse<DocumentIndexModel>>();
            searchResponse.Setup(r => r.IsValid).Returns(true);
            searchResponse.Setup(r => r.Total).Returns(0);
            searchResponse.Setup(r => r.Hits).Returns(new List<IHit<DocumentIndexModel>>());

            SearchDescriptor<DocumentIndexModel>? capturedDescriptor = null;
            _mockClient.Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>>(),
                    default))
                .Callback<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>, CancellationToken>(
                    (func, _) => capturedDescriptor = func(new SearchDescriptor<DocumentIndexModel>()) as SearchDescriptor<DocumentIndexModel>)
                .ReturnsAsync(searchResponse.Object);

            // Act
            await _service.SearchDocumentsAsync("query", from: 20, size: 50);

            // Assert
            _mockClient.Verify(c => c.SearchAsync(
                It.IsAny<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>>(),
                default), Times.Once);
        }

        [Test]
        public async Task DeleteDocumentAsync_ShouldDeleteDocument_WhenValid()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            
            var deleteResponse = new Mock<DeleteByQueryResponse>();
            deleteResponse.Setup(r => r.IsValid).Returns(true);

            _mockClient.Setup(c => c.DeleteByQueryAsync(
                    It.IsAny<Func<DeleteByQueryDescriptor<DocumentIndexModel>, IDeleteByQueryRequest>>(),
                    default))
                .ReturnsAsync(deleteResponse.Object);

            // Act
            await _service.DeleteDocumentAsync(documentId);

            // Assert
            _mockClient.Verify(c => c.DeleteByQueryAsync(
                It.IsAny<Func<DeleteByQueryDescriptor<DocumentIndexModel>, IDeleteByQueryRequest>>(),
                default), Times.Once);
        }

        [Test]
        public async Task DeleteDocumentAsync_ShouldLogError_WhenDeleteFails()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            
            var deleteResponse = new Mock<DeleteByQueryResponse>();
            deleteResponse.Setup(r => r.IsValid).Returns(false);

            _mockClient.Setup(c => c.DeleteByQueryAsync(
                    It.IsAny<Func<DeleteByQueryDescriptor<DocumentIndexModel>, IDeleteByQueryRequest>>(),
                    default))
                .ReturnsAsync(deleteResponse.Object);

            // Act
            await _service.DeleteDocumentAsync(documentId);

            // Assert
            _mockClient.Verify(c => c.DeleteByQueryAsync(
                It.IsAny<Func<DeleteByQueryDescriptor<DocumentIndexModel>, IDeleteByQueryRequest>>(),
                default), Times.Once);
        }

        [Test]
        public async Task SearchDocumentsAsync_ShouldReturnEmptyResult_WhenNoMatches()
        {
            // Arrange
            var searchResponse = new Mock<ISearchResponse<DocumentIndexModel>>();
            searchResponse.Setup(r => r.IsValid).Returns(true);
            searchResponse.Setup(r => r.Total).Returns(0);
            searchResponse.Setup(r => r.Hits).Returns(new List<IHit<DocumentIndexModel>>());

            _mockClient.Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>>(),
                    default))
                .ReturnsAsync(searchResponse.Object);

            // Act
            var result = await _service.SearchDocumentsAsync("nonexistent");

            // Assert
            result.TotalHits.Should().Be(0);
            result.Documents.Should().BeEmpty();
        }

        [Test]
        public async Task SearchDocumentsAsync_ShouldHandleNullScore()
        {
            // Arrange
            var searchResponse = new Mock<ISearchResponse<DocumentIndexModel>>();
            searchResponse.Setup(r => r.IsValid).Returns(true);
            searchResponse.Setup(r => r.Total).Returns(1);
            
            var hit = new Mock<IHit<DocumentIndexModel>>();
            hit.Setup(h => h.Source).Returns(new DocumentIndexModel
            {
                DocumentId = Guid.NewGuid(),
                FileName = "test.txt"
            });
            hit.Setup(h => h.Score).Returns((double?)null);

            searchResponse.Setup(r => r.Hits).Returns(new List<IHit<DocumentIndexModel>> { hit.Object });

            _mockClient.Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndexModel>, ISearchRequest>>(),
                    default))
                .ReturnsAsync(searchResponse.Object);

            // Act
            var result = await _service.SearchDocumentsAsync("query");

            // Assert
            result.Documents.First().Score.Should().Be(0);
        }
    }
}