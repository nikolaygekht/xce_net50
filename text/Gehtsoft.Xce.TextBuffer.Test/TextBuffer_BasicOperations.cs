using System;
using AwesomeAssertions;
using Moq;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_BasicOperations
    {
        #region Get Line Tests

        [Fact]
        public void GetLine_ThreeLines_ReturnsCorrectContent()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });

            // Act & Assert
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
        }

        [Fact]
        public void GetLine_NegativeIndex_ReturnsEmpty()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });

            // Act
            var result = buffer.GetLine(-1);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void GetLine_BeyondEnd_ReturnsEmpty()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });

            // Act
            var result = buffer.GetLine(10);

            // Assert
            result.Should().Be(string.Empty);
        }

        #endregion

        #region Insert Line Tests

        [Fact]
        public void InsertLine_ThreeLines_InsertsCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act
            buffer.InsertLine(0, "line1");
            buffer.InsertLine(1, "line2");
            buffer.InsertLine(2, "line3");

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
        }

        [Fact]
        public void InsertLine_BeyondEnd_AddsEmptyLines()
        {
            // Arrange
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "line1");

            // Act
            buffer.InsertLine(5, "line6");

            // Assert
            buffer.LinesCount.Should().Be(6);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be(string.Empty);
            buffer.GetLine(2).Should().Be(string.Empty);
            buffer.GetLine(3).Should().Be(string.Empty);
            buffer.GetLine(4).Should().Be(string.Empty);
            buffer.GetLine(5).Should().Be("line6");
        }

        [Fact]
        public void InsertLine_NegativeIndex_Throws()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.InsertLine(-1, "text"));
        }

        [Fact]
        public void InsertLine_CallsCallback()
        {
            // Arrange
            var buffer = new TextBuffer();
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.InsertLine(0, "line1");

            // Assert
            mock.Verify(cb => cb.OnLinesInserted(0, 1), Times.Once);
        }

        #endregion

        #region Delete Line Tests

        [Fact]
        public void DeleteLine_FirstLine_DeletesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3", "line4", "line5" });

            // Act
            buffer.DeleteLine(0);

            // Assert
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("line2");
            buffer.GetLine(1).Should().Be("line3");
            buffer.GetLine(2).Should().Be("line4");
            buffer.GetLine(3).Should().Be("line5");
        }

        [Fact]
        public void DeleteLine_MiddleLine_DeletesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3", "line4", "line5" });

            // Act
            buffer.DeleteLine(2);

            // Assert
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line4");
            buffer.GetLine(3).Should().Be("line5");
        }

        [Fact]
        public void DeleteLine_LastLine_DeletesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3", "line4", "line5" });

            // Act
            buffer.DeleteLine(4);

            // Assert
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
            buffer.GetLine(3).Should().Be("line4");
        }

        [Fact]
        public void DeleteLine_NegativeIndex_Throws()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3", "line4", "line5" });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteLine(-1));
        }

        [Fact]
        public void DeleteLine_BeyondEnd_DoesNothing()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3", "line4", "line5" });

            // Act
            buffer.DeleteLine(10);

            // Assert
            buffer.LinesCount.Should().Be(5);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
            buffer.GetLine(3).Should().Be("line4");
            buffer.GetLine(4).Should().Be("line5");
        }

        [Fact]
        public void DeleteLine_CallsCallback()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3", "line4", "line5" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.DeleteLine(2);

            // Assert
            mock.Verify(cb => cb.OnLinesDeleted(2, 1), Times.Once);
        }

        #endregion

        #region Insert Substring Tests

        [Fact]
        public void InsertSubstring_BeginningOfLine_InsertsCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "World" });

            // Act
            buffer.InsertSubstring(0, 0, "Hello ");

            // Assert
            buffer.GetLine(0).Should().Be("Hello World");
        }

        [Fact]
        public void InsertSubstring_MiddleOfLine_InsertsCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });

            // Act
            buffer.InsertSubstring(0, 6, "Beautiful ");

            // Assert
            buffer.GetLine(0).Should().Be("Hello Beautiful World");
        }

        [Fact]
        public void InsertSubstring_EndOfLine_InsertsCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act
            buffer.InsertSubstring(0, 5, " World");

            // Assert
            buffer.GetLine(0).Should().Be("Hello World");
        }

        [Fact]
        public void InsertSubstring_BeyondEndOfLine_AddsSpaces()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hi" });

            // Act
            buffer.InsertSubstring(0, 10, "X");

            // Assert
            buffer.GetLine(0).Should().Be("Hi        X");
            buffer.GetLineLength(0).Should().Be(11);
        }

        [Fact]
        public void InsertSubstring_NegativeLineIndex_Throws()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.InsertSubstring(-1, 0, "text"));
        }

        [Fact]
        public void InsertSubstring_NegativeColumnIndex_Throws()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.InsertSubstring(0, -1, "text"));
        }

        [Fact]
        public void InsertSubstring_CallsCallback()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.InsertSubstring(0, 6, "Beautiful ");

            // Assert
            mock.Verify(cb => cb.OnSubstringInserted(0, 6, 10), Times.Once);
        }

        #endregion

        #region Delete Substring Tests

        [Fact]
        public void DeleteSubstring_InsideLine_DeletesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello Beautiful World" });

            // Act
            buffer.DeleteSubstring(0, 6, 10);

            // Assert
            buffer.GetLine(0).Should().Be("Hello World");
        }

        [Fact]
        public void DeleteSubstring_WholeLine_DeletesAll()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act
            buffer.DeleteSubstring(0, 0, 5);

            // Assert
            buffer.GetLine(0).Should().Be(string.Empty);
            buffer.GetLineLength(0).Should().Be(0);
        }

        [Fact]
        public void DeleteSubstring_StartAtEndOfLine_DoesNothing()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act
            buffer.DeleteSubstring(0, 5, 10);

            // Assert
            buffer.GetLine(0).Should().Be("Hello");
        }

        [Fact]
        public void DeleteSubstring_StartBeyondEndOfLine_DoesNothing()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act
            buffer.DeleteSubstring(0, 10, 5);

            // Assert
            buffer.GetLine(0).Should().Be("Hello");
        }

        [Fact]
        public void DeleteSubstring_EndBeyondLineEnd_DeletesToEnd()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });

            // Act
            buffer.DeleteSubstring(0, 6, 100);

            // Assert
            buffer.GetLine(0).Should().Be("Hello ");
        }

        [Fact]
        public void DeleteSubstring_NegativeLineIndex_Throws()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteSubstring(-1, 0, 1));
        }

        [Fact]
        public void DeleteSubstring_NegativeColumnIndex_Throws()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteSubstring(0, -1, 1));
        }

        [Fact]
        public void DeleteSubstring_NegativeLength_Throws()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteSubstring(0, 0, -1));
        }

        [Fact]
        public void DeleteSubstring_CallsCallback()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello Beautiful World" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.DeleteSubstring(0, 6, 10);

            // Assert
            mock.Verify(cb => cb.OnSubstringDeleted(0, 6, 10), Times.Once);
        }

        #endregion
    }
}
