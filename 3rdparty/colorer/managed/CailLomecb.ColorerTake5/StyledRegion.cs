using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// A style of a region.
    /// </summary>
    public class StyledRegion 
    {
        /// <summary>
        /// Styles that may be applied to a styled region
        /// </summary>
        [Flags]
        public enum Styles
        {
            Bold = 0x1,
            Italic =0x2,
            Underline = 0x4,
            StrikeOut = 0x8
        };

        private readonly IntPtr mRegion;

        internal StyledRegion(IntPtr region)
        {
            mRegion = region;
        }

        /// <summary>
        /// The style of the text
        /// </summary>
        public Styles Style => (Styles)NativeExports.StyledRegionStyle(mRegion);

        /// <summary>
        /// The console color (8 bit, 4 bits for foreground color, 4 bits for background color).
        /// </summary>
        public byte ConsoleColor => (byte)(NativeExports.StyledRegionConsoleColor(mRegion) & 0xff);

        /// <summary>
        /// RGB color for foreground
        /// </summary>
        public uint Foreground => NativeExports.StyledRegionForegroundColor(mRegion);
        /// <summary>
        /// RFB color for background
        /// </summary>
        public uint Background => NativeExports.StyledRegionBackgroundColor(mRegion);
    }

    /// <summary>
    /// The definition of a styled region
    /// </summary>
    public class StyledRegionDefinition : StyledRegion
    {
        /// <summary>
        /// The name of the definition.
        /// </summary>
        public string Name { get; private set; }

        internal StyledRegionDefinition(IntPtr region, string name) : base(region)
        {
            Name = name;
        }
    }
}
