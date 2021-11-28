using System;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The definition of a styled region
    /// </summary>
    public class StyledRegionDefinition : StyledRegion
    {
        /// <summary>
        /// The name of the definition.
        /// </summary>
        public string Name { get; }

        internal StyledRegionDefinition(IntPtr region, string name) : base(region)
        {
            Name = name;
        }
    }
}
