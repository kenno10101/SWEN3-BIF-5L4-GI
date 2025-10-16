using NUnit.Framework;
using Moq;
using FluentAssertions;
using SWEN_DMS.BLL.Services;
using SWEN_DMS.DAL.Repositories;
using SWEN_DMS.Models;
using SWEN_DMS.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SWEN_DMS.Tests
{
    [TestFixture]
    public class DocumentServiceTests
    {
        private Mock<IDocumentRepository> _mockRepository = null!;
        private DocumentService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<IDocumentRepository>();
            _service = new DocumentService(_mockRepository.Object);
        }

        [Test]
        public async Task GetAllDocumentsAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document { Id = Guid.NewGuid(), FileName = "doc1.txt" },
                new Document { Id = Guid.NewGuid(), FileName = "doc2.txt" }
            };
            _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(documents);

            // Act
            var result = await _service.GetAllDocumentsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().FileName.Should().Be("doc1.txt");
        }

        [Test]
        public async Task GetDocumentAsync_ShouldReturnDto_WhenDocumentExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var doc = new Document { Id = id, FileName = "doc.txt" };
            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(doc);

            // Act
            var result = await _service.GetDocumentAsync(id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.FileName.Should().Be("doc.txt");
        }

        [Test]
        public async Task GetDocumentAsync_ShouldReturnNull_WhenDocumentDoesNotExist()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Document?)null);

            // Act
            var result = await _service.GetDocumentAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task AddDocumentAsync_ShouldAddDocumentAndReturnDto()
        {
            // Arrange
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Document>())).Returns(Task.CompletedTask);

            var dto = new DocumentCreateDto
            {
                File = new FormFile(new MemoryStream(new byte[] { 0x1 }), 0, 1, "file", "file.txt")
            };
            var filePath = "/tmp/file.txt";

            // Act
            var result = await _service.AddDocumentAsync(dto, filePath);

            // Assert
            result.FileName.Should().Be("file.txt");
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Document>()), Times.Once);
        }

        [Test]
        public async Task DeleteDocumentAsync_ShouldCallRepositoryDelete()
        {
            // Arrange
            _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
            var id = Guid.NewGuid();

            // Act
            await _service.DeleteDocumentAsync(id);

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}