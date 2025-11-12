using System;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_StressTests
    {
        #region Large Deletion Tests

        [Fact]
        public void DeleteLine_1MBLine_ShouldNotStackOverflow()
        {
            // Arrange - Create a 1MB line (500K characters)
            const int charCount = 500 * 1024;
            var largeLine = new string('A', charCount);
            var buffer = new TextBuffer(new[] { "before", largeLine, "after" });

            // Act - This should use ArrayPool instead of stackalloc
            buffer.DeleteLine(1);

            // Assert
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("before");
            buffer.GetLine(1).Should().Be("after");
        }

        [Fact]
        public void DeleteLine_10MBLine_ShouldNotStackOverflow()
        {
            // Arrange - Create a 10MB line (5M characters)
            const int charCount = 5 * 1024 * 1024;
            var sb = new StringBuilder(charCount);
            for (int i = 0; i < charCount; i++)
            {
                sb.Append((char)('A' + (i % 26)));
            }
            var largeLine = sb.ToString();
            var buffer = new TextBuffer(new[] { "header", largeLine, "footer" });

            // Act - This should use ArrayPool instead of stackalloc
            buffer.DeleteLine(1);

            // Assert
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("header");
            buffer.GetLine(1).Should().Be("footer");
        }

        [Fact]
        public void DeleteLine_LargeLineWithUndo_ShouldWorkCorrectly()
        {
            // Arrange - Create a 2MB line (1M characters)
            const int charCount = 1024 * 1024;
            var largeLine = new string('X', charCount);
            var buffer = new TextBuffer(new[] { "line1", largeLine, "line3" });

            // Act - Delete and undo
            buffer.DeleteLine(1);
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(1).Should().Be(largeLine);
        }

        [Fact]
        public void DeleteSubstring_1MBSubstring_ShouldNotStackOverflow()
        {
            // Arrange - Create a line with 1MB substring
            const int charCount = 500 * 1024;
            var largeLine = new string('B', charCount);
            var buffer = new TextBuffer(new[] { largeLine });

            // Act - Delete large substring
            buffer.DeleteSubstring(0, 0, charCount);

            // Assert
            buffer.GetLineLength(0).Should().Be(0);
        }

        [Fact]
        public void DeleteSubstring_10MBSubstring_ShouldNotStackOverflow()
        {
            // Arrange - Create a line with 10MB substring
            const int charCount = 5 * 1024 * 1024;
            var sb = new StringBuilder(charCount);
            for (int i = 0; i < charCount; i++)
            {
                sb.Append((char)('0' + (i % 10)));
            }
            var largeLine = sb.ToString();
            var buffer = new TextBuffer(new[] { largeLine });

            // Act - Delete large substring
            buffer.DeleteSubstring(0, 0, charCount / 2); // Delete first half (5MB)

            // Assert
            buffer.GetLineLength(0).Should().Be(charCount / 2);
        }

        [Fact]
        public void DeleteSubstring_LargeSubstringWithUndo_ShouldWorkCorrectly()
        {
            // Arrange - Create a 2MB substring
            const int charCount = 1024 * 1024;
            var largeSubstring = new string('Z', charCount);
            var buffer = new TextBuffer(new[] { "prefix" + largeSubstring + "suffix" });

            // Act - Delete and undo
            buffer.DeleteSubstring(0, 6, charCount); // Delete large substring
            buffer.Undo();

            // Assert
            buffer.GetLine(0).Should().Be("prefix" + largeSubstring + "suffix");
        }

        [Fact]
        public void DeleteLine_ThousandLargeLines_ShouldComplete()
        {
            // Arrange - Create 1000 lines of 100KB each
            const int lineCount = 1000;
            const int charsPerLine = 50 * 1024;
            var lines = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                lines[i] = new string((char)('A' + (i % 26)), charsPerLine);
            }
            var buffer = new TextBuffer(lines);

            // Act - Delete all large lines
            for (int i = lineCount - 1; i >= 0; i--)
            {
                buffer.DeleteLine(i);
            }

            // Assert
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void DeleteSubstring_SmallBuffer_ShouldUseStackAlloc()
        {
            // Arrange - Create a line with small content (below threshold)
            var smallLine = new string('S', 512); // Well below 1KB threshold
            var buffer = new TextBuffer(new[] { smallLine });

            // Act - This should use stackalloc (not ArrayPool)
            buffer.DeleteSubstring(0, 0, 256);

            // Assert
            buffer.GetLineLength(0).Should().Be(256);
        }

        [Fact]
        public void DeleteLine_ExactlyAtThreshold_ShouldWork()
        {
            // Arrange - Create a line exactly at the threshold (1024 chars)
            var thresholdLine = new string('T', 1024);
            var buffer = new TextBuffer(new[] { thresholdLine });

            // Act - This should use stackalloc (at threshold)
            buffer.DeleteLine(0);

            // Assert
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void DeleteLine_JustAboveThreshold_ShouldUseArrayPool()
        {
            // Arrange - Create a line just above the threshold (1025 chars)
            var aboveThresholdLine = new string('U', 1025);
            var buffer = new TextBuffer(new[] { aboveThresholdLine });

            // Act - This should use ArrayPool
            buffer.DeleteLine(0);

            // Assert
            buffer.LinesCount.Should().Be(0);
        }

        #endregion
    }
}
