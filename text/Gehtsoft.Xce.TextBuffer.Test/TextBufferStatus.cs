using FluentAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class BufferStatus
    {
        [Theory]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 2, 3, 4, 5, 6, BlockMode.Line, true)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                11, 2, 3, 4, 5, 6, BlockMode.Line, false)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 12, 3, 4, 5, 6, BlockMode.Line, false)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 2, 13, 4, 5, 6, BlockMode.Line, false)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 2, 3, 14, 5, 6, BlockMode.Line, false)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 2, 3, 4, 15, 6, BlockMode.Line, false)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 2, 3, 4, 5, 16, BlockMode.Line, false)]
        [InlineData(1, 2, 3, 4, 5, 6, BlockMode.Line,
                1, 2, 3, 4, 5, 6, BlockMode.Stream, false)]
        public void BufferStatusEquals(int cl1, int cc1, int bsl1, int bsc1, int bel1, int bec1, BlockMode bm1,
                                   int cl2, int cc2, int bsl2, int bsc2, int bel2, int bec2, BlockMode bm2,
                                   bool expected)
        {
            var s1 = new TextBufferStatus();
            s1.CursorPosition.Line = cl1;
            s1.CursorPosition.Column = cc1;
            s1.BlockStart.Line = bsl1;
            s1.BlockStart.Column = bsc1;
            s1.BlockEnd.Line = bel1;
            s1.BlockEnd.Column = bec1;
            s1.BlockMode = bm1;

            var s2 = new TextBufferStatus();
            s2.CursorPosition.Line = cl2;
            s2.CursorPosition.Column = cc2;
            s2.BlockStart.Line = bsl2;
            s2.BlockStart.Column = bsc2;
            s2.BlockEnd.Line = bel2;
            s2.BlockEnd.Column = bec2;
            s2.BlockMode = bm2;

            s1.Equals(s2).Should().Be(expected);
        }

        [Fact]
        public void Clone()
        {
            var s1 = new TextBufferStatus();
            s1.CursorPosition.Line = 1;
            s1.CursorPosition.Column = 2;
            s1.BlockStart.Line = 3;
            s1.BlockStart.Column = 4;
            s1.BlockEnd.Line = 5;
            s1.BlockEnd.Column = 6;
            s1.BlockMode = BlockMode.Box;

            var s2 = s1.Clone();

            s2.Should().BeEquivalentTo(s1);
        }
    }
}
