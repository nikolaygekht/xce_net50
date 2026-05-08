using System;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Pins boundary behaviour of the span-based read APIs:
    /// <c>GetLine(int, Span&lt;char&gt;)</c>, <c>GetSubstring(int, int, int, Span&lt;char&gt;)</c>,
    /// and the string overload <c>GetSubstring(int, int, int)</c>.
    ///
    /// These APIs are non-throwing for out-of-range inputs (returning 0 or
    /// <c>string.Empty</c>) — that's the contract today and these tests pin it.
    /// </summary>
    public class TextBuffer_ReadApi
    {
        // ----------------------------------------------------------------------
        // GetLine(int, Span<char>) — return value is "characters copied".
        // ----------------------------------------------------------------------

        [Fact]
        public void GetLineSpan_TargetShorterThanLine_TruncatesToTargetLength()
        {
            var buffer = new TextBuffer(new[] { "Hello World" });
            Span<char> target = stackalloc char[5];

            int copied = buffer.GetLine(0, target);

            copied.Should().Be(5);
            target.ToString().Should().Be("Hello");
        }

        [Fact]
        public void GetLineSpan_TargetExactlyFitsLine_FullCopy_NoOverrun()
        {
            var buffer = new TextBuffer(new[] { "Hello" });
            Span<char> target = stackalloc char[5];

            int copied = buffer.GetLine(0, target);

            copied.Should().Be(5);
            target.ToString().Should().Be("Hello");
        }

        [Fact]
        public void GetLineSpan_TargetLongerThanLine_LeavesTrailingBytesUntouched()
        {
            // Pre-fill the target with sentinel chars; the read should only
            // overwrite the first N positions and leave the tail alone.
            var buffer = new TextBuffer(new[] { "abc" });
            Span<char> target = stackalloc char[8];
            target.Fill('Z');

            int copied = buffer.GetLine(0, target);

            copied.Should().Be(3);
            target[0].Should().Be('a');
            target[1].Should().Be('b');
            target[2].Should().Be('c');
            // tail untouched
            target[3].Should().Be('Z');
            target[7].Should().Be('Z');
        }

        [Fact]
        public void GetLineSpan_ZeroLengthTarget_NoOpReturnsZero()
        {
            var buffer = new TextBuffer(new[] { "anything" });

            int copied = buffer.GetLine(0, Span<char>.Empty);

            copied.Should().Be(0);
        }

        [Fact]
        public void GetLineSpan_OutOfRangeLine_ReturnsZero_DoesNotThrow()
        {
            // Pinning today's contract: out-of-range reads are non-throwing.
            // Callers that prefer "throw on bad index" can implement that
            // policy outside the buffer.
            var buffer = new TextBuffer(new[] { "only line" });
            Span<char> target = stackalloc char[16];
            target.Fill('Z');

            buffer.GetLine(99, target).Should().Be(0);
            buffer.GetLine(-1, target).Should().Be(0);
            // target untouched
            target[0].Should().Be('Z');
        }

        [Fact]
        public void GetLineSpan_EmptyLine_ReturnsZero()
        {
            var buffer = new TextBuffer(new[] { "first", "", "third" });
            Span<char> target = stackalloc char[8];

            int copied = buffer.GetLine(1, target);

            copied.Should().Be(0);
        }

        // ----------------------------------------------------------------------
        // GetSubstring(int, int, int, Span<char>) — clamps length to whichever
        // is smaller: remaining-on-line or target.Length.
        // ----------------------------------------------------------------------

        [Fact]
        public void GetSubstringSpan_LengthExceedsRemainingLine_ClampsToRemaining()
        {
            var buffer = new TextBuffer(new[] { "Hello World" });
            Span<char> target = stackalloc char[20];

            int copied = buffer.GetSubstring(0, 6, 100, target);

            copied.Should().Be(5);                  // "World"
            target.Slice(0, copied).ToString().Should().Be("World");
        }

        [Fact]
        public void GetSubstringSpan_LengthExceedsTargetSize_ClampsToTarget()
        {
            var buffer = new TextBuffer(new[] { "Hello World" });
            Span<char> target = stackalloc char[3];

            int copied = buffer.GetSubstring(0, 0, 11, target);

            copied.Should().Be(3);
            target.ToString().Should().Be("Hel");
        }

        [Fact]
        public void GetSubstringSpan_PastEndColumn_ReturnsZero_NoImplicitSpaces()
        {
            // Past-end columns are part of the buffer's "infinite empty" tail
            // for writes (D3), but reads return zero copied — they don't
            // synthesize implicit spaces.
            var buffer = new TextBuffer(new[] { "abcd" });
            Span<char> target = stackalloc char[8];
            target.Fill('Z');

            int copied = buffer.GetSubstring(0, 99, 5, target);

            copied.Should().Be(0);
            target[0].Should().Be('Z');                // untouched
        }

        [Fact]
        public void GetSubstringSpan_NegativeArguments_ReturnZero_DoNotThrow()
        {
            // Pin: read APIs are non-throwing even for negatives. Compare with
            // the edit APIs (D4) which throw — reads don't, by current contract.
            var buffer = new TextBuffer(new[] { "abcd" });
            Span<char> target = stackalloc char[8];

            buffer.GetSubstring(-1, 0, 1, target).Should().Be(0);
            buffer.GetSubstring(0, -1, 1, target).Should().Be(0);
            buffer.GetSubstring(0, 0, -1, target).Should().Be(0);
        }

        [Fact]
        public void GetSubstringSpan_OutOfRangeLine_ReturnsZero()
        {
            var buffer = new TextBuffer(new[] { "abcd" });
            Span<char> target = stackalloc char[8];

            buffer.GetSubstring(99, 0, 1, target).Should().Be(0);
        }

        // ----------------------------------------------------------------------
        // GetSubstring(int, int, int) — string overload. Same contract.
        // ----------------------------------------------------------------------

        [Fact]
        public void GetSubstringString_LengthExceedsLine_ClampsToRemaining()
        {
            var buffer = new TextBuffer(new[] { "Hello World" });

            buffer.GetSubstring(0, 6, 100).Should().Be("World");
        }

        [Fact]
        public void GetSubstringString_OutOfRangeOrEmpty_ReturnsEmptyString()
        {
            var buffer = new TextBuffer(new[] { "abcd" });

            buffer.GetSubstring(99, 0, 1).Should().BeEmpty();           // line out of range
            buffer.GetSubstring(0, 99, 5).Should().BeEmpty();           // past-end column
            buffer.GetSubstring(0, 0, 0).Should().BeEmpty();            // zero length
            buffer.GetSubstring(-1, 0, 1).Should().BeEmpty();           // negative line
            buffer.GetSubstring(0, -1, 1).Should().BeEmpty();           // negative col
            buffer.GetSubstring(0, 0, -1).Should().BeEmpty();           // negative length
        }
    }
}
