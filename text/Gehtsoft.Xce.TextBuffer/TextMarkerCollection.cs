using System.Collections;
using System.Collections.Generic;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Collection of text markers that automatically adjusts marker positions when buffer changes
    /// </summary>
    public class TextMarkerCollection : ITextBufferCallback, IEnumerable<TextMarker>
    {
        private readonly List<TextMarker> mMarkers = new List<TextMarker>();

        /// <summary>
        /// Gets the number of markers in the collection
        /// </summary>
        public int Count => mMarkers.Count;

        /// <summary>
        /// Adds a marker to the collection
        /// </summary>
        /// <param name="marker">The marker to add</param>
        public void Add(TextMarker marker)
        {
            if (marker != null)
                mMarkers.Add(marker);
        }

        /// <summary>
        /// Removes a marker from the collection
        /// </summary>
        /// <param name="marker">The marker to remove</param>
        /// <returns>True if the marker was removed, false otherwise</returns>
        public bool Remove(TextMarker marker)
        {
            return mMarkers.Remove(marker);
        }

        /// <summary>
        /// Removes a marker by id
        /// </summary>
        /// <param name="id">The marker id to remove</param>
        /// <returns>True if a marker was removed, false otherwise</returns>
        public bool RemoveById(string id)
        {
            var marker = FindById(id);
            if (marker != null)
            {
                mMarkers.Remove(marker);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds a marker by id
        /// </summary>
        /// <param name="id">The marker id to find</param>
        /// <returns>The marker, or null if not found</returns>
        public TextMarker FindById(string id)
        {
            foreach (var marker in mMarkers)
            {
                if (marker.Id == id)
                    return marker;
            }
            return null;
        }

        /// <summary>
        /// Clears all markers from the collection
        /// </summary>
        public void Clear()
        {
            mMarkers.Clear();
        }

        /// <summary>
        /// Called when lines are inserted into the buffer
        /// </summary>
        public void OnLinesInserted(int lineIndex, int count)
        {
            // Adjust all markers that are on or after the inserted lines
            foreach (var marker in mMarkers)
            {
                if (marker.Line >= lineIndex)
                {
                    marker.Line += count;
                }
            }
        }

        /// <summary>
        /// Called when lines are deleted from the buffer
        /// </summary>
        public void OnLinesDeleted(int lineIndex, int count)
        {
            int deletedFirstLine = lineIndex;
            int deletedLastLine = lineIndex + count - 1;

            // Adjust all markers
            foreach (var marker in mMarkers)
            {
                // Marker is before deleted lines - no change
                if (marker.Line < deletedFirstLine)
                    continue;

                // Marker is after deleted lines - shift up
                if (marker.Line > deletedLastLine)
                {
                    marker.Line -= count;
                }
                // Marker is on a deleted line - move to first deleted line
                else
                {
                    marker.Line = deletedFirstLine;
                    marker.Column = 0;
                }
            }
        }

        /// <summary>
        /// Called when a substring is inserted into the buffer
        /// </summary>
        public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
        {
            // Markers don't adjust for substring operations
        }

        /// <summary>
        /// Called when a substring is deleted from the buffer
        /// </summary>
        public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
        {
            // Markers don't adjust for substring operations
        }

        /// <summary>
        /// Gets an enumerator for the markers
        /// </summary>
        public IEnumerator<TextMarker> GetEnumerator()
        {
            return mMarkers.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for the markers
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // --- snapshot support (used by BufferStateSnapshot) ---

        /// <summary>
        /// Capture each marker's identity-and-position so it can be restored later.
        /// We keep a reference to the marker object itself so the user's external
        /// references stay valid - we just write back into the same instance on restore.
        /// </summary>
        internal MarkerSnapshotEntry[] Snapshot()
        {
            var snap = new MarkerSnapshotEntry[mMarkers.Count];
            for (int i = 0; i < mMarkers.Count; i++)
            {
                var m = mMarkers[i];
                snap[i] = new MarkerSnapshotEntry(m, m.Line, m.Column);
            }
            return snap;
        }

        /// <summary>
        /// Restore the collection to exactly its snapshot state: same set of marker
        /// instances in the same order, each with its captured (line, column).
        /// Markers added since the snapshot are dropped; markers removed since the
        /// snapshot are re-added (same instance, same id) at their captured positions.
        /// External references continue to work because we restore values in-place.
        /// </summary>
        internal void RestoreFromSnapshot(MarkerSnapshotEntry[] snapshot)
        {
            if (snapshot == null) return;
            mMarkers.Clear();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                entry.Marker.Line = entry.Line;
                entry.Marker.Column = entry.Column;
                mMarkers.Add(entry.Marker);
            }
        }

        internal readonly struct MarkerSnapshotEntry
        {
            public readonly TextMarker Marker;
            public readonly int Line;
            public readonly int Column;
            public MarkerSnapshotEntry(TextMarker marker, int line, int column)
            {
                Marker = marker;
                Line = line;
                Column = column;
            }
        }
    }
}
