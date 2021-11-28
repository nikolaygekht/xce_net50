using System;
using System.Collections.Generic;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// A syntax region definition
    /// </summary>
    public sealed class SyntaxRegion : IEquatable<SyntaxRegion>
    {
        private readonly IntPtr mRegion;

        internal SyntaxRegion(IntPtr region)
        {
            StringBuilder buff = new StringBuilder(256);
            NativeExports.RegionName(region, buff, 256);
            Name = buff.ToString();
            mRegion = region;
        }

        internal SyntaxRegion(string name, IntPtr region)
        {
            Name = name;
            mRegion = region;
        }

        /// <summary>
        /// The name of the region
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Checks whether the region is derived from or is the same as the region specified
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool IsDerivedFrom(SyntaxRegion region) => NativeExports.RegionIsDerivedFrom(mRegion, region.mRegion);

        /// <summary>
        /// Checks whether both objects refer to the same region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool Equals(SyntaxRegion region) => NativeExports.RegionsAreEqual(mRegion, region.mRegion);

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is SyntaxRegion r)
                return Equals(r);
            return false;
        }

        /// <summary>
        /// Returns has code of the region
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Name.GetHashCode();
    }
}
