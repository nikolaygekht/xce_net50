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
    }
}
