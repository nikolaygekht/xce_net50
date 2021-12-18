using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Gehtsoft.Xce.TextBuffer.Undo;
using Moq;
using Xunit;

#pragma warning disable S1116 // Empty statements should be removed

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class Undo
    {
        [Fact]
        public void Collection_New()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);
            collection.Count.Should().Be(0);
            collection.IsInTransaction.Should().BeFalse();
            collection.Should().BeEmpty();
            collection.Peek().Should().Be(null);
            collection.Pop().Should().Be(null);
        }

        [Fact]
        public void Collection_Push_Single()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);
            var actionMock = new Mock<IUndoAction>();
            var action = actionMock.Object;

            collection.Push(action);
            collection.Count.Should().Be(1);
            collection.IsInTransaction.Should().BeFalse();
            collection.Should().NotBeEmpty();
        }

        [Fact]
        public void Collection_Pop_Single()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);
            var actionMock = new Mock<IUndoAction>();
            var action = actionMock.Object;

            collection.Push(action);
            collection.Peek().Should().BeSameAs(action);
            collection.Pop().Should().Be(action);
            collection.Count.Should().Be(0);
        }

        private static void Collection_Setup_Many(UndoActionCollection collection, out List<IUndoAction> actions)
        {
            actions = new List<IUndoAction>();
            for (int i = 0; i < 10; i++)
            {
                var actionMock = new Mock<IUndoAction>();
                actions.Add(actionMock.Object);
            }

            foreach (var action in actions)
                collection.Push(action);
        }

        [Fact]
        public void Collection_Push_Many()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            Collection_Setup_Many(collection, out var actions);

            collection.Count.Should().Be(10);
            collection.Should().BeEquivalentTo(actions);
        }

        [Fact]
        public void Collection_Pop_Many()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            Collection_Setup_Many(collection, out var actions);

            for (int i = 1; i <= 10; i++)
            {
                collection.Count.Should().Be(10 - i + 1);
                collection.Peek().Should().BeSameAs(actions[10 - i]);
                collection.Pop().Should().BeSameAs(actions[10 - i]);
            }

            collection.Count.Should().Be(0);
            collection.Peek().Should().BeSameAs(null);
        }

        [Fact]
        public void Transaction_AddAndClose()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            using (var transaction = collection.BeginTransaction())
            {
                collection.Count.Should().Be(1);
                collection.IsInTransaction.Should().BeTrue();
            }
            collection.Count.Should().Be(1);
            collection.IsInTransaction.Should().BeFalse();
        }

        [Fact]
        public void Transaction_NewRecordsGoesToTransaction()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            using (var transaction = collection.BeginTransaction())
            {
                var actionMock = new Mock<IUndoAction>();
                var action = actionMock.Object;
                collection.Push(action);
                collection.Count.Should().Be(1);
                collection.Peek().Should().BeOfType<UndoTransaction>();
                collection.Peek().As<UndoTransaction>().Count.Should().Be(1);
                collection.Peek().As<UndoTransaction>().Peek().Should().BeSameAs(action);
            }
            collection.Count.Should().Be(1);
            collection.IsInTransaction.Should().BeFalse();
        }

        [Fact]
        public void Transaction_NewRecordsAfterClosing()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            using (var transaction = collection.BeginTransaction())
            {
                //do nothing intentionally
            }
            collection.Count.Should().Be(1);
            collection.IsInTransaction.Should().BeFalse();

            var actionMock = new Mock<IUndoAction>();
            var action = actionMock.Object;
            collection.Push(action);
            collection.Count.Should().Be(2);
            collection.Peek().Should().BeSameAs(action);
        }

        [Fact]
        public void Transaction_NewTransactionGoesToTransaction()
        {
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            using (var transaction = collection.BeginTransaction())
            {
                using (var transaction1 = collection.BeginTransaction())
                {
                    collection.Count.Should().Be(1);
                    collection.Peek().Should().BeOfType<UndoTransaction>();

                    collection.Peek().As<UndoTransaction>()
                        .Count.Should().Be(1);
                    collection.Peek().As<UndoTransaction>()
                        .Peek().Should().BeOfType<UndoTransaction>();

                    var action = new Mock<IUndoAction>(MockBehavior.Strict);
                    collection.Push(action.Object);

                    collection
                        .Peek().As<UndoTransaction>()
                        .Peek().As<UndoTransaction>()
                        .Peek().Should().BeSameAs(action.Object);

                    collection.Count.Should().Be(1);
                }
                collection.IsInTransaction.Should().BeTrue();
            }
            collection.Count.Should().Be(1);
            collection.IsInTransaction.Should().BeFalse();
        }

        [Fact]
        public void Transaction_Execute()
        {
            var undoSequence = new MockSequence();
            var redoSequence = new MockSequence();
            using var buffer = new TextBuffer();
            var collection = new UndoActionCollection(buffer);

            var action1 = new Mock<IUndoAction>(MockBehavior.Strict);
            var action2 = new Mock<IUndoAction>(MockBehavior.Strict);
            var action3 = new Mock<IUndoAction>(MockBehavior.Strict);

            action3.InSequence(undoSequence).Setup(a => a.Undo());
            action2.InSequence(undoSequence).Setup(a => a.Undo());
            action1.InSequence(undoSequence).Setup(a => a.Undo());

            action1.InSequence(redoSequence).Setup(a => a.Redo());
            action2.InSequence(redoSequence).Setup(a => a.Redo());
            action3.InSequence(redoSequence).Setup(a => a.Redo());

            using (var trasaction = collection.BeginTransaction())
            {
                collection.Push(action1.Object);
                collection.Push(action2.Object);
                collection.Push(action3.Object);
            }

            collection.Peek().Undo();
            collection.Peek().Redo();

            action1.Verify();
            action2.Verify();
            action3.Verify();
        }

        [Fact]
        public void Base_RestoreStatus()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("aaa", true);
            buffer.AppendLine("bbb", true);
            buffer.Status.BlockMode = BlockMode.Line;
            buffer.Status.BlockStart.Set(0, 0);
            buffer.Status.BlockEnd.Set(2, 0);

            buffer.InsertLine(1, "ccc");
            buffer.Status.BlockEnd.Line.Should().Be(3);

            var undo = buffer.UndoCollection.Pop();
            undo.Undo();
            buffer.Status.BlockEnd.Line.Should().Be(2);

            undo.Redo();
            buffer.Status.BlockEnd.Line.Should().Be(3);
        }

        [Fact]
        public void AppendLine_SuppressUndo()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("aaa", true);
            buffer.UndoCollection.Should().BeEmpty();
        }

        [Fact]
        public void AppendLine_Undo()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("aaa");
            buffer.AppendLine("bbb");

            buffer.GetContent().Should().BeEquivalentTo(new[] { "aaa", "bbb" });
            buffer.UndoCollection.Should().HaveCount(2);

            buffer.UndoCollection.Pop().Undo();

            buffer.GetContent().Should().BeEquivalentTo(new[] { "aaa" });
            buffer.UndoCollection.Should().HaveCount(1);

            buffer.UndoCollection.Pop().Undo();
            buffer.GetContent().Should().BeEmpty();
        }

        [Fact]
        public void InsertLine_SuppressUndo()
        {
            using var buffer = new TextBuffer();
            buffer.InsertLine(0, "aaa", true);
            buffer.UndoCollection.Should().BeEmpty();
        }

        [Fact]
        public void InsertLine_Undo()
        {
            using var buffer = new TextBuffer();
            buffer.InsertLine(0, "aaa");
            buffer.InsertLine(0, "bbb");

            buffer.GetContent().Should().BeEquivalentTo(new[] { "bbb", "aaa" });
            buffer.UndoCollection.Should().HaveCount(2);

            buffer.UndoCollection.Pop().Undo();

            buffer.GetContent().Should().BeEquivalentTo(new[] { "aaa" });
            buffer.UndoCollection.Should().HaveCount(1);

            buffer.UndoCollection.Pop().Undo();
            buffer.GetContent().Should().BeEmpty();
        }

        [Fact]
        public void RemoveLine_SuppressUndo()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("aaa", true);
            buffer.AppendLine("bbb", true);
            buffer.AppendLine("ccc", true);

            buffer.RemoveLine(1, true);

            buffer.UndoCollection.Should().HaveCount(0);
        }

        [Fact]
        public void RemoveLine_Undo()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("aaa", true);
            buffer.AppendLine("bbb", true);
            buffer.AppendLine("ccc", true);

            buffer.RemoveLine(1);

            buffer.UndoCollection.Should().HaveCount(1);
            buffer.GetContent().Should().BeEquivalentTo(new[] { "aaa", "ccc" });

            var undo = buffer.UndoCollection.Pop();
            undo.Undo();
            buffer.GetContent().Should().BeEquivalentTo(new[] { "aaa", "bbb", "ccc" });

            undo.Redo();
            buffer.GetContent().Should().BeEquivalentTo(new[] { "aaa", "ccc" });
        }
    }
}


