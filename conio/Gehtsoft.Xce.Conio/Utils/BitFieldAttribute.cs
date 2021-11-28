using System;
using System.Collections.Concurrent;

namespace Gehtsoft.Xce.Conio
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class BitFieldAttribute : Attribute
    {
        public int Order { get; set; }
        public int BitLength { get; set; }

        public BitFieldAttribute()
        {
            Order = -1;
            BitLength = 1;
        }

        public BitFieldAttribute(int bitlength)
        {
            Order = -1;
            BitLength = bitlength;
        }

        public BitFieldAttribute(int order, int bitlength)
        {
            Order = order;
            BitLength = bitlength;
        }
    }
}
