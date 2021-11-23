using System;

namespace Gehtsoft.Xce.Conio
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BitStructAttribute : Attribute
    {
        public int SizeOf { get; set; }

        public BitStructAttribute()
        {

        }

        public BitStructAttribute(int sizeOf)
        {
            SizeOf = sizeOf;
        }
    }
}
