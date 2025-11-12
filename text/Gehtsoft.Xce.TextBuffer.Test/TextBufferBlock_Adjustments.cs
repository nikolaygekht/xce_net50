using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBufferBlock_Adjustments
    {
        #region Line Insertion Tests

        [Fact]
        public void LineBlock_InsertBeforeBlock_ShiftsDown()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesInserted(3, 2);

            // Assert
            block.FirstLine.Should().Be(7);
            block.LastLine.Should().Be(12);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_InsertAtFirstLine_ShiftsDown()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesInserted(5, 2);

            // Assert
            block.FirstLine.Should().Be(7);
            block.LastLine.Should().Be(12);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_InsertInsideBlock_ExpandsBlock()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesInserted(7, 3);

            // Assert
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(13);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_InsertAfterBlock_NoChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesInserted(15, 5);

            // Assert
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(10);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void BoxBlock_InsertLines_AdjustsLinesOnly()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 5, 10, 10, 20);

            // Act
            block.OnLinesInserted(3, 2);

            // Assert
            block.FirstLine.Should().Be(7);
            block.LastLine.Should().Be(12);
            block.FirstColumn.Should().Be(10);
            block.LastColumn.Should().Be(20);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertLines_AdjustsLinesOnly()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act
            block.OnLinesInserted(6, 3);

            // Assert
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(13);
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Line Deletion Tests

        [Fact]
        public void LineBlock_DeleteBeforeBlock_ShiftsUp()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 10, 15);

            // Act
            block.OnLinesDeleted(5, 3);

            // Assert
            block.FirstLine.Should().Be(7);
            block.LastLine.Should().Be(12);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_DeleteInsideBlock_Shrinks()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesDeleted(7, 2);

            // Assert
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(8);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_DeleteEntireBlock_BecomesInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesDeleted(3, 10);

            // Assert
            block.BlockType.Should().Be(TextBufferBlockType.Line);
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void LineBlock_DeleteStartOfBlock_AdjustsStart()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act - delete lines 4-6
            block.OnLinesDeleted(4, 3);

            // Assert
            block.FirstLine.Should().Be(4);
            block.LastLine.Should().Be(7);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_DeleteEndOfBlock_AdjustsEnd()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act - delete lines 9-12
            block.OnLinesDeleted(9, 4);

            // Assert
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(8);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_DeleteAfterBlock_NoChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act
            block.OnLinesDeleted(15, 5);

            // Assert
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(10);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Stream Block - Substring Insertion Tests

        [Fact]
        public void StreamBlock_InsertBeforeFirstColumn_ShiftsFirstColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - insert on first line before first column
            block.OnSubstringInserted(5, 10, 8);

            // Assert
            block.FirstColumn.Should().Be(23);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertAtFirstColumn_ShiftsFirstColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - insert at first column
            block.OnSubstringInserted(5, 15, 5);

            // Assert
            block.FirstColumn.Should().Be(20);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertAfterFirstColumn_NoChangeToFirstColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - insert after first column on first line
            block.OnSubstringInserted(5, 20, 5);

            // Assert
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertBeforeLastColumn_ShiftsLastColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - insert on last line before last column
            block.OnSubstringInserted(10, 20, 7);

            // Assert
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(32);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertAtLastColumn_NoChangeToLastColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - insert at last column (not before)
            block.OnSubstringInserted(10, 25, 5);

            // Assert
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertOnMiddleLine_NoChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - insert on middle line
            block.OnSubstringInserted(7, 0, 100);

            // Assert
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_InsertSingleLine_AdjustsBothColumns()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 5, 10, 20);

            // Act - insert before first column on single line
            block.OnSubstringInserted(5, 5, 8);

            // Assert - both first and last columns shift
            block.FirstColumn.Should().Be(18);
            block.LastColumn.Should().Be(28);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Stream Block - Substring Deletion Tests

        [Fact]
        public void StreamBlock_DeleteBeforeFirstColumn_ShiftsFirstColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - delete on first line before first column
            block.OnSubstringDeleted(5, 5, 8);

            // Assert
            block.FirstColumn.Should().Be(7);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_DeleteOverlappingFirstColumn_AdjustsFirstColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - delete overlapping first column (delete 10-20, first column at 15)
            block.OnSubstringDeleted(5, 10, 10);

            // Assert - first column moves to deletion start
            block.FirstColumn.Should().Be(10);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_DeleteBeforeLastColumn_ShiftsLastColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - delete on last line before last column
            block.OnSubstringDeleted(10, 10, 8);

            // Assert
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(17);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_DeleteOverlappingLastColumn_AdjustsLastColumn()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - delete overlapping last column (delete 20-30, last column at 25)
            block.OnSubstringDeleted(10, 20, 10);

            // Assert - last column moves to deletion start
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(20);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_DeleteAfterLastColumn_NoChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 10, 15, 25);

            // Act - delete after last column
            block.OnSubstringDeleted(10, 30, 10);

            // Assert
            block.FirstColumn.Should().Be(15);
            block.LastColumn.Should().Be(25);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Box Block - No Column Adjustments Tests

        [Fact]
        public void BoxBlock_InsertBeforeBlock_NoColumnChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 5, 10, 20, 30);

            // Act - insert before box columns
            block.OnSubstringInserted(7, 5, 100);

            // Assert - columns stay in place
            block.FirstColumn.Should().Be(20);
            block.LastColumn.Should().Be(30);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void BoxBlock_InsertInsideBlock_NoColumnChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 5, 10, 20, 30);

            // Act - insert inside box
            block.OnSubstringInserted(7, 25, 50);

            // Assert - columns stay in place
            block.FirstColumn.Should().Be(20);
            block.LastColumn.Should().Be(30);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void BoxBlock_DeleteBeforeBlock_NoColumnChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 5, 10, 20, 30);

            // Act - delete before box columns
            block.OnSubstringDeleted(7, 5, 10);

            // Assert - columns stay in place
            block.FirstColumn.Should().Be(20);
            block.LastColumn.Should().Be(30);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void BoxBlock_DeleteInsideBlock_NoColumnChange()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 5, 10, 20, 30);

            // Act - delete inside box
            block.OnSubstringDeleted(7, 22, 5);

            // Assert - columns stay in place
            block.FirstColumn.Should().Be(20);
            block.LastColumn.Should().Be(30);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Line Block - Ignores Column Operations

        [Fact]
        public void LineBlock_SubstringOperations_Ignored()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 10);

            // Act - various substring operations
            block.OnSubstringInserted(7, 10, 50);
            block.OnSubstringDeleted(8, 5, 20);

            // Assert - block unchanged
            block.FirstLine.Should().Be(5);
            block.LastLine.Should().Be(10);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region None Block Tests

        [Fact]
        public void NoneBlock_IgnoresAllCallbacks()
        {
            // Arrange
            var block = new TextBufferBlock();

            // Act - various operations
            block.OnLinesInserted(5, 10);
            block.OnLinesDeleted(3, 5);
            block.OnSubstringInserted(7, 10, 20);
            block.OnSubstringDeleted(8, 5, 15);

            // Assert - block unchanged
            block.FirstLine.Should().Be(0);
            block.LastLine.Should().Be(0);
            block.FirstColumn.Should().Be(0);
            block.LastColumn.Should().Be(0);
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Integration with TextBuffer Tests

        [Fact]
        public void TextBuffer_WithLineBlock_AdjustsOnInsert()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0", "line1", "line2", "line3", "line4" });
            var block = new TextBufferBlock(TextBufferBlockType.Line, 1, 3);
            buffer.Callbacks.Add(block);

            // Act - insert before block
            buffer.InsertLine(0, "new line");

            // Assert
            block.FirstLine.Should().Be(2);
            block.LastLine.Should().Be(4);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void TextBuffer_WithStreamBlock_AdjustsOnSubstringInsert()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 0, 0, 6, 11);
            buffer.Callbacks.Add(block);

            // Act - insert before block
            buffer.InsertSubstring(0, 0, "XXX");

            // Assert
            block.FirstColumn.Should().Be(9);
            block.LastColumn.Should().Be(14);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void TextBuffer_WithBoxBlock_StaysInPlace()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0", "line1", "line2" });
            var block = new TextBufferBlock(TextBufferBlockType.Box, 0, 2, 5, 10);
            buffer.Callbacks.Add(block);

            // Act - insert substring
            buffer.InsertSubstring(1, 0, "XXXXX");

            // Assert - columns don't change
            block.FirstColumn.Should().Be(5);
            block.LastColumn.Should().Be(10);
            block.Valid.Should().Be(true);
        }

        #endregion
    }
}
