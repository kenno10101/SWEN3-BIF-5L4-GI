using NUnit.Framework;
using FluentAssertions;
using SWEN_DMS.IndexingWorker.Models;
using System;
using System.Collections.Generic;

namespace SWEN_DMS.Tests.IndexingWorker
{
    [TestFixture]
    public class DocumentIndexModelTests
    {
        [Test]
        public void DocumentIndexModel_ShouldInitializeWithDefaults()
        {
            // Act
            var model = new DocumentIndexModel();

            // Assert
            model.DocumentId.Should().Be(Guid.Empty);
            model.FileName.Should().Be(string.Empty);
            model.ExtractedText.Should().BeNull();
            model.Summary.Should().BeNull();
            model.Tags.Should().NotBeNull();
            model.Tags.Should().BeEmpty();
            model.UploadedAt.Should().Be(default(DateTime));
        }

        [Test]
        public void DocumentIndexModel_ShouldSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var uploadedAt = DateTime.UtcNow;
            var tags = new List<string> { "tag1", "tag2", "tag3" };

            // Act
            var model = new DocumentIndexModel
            {
                DocumentId = id,
                FileName = "document.pdf",
                ExtractedText = "This is extracted text from the document",
                Summary = "Document summary",
                Tags = tags,
                UploadedAt = uploadedAt
            };

            // Assert
            model.DocumentId.Should().Be(id);
            model.FileName.Should().Be("document.pdf");
            model.ExtractedText.Should().Be("This is extracted text from the document");
            model.Summary.Should().Be("Document summary");
            model.Tags.Should().BeEquivalentTo(tags);
            model.UploadedAt.Should().Be(uploadedAt);
        }

        [Test]
        public void DocumentIndexModel_Tags_ShouldBeModifiable()
        {
            // Arrange
            var model = new DocumentIndexModel();

            // Act
            model.Tags.Add("tag1");
            model.Tags.Add("tag2");

            // Assert
            model.Tags.Should().HaveCount(2);
            model.Tags.Should().Contain("tag1");
            model.Tags.Should().Contain("tag2");
        }
    }

    [TestFixture]
    public class SearchResultTests
    {
        [Test]
        public void SearchResult_ShouldInitializeWithDefaults()
        {
            // Act
            var result = new SearchResult();

            // Assert
            result.TotalHits.Should().Be(0);
            result.Documents.Should().NotBeNull();
            result.Documents.Should().BeEmpty();
        }

        [Test]
        public void SearchResult_ShouldSetProperties()
        {
            // Arrange
            var documents = new List<SearchResultDocument>
            {
                new SearchResultDocument { DocumentId = Guid.NewGuid() },
                new SearchResultDocument { DocumentId = Guid.NewGuid() }
            };

            // Act
            var result = new SearchResult
            {
                TotalHits = 42,
                Documents = documents
            };

            // Assert
            result.TotalHits.Should().Be(42);
            result.Documents.Should().HaveCount(2);
            result.Documents.Should().BeEquivalentTo(documents);
        }
    }

    [TestFixture]
    public class SearchResultDocumentTests
    {
        [Test]
        public void SearchResultDocument_ShouldInitializeWithDefaults()
        {
            // Act
            var document = new SearchResultDocument();

            // Assert
            document.DocumentId.Should().Be(Guid.Empty);
            document.FileName.Should().Be(string.Empty);
            document.ExtractedText.Should().BeNull();
            document.Summary.Should().BeNull();
            document.Tags.Should().NotBeNull();
            document.Tags.Should().BeEmpty();
            document.UploadedAt.Should().Be(default(DateTime));
            document.Score.Should().Be(0);
            document.Highlights.Should().BeNull();
        }

        [Test]
        public void SearchResultDocument_ShouldSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var uploadedAt = DateTime.UtcNow;
            var tags = new List<string> { "important", "urgent" };
            var highlights = new Dictionary<string, IReadOnlyCollection<string>>
            {
                { "ExtractedText", new List<string> { "<mark>matched</mark> text" } }
            };

            // Act
            var document = new SearchResultDocument
            {
                DocumentId = id,
                FileName = "search-result.docx",
                ExtractedText = "Full text content",
                Summary = "Brief summary",
                Tags = tags,
                UploadedAt = uploadedAt,
                Score = 0.95,
                Highlights = highlights
            };

            // Assert
            document.DocumentId.Should().Be(id);
            document.FileName.Should().Be("search-result.docx");
            document.ExtractedText.Should().Be("Full text content");
            document.Summary.Should().Be("Brief summary");
            document.Tags.Should().BeEquivalentTo(tags);
            document.UploadedAt.Should().Be(uploadedAt);
            document.Score.Should().Be(0.95);
            document.Highlights.Should().NotBeNull();
            document.Highlights.Should().ContainKey("ExtractedText");
        }

        [Test]
        public void SearchResultDocument_Score_ShouldHandleVariousValues()
        {
            // Arrange & Act
            var doc1 = new SearchResultDocument { Score = 0.0 };
            var doc2 = new SearchResultDocument { Score = 1.0 };
            var doc3 = new SearchResultDocument { Score = 0.5 };
            var doc4 = new SearchResultDocument { Score = 10.5 };

            // Assert
            doc1.Score.Should().Be(0.0);
            doc2.Score.Should().Be(1.0);
            doc3.Score.Should().Be(0.5);
            doc4.Score.Should().Be(10.5);
        }

        [Test]
        public void SearchResultDocument_Highlights_CanBeNull()
        {
            // Act
            var document = new SearchResultDocument
            {
                Highlights = null
            };

            // Assert
            document.Highlights.Should().BeNull();
        }

        [Test]
        public void SearchResultDocument_Highlights_CanContainMultipleFields()
        {
            // Arrange
            var highlights = new Dictionary<string, IReadOnlyCollection<string>>
            {
                { "ExtractedText", new List<string> { "<mark>keyword</mark> in text" } },
                { "FileName", new List<string> { "<mark>keyword</mark>.pdf" } },
                { "Summary", new List<string> { "Summary with <mark>keyword</mark>" } }
            };

            // Act
            var document = new SearchResultDocument
            {
                Highlights = highlights
            };

            // Assert
            document.Highlights.Should().HaveCount(3);
            document.Highlights.Should().ContainKeys("ExtractedText", "FileName", "Summary");
        }
    }
}