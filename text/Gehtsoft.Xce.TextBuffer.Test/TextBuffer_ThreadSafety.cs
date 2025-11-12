using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_ThreadSafety
    {
        #region Callback Thread-Safety Tests

        [Fact]
        public void ConcurrentCallbackAddAndEdit_ShouldNotThrow()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });
            var callbackInvocations = 0;
            var exceptions = new List<Exception>();
            var cts = new CancellationTokenSource();

            // Act - Start background thread that adds/removes callbacks
            var callbackTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        var callback = new TestCallback(() => Interlocked.Increment(ref callbackInvocations));
                        buffer.Callbacks.Add(callback);
                        Thread.Sleep(1); // Small delay to increase chance of collision
                        buffer.Callbacks.Remove(callback);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Act - Main thread performs edits
            var editTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        buffer.InsertLine(0, "new line");
                        buffer.DeleteLine(0);
                        buffer.InsertSubstring(0, 0, "text");
                        buffer.DeleteSubstring(0, 0, 4);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Wait for both tasks with timeout
            var completed = Task.WaitAll(new[] { callbackTask, editTask }, TimeSpan.FromSeconds(10));

            // Cleanup
            cts.Cancel();

            // Assert
            completed.Should().BeTrue("Tasks should complete within timeout");
            exceptions.Should().BeEmpty("No exceptions should be thrown during concurrent operations");
        }

        [Fact]
        public void ConcurrentCallbackClearAndInvoke_ShouldNotThrow()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });
            var callbackInvocations = 0;
            var exceptions = new List<Exception>();

            // Add some initial callbacks
            for (int i = 0; i < 10; i++)
            {
                buffer.Callbacks.Add(new TestCallback(() => Interlocked.Increment(ref callbackInvocations)));
            }

            var cts = new CancellationTokenSource();

            // Act - Thread that clears callbacks
            var clearTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 50 && !cts.Token.IsCancellationRequested; i++)
                    {
                        buffer.Callbacks.Clear();
                        Thread.Sleep(2);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Act - Thread that invokes callbacks through edits
            var editTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        buffer.InsertLine(0, "new line");
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Wait for both tasks with timeout
            var completed = Task.WaitAll(new[] { clearTask, editTask }, TimeSpan.FromSeconds(10));

            // Cleanup
            cts.Cancel();

            // Assert
            completed.Should().BeTrue("Tasks should complete within timeout");
            exceptions.Should().BeEmpty("No exceptions should be thrown during concurrent operations");
        }

        [Fact]
        public void ConcurrentCallbackCountAndModify_ShouldNotThrow()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });
            var exceptions = new List<Exception>();
            var cts = new CancellationTokenSource();

            // Act - Thread that checks count repeatedly
            var countTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 200 && !cts.Token.IsCancellationRequested; i++)
                    {
                        var count = buffer.Callbacks.Count;
                        // Just read the count, should not throw
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Act - Thread that adds callbacks
            var addTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        buffer.Callbacks.Add(new TestCallback(() => { }));
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Act - Thread that removes callbacks
            var removeTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        var callback = new TestCallback(() => { });
                        buffer.Callbacks.Add(callback);
                        buffer.Callbacks.Remove(callback);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Wait for all tasks with timeout
            var completed = Task.WaitAll(new[] { countTask, addTask, removeTask }, TimeSpan.FromSeconds(10));

            // Cleanup
            cts.Cancel();

            // Assert
            completed.Should().BeTrue("Tasks should complete within timeout");
            exceptions.Should().BeEmpty("No exceptions should be thrown during concurrent operations");
        }

        [Fact]
        public void MultipleThreadsEditingSimultaneously_CallbacksShouldReceiveAllNotifications()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "initial" });
            var insertCount = 0;
            var deleteCount = 0;
            var callback = new TestCallback(
                onLinesInserted: (line, count) => Interlocked.Add(ref insertCount, count),
                onLinesDeleted: (line, count) => Interlocked.Add(ref deleteCount, count)
            );
            buffer.Callbacks.Add(callback);

            const int threadsCount = 4;
            const int operationsPerThread = 25;
            var tasks = new Task[threadsCount];

            // Act - Multiple threads inserting and deleting lines
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

            // Assert - All insertions should have been counted
            insertCount.Should().Be(threadsCount * operationsPerThread,
                "All line insertions should trigger callbacks");
        }

        [Fact]
        public void CallbackRemovesSelf_ShouldNotThrow()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2" });
            var invocationCount = 0;
            SelfRemovingCallback selfRemovingCallback = null;

            selfRemovingCallback = new SelfRemovingCallback(
                buffer,
                () => { invocationCount++; }
            );

            buffer.Callbacks.Add(selfRemovingCallback);

            // Act - This should not throw even though callback removes itself
            buffer.InsertLine(0, "new line");

            // Assert
            invocationCount.Should().Be(1, "Callback should have been invoked once before removal");
            buffer.Callbacks.Count.Should().Be(0, "Callback should have removed itself");
        }

        [Fact]
        public void MultipleCallbacks_OneSelfRemoves_OthersShouldStillBeInvoked()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });
            var callback1Count = 0;
            var callback2Count = 0;
            var callback3Count = 0;

            var callback1 = new TestCallback(() => callback1Count++);
            SelfRemovingCallback callback2 = null;
            callback2 = new SelfRemovingCallback(buffer, () => callback2Count++);
            var callback3 = new TestCallback(() => callback3Count++);

            buffer.Callbacks.Add(callback1);
            buffer.Callbacks.Add(callback2);
            buffer.Callbacks.Add(callback3);

            // Act
            buffer.InsertLine(0, "new line");

            // Assert
            callback1Count.Should().Be(1, "First callback should be invoked");
            callback2Count.Should().Be(1, "Self-removing callback should be invoked");
            callback3Count.Should().Be(1, "Third callback should be invoked");
            buffer.Callbacks.Count.Should().Be(2, "Only self-removing callback should be removed");
        }

        [Fact]
        public void CallbackAddsDuringInvocation_ShouldNotBeInvokedInSameIteration()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });
            var callback1Invoked = false;
            var callback2Invoked = false;
            TestCallback callback2 = null;

            var callback1 = new TestCallback(() =>
            {
                callback1Invoked = true;
                // Add another callback during invocation
                callback2 = new TestCallback(() => callback2Invoked = true);
                buffer.Callbacks.Add(callback2);
            });

            buffer.Callbacks.Add(callback1);

            // Act - First edit
            buffer.InsertLine(0, "new line");

            // Assert
            callback1Invoked.Should().BeTrue("First callback should be invoked");
            callback2Invoked.Should().BeFalse("Newly added callback should not be invoked in same iteration");

            // Act - Second edit
            callback1Invoked = false;
            callback2Invoked = false;
            buffer.InsertLine(0, "another line");

            // Assert
            callback1Invoked.Should().BeTrue("First callback should be invoked again");
            callback2Invoked.Should().BeTrue("Newly added callback should be invoked in next iteration");
        }

        #endregion

        #region Helper Classes

        private class SelfRemovingCallback : ITextBufferCallback
        {
            private readonly TextBuffer _buffer;
            private readonly Action _onInvoke;

            public SelfRemovingCallback(TextBuffer buffer, Action onInvoke = null)
            {
                _buffer = buffer;
                _onInvoke = onInvoke;
            }

            public void OnLinesInserted(int lineIndex, int count)
            {
                _onInvoke?.Invoke();
                _buffer.Callbacks.Remove(this);
            }

            public void OnLinesDeleted(int lineIndex, int count)
            {
                _onInvoke?.Invoke();
                _buffer.Callbacks.Remove(this);
            }

            public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
            {
                _onInvoke?.Invoke();
                _buffer.Callbacks.Remove(this);
            }

            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
            {
                _onInvoke?.Invoke();
                _buffer.Callbacks.Remove(this);
            }
        }

        private class TestCallback : ITextBufferCallback
        {
            private readonly Action _onAnyOperation;
            private readonly Action<int, int> _onLinesInserted;
            private readonly Action<int, int> _onLinesDeleted;
            private readonly Action<int, int, int> _onSubstringInserted;
            private readonly Action<int, int, int> _onSubstringDeleted;

            public TestCallback(
                Action onAnyOperation = null,
                Action<int, int> onLinesInserted = null,
                Action<int, int> onLinesDeleted = null,
                Action<int, int, int> onSubstringInserted = null,
                Action<int, int, int> onSubstringDeleted = null)
            {
                _onAnyOperation = onAnyOperation;
                _onLinesInserted = onLinesInserted;
                _onLinesDeleted = onLinesDeleted;
                _onSubstringInserted = onSubstringInserted;
                _onSubstringDeleted = onSubstringDeleted;
            }

            public void OnLinesInserted(int lineIndex, int count)
            {
                _onAnyOperation?.Invoke();
                _onLinesInserted?.Invoke(lineIndex, count);
            }

            public void OnLinesDeleted(int lineIndex, int count)
            {
                _onAnyOperation?.Invoke();
                _onLinesDeleted?.Invoke(lineIndex, count);
            }

            public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
            {
                _onAnyOperation?.Invoke();
                _onSubstringInserted?.Invoke(lineIndex, columnIndex, length);
            }

            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
            {
                _onAnyOperation?.Invoke();
                _onSubstringDeleted?.Invoke(lineIndex, columnIndex, length);
            }
        }

        #endregion
    }
}
