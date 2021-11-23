using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Gehtsoft.Xce.Conio
{
    internal static partial class MarshalEx
    {
        internal class MarshalFieldInfo
        {
            public int Order { get; set; }
            public int BitLength { get; set; }
            public int FirstByte { get; set; }
            public int OffsetInFirstByte { get; set; }
            public FieldInfo FieldInfo { get; set; }
        }

        internal class MarshalClassInfo
        {

            public int SizeOf { get; set; }
            public List<MarshalFieldInfo> Fields { get; } = new List<MarshalFieldInfo>();
        }

        private static object gDictionaryMutex = new object();
        private static Dictionary<Type, MarshalClassInfo> gDictionary = new Dictionary<Type, MarshalClassInfo>();

        internal static MarshalClassInfo GetClassInfo(Type type)
        {
            lock (gDictionaryMutex)
            {
                if (gDictionary.TryGetValue(type, out MarshalClassInfo classInfo))
                    return classInfo;

                var classAttr = type.GetCustomAttribute<BitStructAttribute>();
                if (classAttr == null)
                    throw new ArgumentException($"The type must have {nameof(BitStructAttribute)}", nameof(type));

                classInfo = new MarshalClassInfo();
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                int or = 10000;
                int size = 0;
                foreach (FieldInfo field in fields)
                {
                    var fieldAttr = field.GetCustomAttribute<BitFieldAttribute>();
                    if (fieldAttr != null)
                    {
                        int order = fieldAttr.Order;
                        int length = fieldAttr.BitLength;
                        if (length < 1 || length > 32)
                            throw new ArgumentException("A bit length of the structure should not exceed 64 bits", nameof(type));
                            
                        size += length;
                        if (order < 0)
                            order = or++;
                        classInfo.Fields.Add(new MarshalFieldInfo() { Order = order, BitLength = length, FieldInfo = field });
                    }
                }
                if (classInfo.Fields.Count == 0)
                    throw new ArgumentException($"The type has not fields attributed with {nameof(BitFieldAttribute)}", nameof(type));

                int byteSize = size / 8;
                if (size % 8 != 0)
                    byteSize++;
                if (classAttr.SizeOf > 0)
                {
                    if (classAttr.SizeOf < byteSize)
                        throw new ArgumentException("The forced structure size is shorter than it is needed for all fields", nameof(type));
                    classInfo.SizeOf = classAttr.SizeOf;
                }
                else
                    classInfo.SizeOf = byteSize;
                classInfo.Fields.Sort((a, b) => a.Order.CompareTo(b.Order));
                gDictionary[type] = classInfo;
                return classInfo;
            }
        }

        public static int BitFieldStructSizeOf(Type type)
        {
            var info = GetClassInfo(type);
            return info.SizeOf;
        }

        public static int BitFieldStructSizeOf<T>() => BitFieldStructSizeOf(typeof(T));

        private static void BitFieldStructToPtr(object structure, IntPtr address, int offset, MarshalClassInfo info)
        {
            int dstIndex = offset;
            int currPos = 0;
            byte currentByte = 0;
            int bw = 0;

            for (int i = 0; i < info.Fields.Count; i++)
            {
                var f = info.Fields[i];
                var v = f.FieldInfo.GetValue(structure);
                uint iv = (uint)Convert.ChangeType(v, typeof(uint));

                for (int j = 0; j < f.BitLength; j++, iv >>= 1)
                {
                    byte bit = (byte)(iv & 0x1);
                    if (currPos > 0)
                        bit <<= currPos;
                    currentByte |= bit;
                    currPos++;
                    if (currPos == 8)
                    {
                        Marshal.WriteByte(address, dstIndex, currentByte);
                        bw++;
                        dstIndex++;
                        currentByte = 0;
                        currPos = 0;
                    }
                }
            }
            if (currPos > 0)
            {
                Marshal.WriteByte(address, dstIndex++, currentByte);
                bw++;
            }
            currentByte = 0;
            while (bw < info.SizeOf)
            {
                Marshal.WriteByte(address, dstIndex++, currentByte);
                bw++;
            }


        }

        public static void BitFieldStructToPtr(object structure, IntPtr address, int offset = 0)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));

            MarshalClassInfo info = GetClassInfo(structure.GetType());
            BitFieldStructToPtr(structure, address, offset, info);
        }

        public static IntPtr BitFieldStructToPtr(object structure)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));

            MarshalClassInfo info = GetClassInfo(structure.GetType());
            IntPtr address = Marshal.AllocCoTaskMem(info.SizeOf);
            BitFieldStructToPtr(structure, address, 0, info);
            return address;
        }

        public static T PtrToBitFieldStruct<T>(IntPtr memory, int offset = 0) => (T)PtrToBitFieldStruct(typeof(T), memory, offset);

        public static object PtrToBitFieldStruct(Type type, IntPtr memory, int offset = 0)
        {
            var info = GetClassInfo(type);
            IntPtr res = Marshal.AllocCoTaskMem(info.SizeOf);
            object structure = Activator.CreateInstance(type);

            int srcIndex = offset;
            int currPos = 0;
            byte currentByte = Marshal.ReadByte(memory, srcIndex);         

            for (int i = 0; i < info.Fields.Count; i++)
            {
                var f = info.Fields[i];
                uint currValue = 0;
                for (int j = 0; j < f.BitLength; j++)
                {
                    if (currPos == 8)
                    {
                        srcIndex++;
                        currPos = 0;
                        currentByte = Marshal.ReadByte(memory, srcIndex);
                    }

                    uint bit = (uint)(currentByte & 0x1);
                    if (j > 0)
                        bit <<= j;

                    currValue |= bit;

                    currPos++;
                    currentByte >>= 1;
                }

                object cv = Convert.ChangeType(currValue, f.FieldInfo.FieldType);
                f.FieldInfo.SetValue(structure, cv);
            }
            return structure;
        }
    }
}
