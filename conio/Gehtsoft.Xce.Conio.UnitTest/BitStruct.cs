using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Gehtsoft.Xce.Conio.UnitTest
{
    [Collection("TypeBuilder")]
    public class BitStruct
    {
        private readonly TypeBuilderFixture mTypeBuilderFixture;
        public BitStruct(TypeBuilderFixture typeBuilderFixture)
        {
            mTypeBuilderFixture = typeBuilderFixture;
        }

        [Fact]
        public void DefaultLength_LessOneByte()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("DefaultLength_LessOneByte");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<int>("a", 3);
            var type = tb.CreateType();
            var classInfo = MarshalEx.GetClassInfo(type);
            classInfo.SizeOf.Should().Be(1);
        }

        [Fact]
        public void DefaultLength_OneByteSharp()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("DefaultLength_OneByteSharp");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<int>("a", 3);
            tb.AddBitField<int>("b", 5);
            var type = tb.CreateType();
            var classInfo = MarshalEx.GetClassInfo(type);
            classInfo.SizeOf.Should().Be(1);

            classInfo.Fields.Should().HaveCount(2);
            classInfo.Fields[0].FieldInfo.Should().NotBeNull();
            classInfo.Fields[0].FieldInfo.Name.Should().Be("a");
            classInfo.Fields[0].BitLength.Should().Be(3);
            classInfo.Fields[1].FieldInfo.Should().NotBeNull();
            classInfo.Fields[1].FieldInfo.Name.Should().Be("b");
            classInfo.Fields[1].BitLength.Should().Be(5);
        }

        [Fact]
        public void DefaultLength_TwoBytes()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("DefaultLength_TwoBytes");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<int>("a", 13);
            var type = tb.CreateType();
            var classInfo = MarshalEx.GetClassInfo(type);
            classInfo.SizeOf.Should().Be(2);
        }

        [Fact]
        public void DefaultLength_Forced()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("DefaultLength_Forced");
            tb.AddBitStructClassAttribute(3);
            tb.AddBitField<int>("a", 13);
            var type = tb.CreateType();
            var classInfo = MarshalEx.GetClassInfo(type);
            classInfo.SizeOf.Should().Be(3);

            classInfo.Fields.Should().HaveCount(1);
            classInfo.Fields[0].FieldInfo.Should().NotBeNull();
            classInfo.Fields[0].FieldInfo.Name.Should().Be("a");
            classInfo.Fields[0].BitLength.Should().Be(13);
        }

        [Fact]
        public void ExplicitOrder()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("ExplicitOrder");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<int>("a", 3, 1);
            tb.AddBitField<int>("b", 2, 1);
            tb.AddBitField<int>("c", 1, 1);
            var type = tb.CreateType();

            var classInfo = MarshalEx.GetClassInfo(type);
            classInfo.Fields.Should().HaveCount(3);
            classInfo.Fields[0].FieldInfo.Name.Should().Be("c");
            classInfo.Fields[1].FieldInfo.Name.Should().Be("b");
            classInfo.Fields[2].FieldInfo.Name.Should().Be("a");
        }
        [Fact]
        public void MixedOrder()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("MixedOrder");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<int>("a", 1);
            tb.AddBitField<int>("b", 1);
            tb.AddBitField<int>("c", 1, 1);
            var type = tb.CreateType();

            var classInfo = MarshalEx.GetClassInfo(type);
            classInfo.Fields.Should().HaveCount(3);
            classInfo.Fields[0].FieldInfo.Name.Should().Be("c");
            classInfo.Fields[1].FieldInfo.Name.Should().Be("a");
            classInfo.Fields[2].FieldInfo.Name.Should().Be("b");
        }

        [Fact]
        public void Serialization1()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("Serialization1");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<int>("a", 1);
            tb.AddBitField<int>("b", 1);
            tb.AddBitField<int>("c", 1);
            tb.AddBitField<int>("d", 1);
            tb.AddBitField<int>("e", 2);
            tb.AddBitField<int>("f", 2);
            var type = tb.CreateType();

            object o = Activator.CreateInstance(type);
            o.SetField("a", 1);
            o.SetField("b", 0);
            o.SetField("c", 1);
            o.SetField("d", 0);
            o.SetField("e", 1);
            o.SetField("f", 2);

            IntPtr v = MarshalEx.BitFieldStructToPtr(o);
            try
            {
                byte r = Marshal.ReadByte(v, 0);
                r.Should().Be(0b10010101);

                object o1 = MarshalEx.PtrToBitFieldStruct(type, v);
                o1.GetField("a").Should().Be(1);
                o1.GetField("b").Should().Be(0);
                o1.GetField("c").Should().Be(1);
                o1.GetField("d").Should().Be(0);
                o1.GetField("e").Should().Be(1);
                o1.GetField("f").Should().Be(2);
            }
            finally
            {
                Marshal.FreeCoTaskMem(v);
            }
            o.SetField("a", 0);
            o.SetField("b", 1);
            o.SetField("c", 0);
            o.SetField("d", 1);
            o.SetField("e", 2);
            o.SetField("f", 1);

            v = MarshalEx.BitFieldStructToPtr(o);
            try
            {
                byte r = Marshal.ReadByte(v, 0);
                r.Should().Be(0b01101010);

                object o1 = MarshalEx.PtrToBitFieldStruct(type, v);
                o1.GetField("a").Should().Be(0);
                o1.GetField("b").Should().Be(1);
                o1.GetField("c").Should().Be(0);
                o1.GetField("d").Should().Be(1);
                o1.GetField("e").Should().Be(2);
                o1.GetField("f").Should().Be(1);
            }
            finally
            {
                Marshal.FreeCoTaskMem(v);
            }
        }

        [Theory]
        [InlineData(0xaaaa_aaaa)]
        [InlineData(0x5555_5555)]
        [InlineData(0x0)]
        [InlineData(0xffff_ffff)]
        [InlineData(0x1234_5678)]
        [InlineData(0x8765_4321)]
        public void Serialization2(uint iv)
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder($"Serialization2_{iv:x}");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<uint>("a", 3);
            tb.AddBitField<uint>("b", 8);
            tb.AddBitField<uint>("c", 2);
            tb.AddBitField<uint>("d", 12);
            tb.AddBitField<uint>("e", 32);
            tb.AddBitField<uint>("f", 7);
            var type = tb.CreateType();

            object o = Activator.CreateInstance(type);
            o.SetField("a", iv);
            o.SetField("b", iv);
            o.SetField("c", iv);
            o.SetField("d", iv);
            o.SetField("e", iv);
            o.SetField("f", iv);

            IntPtr v = MarshalEx.BitFieldStructToPtr(o);
            object o1 = MarshalEx.PtrToBitFieldStruct(type, v);

            o1.GetField<uint>("a").Should().Be(iv & 0x7);
            o1.GetField<uint>("b").Should().Be(iv & 0xff);
            o1.GetField<uint>("c").Should().Be(iv & 0x3);
            o1.GetField<uint>("d").Should().Be(iv & 0xfff);
            o1.GetField<uint>("e").Should().Be(iv);
            o1.GetField<uint>("f").Should().Be(iv & 0x7f);
        }

        [Fact]
        public void CleanUpRest()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("CleanUpRest");
            tb.AddBitStructClassAttribute(4);
            tb.AddBitField<uint>("a", 3);
            tb.AddBitField<uint>("b", 8);
            IntPtr v = Marshal.AllocCoTaskMem(8);
            for (int i = 0; i < 8; i++)
                Marshal.WriteByte(v, i, 0xff);

            var type = tb.CreateType();

            object o = Activator.CreateInstance(type);
            o.SetField("a", 1);
            o.SetField("b", 1);

            MarshalEx.BitFieldStructToPtr(o, v, 0);
            Marshal.ReadByte(v, 0).Should().Be(0b1001);
            Marshal.ReadByte(v, 1).Should().Be(0);
            Marshal.ReadByte(v, 2).Should().Be(0);
            Marshal.ReadByte(v, 3).Should().Be(0);
            Marshal.ReadByte(v, 4).Should().Be(0xff);
            Marshal.ReadByte(v, 5).Should().Be(0xff);
            Marshal.ReadByte(v, 6).Should().Be(0xff);
            Marshal.ReadByte(v, 7).Should().Be(0xff);
        }
        [Fact]
        public void NotOverShoot()
        {
            var tb = mTypeBuilderFixture.CreateTypeBuilder("NotOverShoot");
            tb.AddBitStructClassAttribute();
            tb.AddBitField<uint>("a", 3);
            tb.AddBitField<uint>("b", 8);
            IntPtr v = Marshal.AllocCoTaskMem(8);
            for (int i = 0; i < 8; i++)
                Marshal.WriteByte(v, i, 0xff);

            var type = tb.CreateType();

            object o = Activator.CreateInstance(type);
            o.SetField("a", 0b010);
            o.SetField("b", 0b1111_1111);

            MarshalEx.BitFieldStructToPtr(o, v, 0);
            Marshal.ReadByte(v, 0).Should().Be(0b1111_1010);
            Marshal.ReadByte(v, 1).Should().Be(0b111);
            Marshal.ReadByte(v, 2).Should().Be(0xff);
            Marshal.ReadByte(v, 3).Should().Be(0xff);
            Marshal.ReadByte(v, 4).Should().Be(0xff);
            Marshal.ReadByte(v, 5).Should().Be(0xff);
            Marshal.ReadByte(v, 6).Should().Be(0xff);
            Marshal.ReadByte(v, 7).Should().Be(0xff);
        }
    }
}

