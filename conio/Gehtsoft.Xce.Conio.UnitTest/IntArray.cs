using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Gehtsoft.Xce.Conio;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable IDE0052, S4487 // Remove unread private members

namespace Gehtsoft.Xce.Conio.UnitTest
{
    public class IntArrayTest
    {
        private readonly ITestOutputHelper mOutput;

        public IntArrayTest(ITestOutputHelper output)
        {
            mOutput = output;
        }

        [Fact]
        public void Length()
        {
            IntArray arr = new IntArray(5, 17);
            arr.Count.Should().Be(85);
        }

        [Fact]
        public void Initial()
        {
            IntArray arr = new IntArray(5, 17);
            for (int i = 0; i < arr.Count; i++)
                arr[i].Should().Be(-1);
        }

        [Fact]
        public void Positioning()
        {
            IntArray arr = new IntArray(5, 17);

            for (int i = 0; i < arr.Count; i++)
                arr[i] = i;

            for (int i = 0; i < arr.Count; i++)
                arr[i].Should().Be(i);

            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 17; c++)
                    arr[r, c].Should().Be(r * 17 + c);
        }
    }
}
