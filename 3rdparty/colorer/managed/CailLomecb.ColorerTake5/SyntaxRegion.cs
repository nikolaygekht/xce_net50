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
        private readonly string mName;
        private IntPtr mRegion;

        internal SyntaxRegion(IntPtr region)
        {
            StringBuilder buff = new StringBuilder(256);
            NativeExports.RegionName(region, buff, 256);
            mName = buff.ToString();
            mRegion = region;
        }

        internal SyntaxRegion(string name, IntPtr region)
        {
            mName = name;
            mRegion = region;
        }

        /// <summary>
        /// The name of the region
        /// </summary>
        public string Name => mName;

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

        /// <summary>
        /// Returns has code of the region
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => mName.GetHashCode();
    }
}
