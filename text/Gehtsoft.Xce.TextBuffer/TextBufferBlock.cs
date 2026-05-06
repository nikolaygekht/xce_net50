using System;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Represents a selected block in the text buffer that automatically adjusts to buffer changes
    /// </summary>
    public class TextBufferBlock : ITextBufferCallback
    {
        /// <summary>
        /// Gets or sets the first line of the block (0-based)
        /// </summary>
        public int FirstLine { get; set; }

        /// <summary>
        /// Gets or sets the last line of the block (0-based, inclusive)
        /// </summary>
        public int LastLine { get; set; }

        /// <summary>
        /// Gets or sets the first column of the block (0-based)
        /// </summary>
        public int FirstColumn { get; set; }

        /// <summary>
        /// Gets or sets the last column of the block (0-based, exclusive for stream, inclusive for box)
        /// </summary>
        public int LastColumn { get; set; }

        /// <summary>
        /// Gets or sets the block type
        /// </summary>
        public TextBufferBlockType BlockType { get; set; }

        /// <summary>
        /// Gets whether the block is valid according to its type
        /// </summary>
        public bool Valid
        {
            get
            {
                // Common validation for all types
                if (BlockType == TextBufferBlockType.None)
                    return true;

                if (FirstLine < 0 || FirstLine > LastLine)
                    return false;

                switch (BlockType)
                {
                    case TextBufferBlockType.Line:
                        // Line blocks only need valid line range
                        return true;

                    case TextBufferBlockType.Box:
                        // Box blocks need valid column range
                        if (FirstColumn < 0 || FirstColumn > LastColumn)
                            return false;
                        return true;

                    case TextBufferBlockType.Stream:
                        // Stream blocks need non-negative columns
                        if (FirstColumn < 0 || LastColumn < 0)
                            return false;

                        // If single line, first column must be before last column
                        if (FirstLine == LastLine && FirstColumn >= LastColumn)
                            return false;

                        return true;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Creates an empty block (None type)
        /// </summary>
        public TextBufferBlock()
        {
            BlockType = TextBufferBlockType.None;
            FirstLine = 0;
            LastLine = 0;
            FirstColumn = 0;
            LastColumn = 0;
        }

        /// <summary>
        /// Creates a block with specified parameters
        /// </summary>
        public TextBufferBlock(TextBufferBlockType blockType, int firstLine, int lastLine, int firstColumn = 0, int lastColumn = 0)
        {
            BlockType = blockType;
            FirstLine = firstLine;
            LastLine = lastLine;
            FirstColumn = firstColumn;
            LastColumn = lastColumn;
        }

        /// <summary>Configure as a Line block covering full lines [firstLine..lastLine] inclusive.</summary>
        public void SetLine(int firstLine, int lastLine)
        {
            BlockType = TextBufferBlockType.Line;
            FirstLine = firstLine;
            LastLine = lastLine;
            FirstColumn = 0;
            LastColumn = 0;
        }

        /// <summary>Configure as a Box (rectangular) block.</summary>
        public void SetBox(int firstLine, int firstColumn, int lastLine, int lastColumn)
        {
            BlockType = TextBufferBlockType.Box;
            FirstLine = firstLine;
            FirstColumn = firstColumn;
            LastLine = lastLine;
            LastColumn = lastColumn;
        }

        /// <summary>Configure as a Stream (continuous text) selection.</summary>
        public void SetStream(int firstLine, int firstColumn, int lastLine, int lastColumn)
        {
            BlockType = TextBufferBlockType.Stream;
            FirstLine = firstLine;
            FirstColumn = firstColumn;
            LastLine = lastLine;
            LastColumn = lastColumn;
        }

        /// <summary>Clear the block: type becomes None.</summary>
        public void Clear()
        {
            BlockType = TextBufferBlockType.None;
            FirstLine = 0;
            LastLine = 0;
            FirstColumn = 0;
            LastColumn = 0;
        }

        /// <summary>Capture full state for a snapshot.</summary>
        internal (TextBufferBlockType type, int firstLine, int lastLine, int firstCol, int lastCol) Snapshot()
            => (BlockType, FirstLine, LastLine, FirstColumn, LastColumn);

        /// <summary>Restore state from a snapshot.</summary>
        internal void RestoreFromSnapshot(TextBufferBlockType type, int firstLine, int lastLine, int firstCol, int lastCol)
        {
            BlockType = type;
            FirstLine = firstLine;
            LastLine = lastLine;
            FirstColumn = firstCol;
            LastColumn = lastCol;
        }

        /// <summary>
        /// Called when lines are inserted into the buffer
        /// </summary>
        public void OnLinesInserted(int lineIndex, int count)
        {
            if (BlockType == TextBufferBlockType.None)
                return;

            // Lines inserted before the block - shift the block down
            if (lineIndex <= FirstLine)
            {
                FirstLine += count;
                LastLine += count;
            }
            // Lines inserted inside the block - expand the block
            else if (lineIndex <= LastLine)
            {
                LastLine += count;
            }
            // Lines inserted after the block - no change needed
        }

        /// <summary>
        /// Called when lines are deleted from the buffer
        /// </summary>
        public void OnLinesDeleted(int lineIndex, int count)
        {
            if (BlockType == TextBufferBlockType.None)
                return;

            int deletedFirstLine = lineIndex;
            int deletedLastLine = lineIndex + count - 1;

            // Deletion completely before the block - shift the block up
            if (deletedLastLine < FirstLine)
            {
                FirstLine -= count;
                LastLine -= count;
            }
            // Deletion starts before block and ends inside or after block
            else if (deletedFirstLine <= FirstLine && deletedLastLine >= FirstLine)
            {
                // If deletion encompasses entire block
                if (deletedLastLine >= LastLine)
                {
                    // Keep block type, but make it invalid
                    FirstLine = deletedFirstLine;
                    LastLine = deletedFirstLine - 1;
                }
                else
                {
                    // Block start is deleted, move to first line after deletion
                    FirstLine = deletedFirstLine;
                    LastLine -= count;
                }
            }
            // Deletion is completely inside the block
            else if (deletedFirstLine > FirstLine && deletedLastLine <= LastLine)
            {
                LastLine -= count;
            }
            // Deletion starts inside block and ends after block
            else if (deletedFirstLine <= LastLine && deletedLastLine > LastLine)
            {
                LastLine = deletedFirstLine - 1;
            }
            // Deletion is completely after the block - no change needed
        }

        /// <summary>
        /// Called when a substring is inserted into the buffer.
        /// <para>
        /// Stream-block boundary rules: <c>columnIndex &lt;= FirstColumn</c> shifts FirstColumn
        /// (insert lands BEFORE the block's start) but <c>columnIndex &lt; LastColumn</c>
        /// strict-less-than shifts LastColumn (insert exactly AT LastColumn lands AFTER the block).
        /// The asymmetry is intentional: typing exactly at a selection boundary should not
        /// be swallowed into the selection. Insertions strictly inside (FirstColumn &lt; col &lt; LastColumn)
        /// grow the block as expected.
        /// </para>
        /// <para>
        /// Box blocks: columns are anchored - substring edits never shift them.
        /// Line blocks: columns are not tracked.
        /// </para>
        /// </summary>
        public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
        {
            if (BlockType == TextBufferBlockType.None)
                return;

            // Only stream blocks adjust columns on insertion
            if (BlockType == TextBufferBlockType.Stream)
            {
                // Insertion on the first line before the first column - shift first column
                if (lineIndex == FirstLine && columnIndex <= FirstColumn)
                {
                    FirstColumn += length;
                }

                // Insertion on the last line before the last column - shift last column
                if (lineIndex == LastLine && columnIndex < LastColumn)
                {
                    LastColumn += length;
                }
            }
            // Box blocks stay in place - no column adjustments on insertion
            // Line blocks don't care about columns
        }

        /// <summary>
        /// Called when a substring is deleted from the buffer.
        /// <para>
        /// Stream-block deletion rules:
        /// - deletion entirely BEFORE FirstColumn shifts both edges left;
        /// - deletion straddling FirstColumn moves FirstColumn to the deletion start
        ///   (any portion of the block that was deleted is gone);
        /// - deletion exactly AT FirstColumn (deletedFirstColumn == FirstColumn) does NOT
        ///   shift FirstColumn - the block's left edge stays put, content shrinks;
        /// - same logic mirrored for LastColumn.
        /// </para>
        /// <para>
        /// Box blocks: columns are anchored; substring deletes never shift them.
        /// </para>
        /// </summary>
        public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
        {
            if (BlockType == TextBufferBlockType.None)
                return;

            int deletedFirstColumn = columnIndex;
            int deletedLastColumn = columnIndex + length;

            // Only stream blocks adjust columns on deletion
            if (BlockType == TextBufferBlockType.Stream)
            {
                // Deletion on the first line
                if (lineIndex == FirstLine)
                {
                    // Deletion completely before first column - shift first column left
                    if (deletedLastColumn <= FirstColumn)
                    {
                        FirstColumn -= length;
                    }
                    // Deletion overlaps with or encompasses first column
                    else if (deletedFirstColumn < FirstColumn)
                    {
                        // Move first column to deletion start
                        int overlap = Math.Min(FirstColumn - deletedFirstColumn, length);
                        FirstColumn -= overlap;
                    }
                }

                // Deletion on the last line
                if (lineIndex == LastLine)
                {
                    // Deletion completely before last column - shift last column left
                    if (deletedLastColumn <= LastColumn)
                    {
                        LastColumn -= length;
                    }
                    // Deletion overlaps with last column
                    else if (deletedFirstColumn < LastColumn)
                    {
                        int overlap = Math.Min(LastColumn - deletedFirstColumn, length);
                        LastColumn -= overlap;
                    }
                }
            }
            // Box blocks stay in place - no column adjustments on deletion
            // Line blocks don't care about columns
        }
    }
}
