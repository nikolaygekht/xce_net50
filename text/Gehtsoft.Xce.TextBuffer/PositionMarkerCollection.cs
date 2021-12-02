using System.Collections;
using System.Collections.Generic;

namespace Gehtsoft.Xce.TextBuffer
{
    internal sealed class PositionMarkerCollection : IReadOnlyList<PositionMarker>
    {
        private readonly List<PositionMarker> mList = new List<PositionMarker>();

        public PositionMarkerCollection()
        {
            for (int i = 0; i < 10; i++)
                mList.Add(new PositionMarker(i.ToString(), -1, -1));
        }

        public PositionMarker this[int index] => mList[index];

        public int Count => mList.Count;

        public IEnumerator<PositionMarker> GetEnumerator()
        {
            return ((IEnumerable<PositionMarker>)mList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)mList).GetEnumerator();
        }
    }
}

