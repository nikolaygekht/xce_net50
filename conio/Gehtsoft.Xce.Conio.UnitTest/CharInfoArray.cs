using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Gehtsoft.Xce.Conio.UnitTest
{
    public class CharInfoArrayTest
    {
        [Fact]
        public void Length()
        {
            CharInfoArray arr = new CharInfoArray(5, 17);
            arr.Count.Should().Be(85);
        }

        [Fact]
        public void Positionioning()
        {
            CharInfoArray arr = new CharInfoArray(5, 17);
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 17; c++)
                {
                    arr[r, c].UnicodeChar = ' ';
                    arr[r, c].Attributes = (ushort)(r * 17 + c);
                }

            for (int i = 0; i < arr.Count; i++)
            {
                arr[i].UnicodeChar.Should().Be(' ');
                arr[i].Attributes.Should().Be((ushort)i);
            }
        }

        [Fact]
        public void Memory()
        {
            CharInfoArray arr = new CharInfoArray(5, 17);
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 17; c++)
                {
                    arr[r, c].UnicodeChar = ' ';
                    arr[r, c].Attributes = (ushort)(r * 17 + c);
                }

            using (var ptr = arr.GetPointer())
            {
                var sz = Marshal.SizeOf<Win32.CHAR_INFO>();
                for (int i = 0; i < arr.Count; i++)
                {
                    var p = IntPtr.Add(ptr.Pointer, i * sz);
                    var s = Marshal.PtrToStructure<Win32.CHAR_INFO>(p);
                    s.UnicodeChar.Should().Be(' ');
                    s.Attributes.Should().Be((ushort)i);
                }
            }
        }
    }
}
