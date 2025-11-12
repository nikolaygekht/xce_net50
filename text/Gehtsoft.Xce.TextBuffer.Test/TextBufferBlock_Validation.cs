using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBufferBlock_Validation
    {
        #region None Block Tests

        [Fact]
        public void NoneBlock_DefaultConstructor_IsValid()
        {
            // Arrange & Act
            var block = new TextBufferBlock();

            // Assert
            block.BlockType.Should().Be(TextBufferBlockType.None);
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void NoneBlock_AlwaysValid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.None, -1, -5, -10, -20);

            // Assert
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Line Block Tests

        [Fact]
        public void LineBlock_ValidRange_IsValid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 0, 5);

            // Assert
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_SingleLine_IsValid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 3, 3);

            // Assert
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void LineBlock_NegativeFirstLine_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, -1, 5);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void LineBlock_FirstLineAfterLastLine_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Line, 5, 3);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void LineBlock_ColumnsIgnored()
        {
            // Arrange - columns don't matter for line blocks
            var block = new TextBufferBlock(TextBufferBlockType.Line, 0, 5, -10, -20);

            // Assert
            block.Valid.Should().Be(true);
        }

        #endregion

        #region Box Block Tests

        [Fact]
        public void BoxBlock_ValidRange_IsValid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 0, 5, 10, 20);

            // Assert
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void BoxBlock_SameColumns_IsValid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 0, 5, 10, 10);

            // Assert
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void BoxBlock_NegativeFirstColumn_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 0, 5, -1, 20);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void BoxBlock_FirstColumnAfterLastColumn_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 0, 5, 20, 10);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void BoxBlock_InvalidLines_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Box, 5, 3, 10, 20);

            // Assert
            block.Valid.Should().Be(false);
        }

        #endregion

        #region Stream Block Tests

        [Fact]
        public void StreamBlock_ValidMultiLine_IsValid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 0, 5, 10, 20);

            // Assert
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_ValidSingleLine_IsValid()
        {
            // Arrange - first column before last column
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 3, 3, 10, 20);

            // Assert
            block.Valid.Should().Be(true);
        }

        [Fact]
        public void StreamBlock_SingleLineSameColumns_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 3, 3, 10, 10);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void StreamBlock_SingleLineFirstAfterLast_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 3, 3, 20, 10);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void StreamBlock_NegativeFirstColumn_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 0, 5, -1, 20);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void StreamBlock_NegativeLastColumn_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 0, 5, 10, -1);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void StreamBlock_InvalidLines_IsInvalid()
        {
            // Arrange
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 5, 3, 10, 20);

            // Assert
            block.Valid.Should().Be(false);
        }

        [Fact]
        public void StreamBlock_MultiLineColumnsCanBeReversed()
        {
            // Arrange - for multi-line, column order doesn't matter for validation
            var block = new TextBufferBlock(TextBufferBlockType.Stream, 0, 5, 20, 10);

            // Assert - this is valid because it's multi-line
            block.Valid.Should().Be(true);
        }

        #endregion
    }
}
