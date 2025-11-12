using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextMarker_Tests
    {
        #region TextMarker Basic Tests

        [Fact]
        public void TextMarker_DefaultConstructor_InitializesProperties()
        {
            // Act
            var marker = new TextMarker();

            // Assert
            marker.Id.Should().Be(string.Empty);
            marker.Line.Should().Be(0);
            marker.Column.Should().Be(0);
        }

        [Fact]
        public void TextMarker_ParameterizedConstructor_SetsProperties()
        {
            // Act
            var marker = new TextMarker("bookmark1", 5, 10);

            // Assert
            marker.Id.Should().Be("bookmark1");
            marker.Line.Should().Be(5);
            marker.Column.Should().Be(10);
        }

        [Fact]
        public void TextMarker_PropertiesAreMutable()
        {
            // Arrange
            var marker = new TextMarker("test", 1, 2);

            // Act
            marker.Id = "changed";
            marker.Line = 10;
            marker.Column = 20;

            // Assert
            marker.Id.Should().Be("changed");
            marker.Line.Should().Be(10);
            marker.Column.Should().Be(20);
        }

        #endregion

        #region TextMarkerCollection Basic Tests

        [Fact]
        public void TextMarkerCollection_StartsEmpty()
        {
            // Arrange & Act
            var collection = new TextMarkerCollection();

            // Assert
            collection.Count.Should().Be(0);
        }

        [Fact]
        public void TextMarkerCollection_Add_IncreasesCount()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 5, 10);

            // Act
            collection.Add(marker);

            // Assert
            collection.Count.Should().Be(1);
        }

        [Fact]
        public void TextMarkerCollection_AddMultiple_IncreasesCount()
        {
            // Arrange
            var collection = new TextMarkerCollection();

            // Act
            collection.Add(new TextMarker("m1", 1, 1));
            collection.Add(new TextMarker("m2", 2, 2));
            collection.Add(new TextMarker("m3", 3, 3));

            // Assert
            collection.Count.Should().Be(3);
        }

        [Fact]
        public void TextMarkerCollection_Remove_DecreasesCount()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 5, 10);
            collection.Add(marker);

            // Act
            var removed = collection.Remove(marker);

            // Assert
            removed.Should().Be(true);
            collection.Count.Should().Be(0);
        }

        [Fact]
        public void TextMarkerCollection_RemoveNonExistent_ReturnsFalse()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 5, 10);

            // Act
            var removed = collection.Remove(marker);

            // Assert
            removed.Should().Be(false);
        }

        [Fact]
        public void TextMarkerCollection_FindById_ReturnsMarker()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker1 = new TextMarker("m1", 1, 1);
            var marker2 = new TextMarker("m2", 2, 2);
            collection.Add(marker1);
            collection.Add(marker2);

            // Act
            var found = collection.FindById("m2");

            // Assert
            found.Should().Be(marker2);
        }

        [Fact]
        public void TextMarkerCollection_FindById_NotFound_ReturnsNull()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            collection.Add(new TextMarker("m1", 1, 1));

            // Act
            var found = collection.FindById("m2");

            // Assert
            found.Should().Be(null);
        }

        [Fact]
        public void TextMarkerCollection_RemoveById_RemovesMarker()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            collection.Add(new TextMarker("m1", 1, 1));
            collection.Add(new TextMarker("m2", 2, 2));

            // Act
            var removed = collection.RemoveById("m1");

            // Assert
            removed.Should().Be(true);
            collection.Count.Should().Be(1);
            collection.FindById("m1").Should().Be(null);
        }

        [Fact]
        public void TextMarkerCollection_Clear_RemovesAllMarkers()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            collection.Add(new TextMarker("m1", 1, 1));
            collection.Add(new TextMarker("m2", 2, 2));
            collection.Add(new TextMarker("m3", 3, 3));

            // Act
            collection.Clear();

            // Assert
            collection.Count.Should().Be(0);
        }

        [Fact]
        public void TextMarkerCollection_IsEnumerable()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            collection.Add(new TextMarker("m1", 1, 1));
            collection.Add(new TextMarker("m2", 2, 2));
            collection.Add(new TextMarker("m3", 3, 3));

            // Act
            int count = 0;
            foreach (var marker in collection)
            {
                count++;
            }

            // Assert
            count.Should().Be(3);
        }

        #endregion

        #region Line Insertion Adjustment Tests

        [Fact]
        public void TextMarkerCollection_InsertBeforeMarker_ShiftsMarkerDown()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 10, 5);
            collection.Add(marker);

            // Act
            collection.OnLinesInserted(5, 3);

            // Assert
            marker.Line.Should().Be(13);
            marker.Column.Should().Be(5);
        }

        [Fact]
        public void TextMarkerCollection_InsertAtMarkerLine_ShiftsMarkerDown()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 10, 5);
            collection.Add(marker);

            // Act
            collection.OnLinesInserted(10, 2);

            // Assert
            marker.Line.Should().Be(12);
            marker.Column.Should().Be(5);
        }

        [Fact]
        public void TextMarkerCollection_InsertAfterMarker_NoChange()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 10, 5);
            collection.Add(marker);

            // Act
            collection.OnLinesInserted(15, 5);

            // Assert
            marker.Line.Should().Be(10);
            marker.Column.Should().Be(5);
        }

        [Fact]
        public void TextMarkerCollection_InsertLines_AdjustsMultipleMarkers()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker1 = new TextMarker("m1", 5, 0);
            var marker2 = new TextMarker("m2", 10, 0);
            var marker3 = new TextMarker("m3", 15, 0);
            collection.Add(marker1);
            collection.Add(marker2);
            collection.Add(marker3);

            // Act - insert at line 8
            collection.OnLinesInserted(8, 3);

            // Assert
            marker1.Line.Should().Be(5);  // Before insertion, no change
            marker2.Line.Should().Be(13); // At/after insertion, shifted
            marker3.Line.Should().Be(18); // After insertion, shifted
        }

        #endregion

        #region Line Deletion Adjustment Tests

        [Fact]
        public void TextMarkerCollection_DeleteBeforeMarker_ShiftsMarkerUp()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 10, 5);
            collection.Add(marker);

            // Act - delete lines 3-5
            collection.OnLinesDeleted(3, 3);

            // Assert
            marker.Line.Should().Be(7);
            marker.Column.Should().Be(5);
        }

        [Fact]
        public void TextMarkerCollection_DeleteMarkerLine_MovesToDeletionStart()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 10, 5);
            collection.Add(marker);

            // Act - delete lines 10-12
            collection.OnLinesDeleted(10, 3);

            // Assert
            marker.Line.Should().Be(10);
            marker.Column.Should().Be(0); // Column reset to 0
        }

        [Fact]
        public void TextMarkerCollection_DeleteAfterMarker_NoChange()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 10, 5);
            collection.Add(marker);

            // Act - delete lines 15-20
            collection.OnLinesDeleted(15, 6);

            // Assert
            marker.Line.Should().Be(10);
            marker.Column.Should().Be(5);
        }

        [Fact]
        public void TextMarkerCollection_DeleteLines_AdjustsMultipleMarkers()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker1 = new TextMarker("m1", 5, 10);
            var marker2 = new TextMarker("m2", 10, 20);
            var marker3 = new TextMarker("m3", 15, 30);
            collection.Add(marker1);
            collection.Add(marker2);
            collection.Add(marker3);

            // Act - delete lines 8-12 (5 lines)
            collection.OnLinesDeleted(8, 5);

            // Assert
            marker1.Line.Should().Be(5);  // Before deletion, no change
            marker1.Column.Should().Be(10);

            marker2.Line.Should().Be(8);  // On deleted line, moved to deletion start
            marker2.Column.Should().Be(0); // Column reset

            marker3.Line.Should().Be(10); // After deletion, shifted up
            marker3.Column.Should().Be(30);
        }

        #endregion

        #region Substring Operations Tests

        [Fact]
        public void TextMarkerCollection_SubstringInsert_NoEffect()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 5, 10);
            collection.Add(marker);

            // Act
            collection.OnSubstringInserted(5, 5, 20);

            // Assert - markers don't adjust for substring operations
            marker.Line.Should().Be(5);
            marker.Column.Should().Be(10);
        }

        [Fact]
        public void TextMarkerCollection_SubstringDelete_NoEffect()
        {
            // Arrange
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("m1", 5, 10);
            collection.Add(marker);

            // Act
            collection.OnSubstringDeleted(5, 0, 15);

            // Assert - markers don't adjust for substring operations
            marker.Line.Should().Be(5);
            marker.Column.Should().Be(10);
        }

        #endregion

        #region Integration with TextBuffer Tests

        [Fact]
        public void TextBuffer_WithMarkerCollection_AdjustsOnInsert()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0", "line1", "line2", "line3", "line4" });
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("bookmark", 3, 2);
            collection.Add(marker);
            buffer.Callbacks.Add(collection);

            // Act - insert before marker
            buffer.InsertLine(1, "new line");

            // Assert
            marker.Line.Should().Be(4);
            marker.Column.Should().Be(2);
        }

        [Fact]
        public void TextBuffer_WithMarkerCollection_AdjustsOnDelete()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0", "line1", "line2", "line3", "line4" });
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("bookmark", 3, 5);
            collection.Add(marker);
            buffer.Callbacks.Add(collection);

            // Act - delete marker's line
            buffer.DeleteLine(3);

            // Assert
            marker.Line.Should().Be(3);
            marker.Column.Should().Be(0);
        }

        [Fact]
        public void TextBuffer_WithMarkerCollection_NoChangeOnSubstring()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("caret", 0, 6);
            collection.Add(marker);
            buffer.Callbacks.Add(collection);

            // Act - insert substring
            buffer.InsertSubstring(0, 0, "XXX");

            // Assert - marker doesn't move
            marker.Line.Should().Be(0);
            marker.Column.Should().Be(6);
        }

        [Fact]
        public void TextBuffer_WithMarkerCollection_UndoRedo_AdjustsMarkers()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0", "line1", "line2" });
            var collection = new TextMarkerCollection();
            var marker = new TextMarker("bookmark", 2, 0);
            collection.Add(marker);
            buffer.Callbacks.Add(collection);

            // Act - insert line, undo, redo
            buffer.InsertLine(0, "new line");
            marker.Line.Should().Be(3);

            buffer.Undo();
            marker.Line.Should().Be(2);

            buffer.Redo();
            marker.Line.Should().Be(3);
        }

        #endregion
    }
}
