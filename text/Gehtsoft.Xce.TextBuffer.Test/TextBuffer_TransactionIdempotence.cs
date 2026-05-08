using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Pins that the transaction scope is double-dispose safe: a manual
    /// <c>Dispose()</c> followed by a <c>using</c>-block exit (or any other
    /// duplicate dispose) commits exactly once, throws nothing, and leaves
    /// no residue on the undo stack.
    /// </summary>
    public class TextBuffer_TransactionIdempotence
    {
        [Fact]
        public void TransactionScope_DisposedTwice_CommitsExactlyOnce()
        {
            var buffer = new TextBuffer();
            var scope = buffer.BeginUndoTransaction();
            buffer.InsertLine(0, "in-tx");

            scope.Dispose();        // commits
            scope.Dispose();        // no-op (must not throw, must not double-commit)

            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("in-tx");

            // Exactly one undo entry from this transaction.
            buffer.Undo();
            buffer.LinesCount.Should().Be(0);
            buffer.CanUndo.Should().BeFalse();
        }

        [Fact]
        public void TransactionScope_ManualDisposeBeforeUsingExit_NoExtraCommit()
        {
            // Real scenario: developer calls Dispose() explicitly inside a
            // using block (e.g., to commit early before some non-buffer side
            // effect runs). The using-statement's exit dispose must be a no-op.
            var buffer = new TextBuffer();
            using (var scope = buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "early-commit");
                scope.Dispose();    // manual commit
                // 'using' will Dispose again on scope exit — must be harmless.
            }

            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("early-commit");

            buffer.Undo();
            buffer.LinesCount.Should().Be(0);
            buffer.CanUndo.Should().BeFalse();
        }
    }
}
