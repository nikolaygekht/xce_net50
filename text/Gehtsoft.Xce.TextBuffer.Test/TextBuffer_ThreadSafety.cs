using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_ThreadSafety
    {
        // The buffer exposes a single Owner sink (no multi-subscriber registry), so
        // there is no concurrent add/remove/iterate surface to stress-test. What
        // remains is: many threads driving edits in parallel must serialize through
        // the buffer's lock and the owner must observe every notification.

        [Fact]
        public void MultipleThreadsEditingSimultaneously_OwnerShouldReceiveAllNotifications()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "initial" });
            var insertCount = 0;
            var callback = new TestCallback(
                onLinesInserted: (line, count) => Interlocked.Add(ref insertCount, count)
            );
            buffer.Owner = callback;

            const int threadsCount = 4;
            const int operationsPerThread = 25;
            var tasks = new Task[threadsCount];

            // Act - Multiple threads inserting lines
            for (int t = 0; t < threadsCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        buffer.InsertLine(0, $"line {i}");
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert - Every insertion should have produced exactly one callback.
            insertCount.Should().Be(threadsCount * operationsPerThread,
                "All line insertions should trigger callbacks");
        }

        [Fact]
        public void MultipleThreadsEditingSimultaneously_BufferStateRemainsConsistent()
        {
            // Arrange - no owner; just hammer the buffer with parallel inserts/deletes
            // and check the lock keeps internal state coherent.
            var buffer = new TextBuffer(new[] { "anchor" });

            const int threadsCount = 4;
            const int operationsPerThread = 50;
            var tasks = new Task[threadsCount];

            for (int t = 0; t < threadsCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        buffer.InsertLine(0, "tmp");
                        buffer.DeleteLine(0);
                    }
                });
            }

            Task.WaitAll(tasks);

            // Net effect: every insert is matched by a delete, so we land back at
            // the original line count. If the lock didn't hold, we'd see torn state
            // (negative counts, missed deletes, etc.) and one of the assertions
            // along the way would have thrown.
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("anchor");
        }

        private class TestCallback : ITextBufferCallback
        {
            private readonly Action<int, int> _onLinesInserted;
            private readonly Action<int, int> _onLinesDeleted;
            private readonly Action<int, int, int> _onSubstringInserted;
            private readonly Action<int, int, int> _onSubstringDeleted;

            public TestCallback(
                Action<int, int> onLinesInserted = null,
                Action<int, int> onLinesDeleted = null,
                Action<int, int, int> onSubstringInserted = null,
                Action<int, int, int> onSubstringDeleted = null)
            {
                _onLinesInserted = onLinesInserted;
                _onLinesDeleted = onLinesDeleted;
                _onSubstringInserted = onSubstringInserted;
                _onSubstringDeleted = onSubstringDeleted;
            }

            public void OnLinesInserted(int lineIndex, int count)
                => _onLinesInserted?.Invoke(lineIndex, count);

            public void OnLinesDeleted(int lineIndex, int count)
                => _onLinesDeleted?.Invoke(lineIndex, count);

            public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
                => _onSubstringInserted?.Invoke(lineIndex, columnIndex, length);

            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
                => _onSubstringDeleted?.Invoke(lineIndex, columnIndex, length);
        }
    }
}
