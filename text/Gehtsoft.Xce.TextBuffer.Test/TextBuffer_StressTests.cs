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

        #region Large Insert / Deep Auto-Extend (PR3.3)

        [Fact]
        public void InsertSubstring_10MB_UndoRedo_ContentRoundTrips()
        {
            // Mirror of the 10MB delete: a single InsertSubstring of 10M chars
            // must complete, undo to empty content, and redo back to the inserted text.
            const int charCount = 5 * 1024 * 1024;
            var sb = new StringBuilder(charCount);
            for (int i = 0; i < charCount; i++)
                sb.Append((char)('a' + (i % 26)));
            var bigText = sb.ToString();

            var buffer = new TextBuffer(new[] { "" });
            buffer.InsertSubstring(0, 0, bigText);

            buffer.GetLineLength(0).Should().Be(charCount);

            buffer.Undo();
            buffer.GetLineLength(0).Should().Be(0);

            buffer.Redo();
            buffer.GetLineLength(0).Should().Be(charCount);
            // Spot-check a few positions to confirm content equality without
            // building a second 10MB string just for the assertion.
            buffer.GetSubstring(0, 0, 1).Should().Be("a");
            buffer.GetSubstring(0, 25, 1).Should().Be("z");
            buffer.GetSubstring(0, charCount - 1, 1).Should().Be(bigText[charCount - 1].ToString());
        }

        [Fact]
        public void InsertLine_AtIndex100k_FromEmptyBuffer_AutoExtendsAndUndoRestoresEmpty()
        {
            // Deep auto-extend: from an empty buffer, requesting a line index of
            // 100,000 must materialize the intermediate empty lines and the
            // requested one. A single Undo must restore the empty buffer state.
            const int targetIndex = 100_000;
            var buffer = new TextBuffer();

            buffer.InsertLine(targetIndex, "deep");

            buffer.LinesCount.Should().Be(targetIndex + 1);
            buffer.GetLine(targetIndex).Should().Be("deep");
            buffer.GetLine(0).Should().Be("");                              // auto-extended
            buffer.GetLine(targetIndex - 1).Should().Be("");

            buffer.Undo();

            buffer.LinesCount.Should().Be(0);
            buffer.CanUndo.Should().BeFalse();
        }

        [Fact]
        public void InsertSubstring_DeepAutoExtend_LineAndColumn_UndoRestoresEmpty()
        {
            // Worst case: insert past both buffer end AND line end. A single
            // InsertSubstring at (5000, 1000) on an empty buffer materializes
            // 5001 lines and pads line 5000 with 1000 spaces before inserting.
            // One undo must wipe everything.
            var buffer = new TextBuffer();

            buffer.InsertSubstring(5000, 1000, "X");

            buffer.LinesCount.Should().Be(5001);
            buffer.GetLineLength(5000).Should().Be(1001);
            buffer.GetSubstring(5000, 1000, 1).Should().Be("X");
            buffer.GetSubstring(5000, 0, 4).Should().Be("    ");           // padding

            buffer.Undo();

            buffer.LinesCount.Should().Be(0);
            buffer.CanUndo.Should().BeFalse();
        }

        #endregion
    }
}
