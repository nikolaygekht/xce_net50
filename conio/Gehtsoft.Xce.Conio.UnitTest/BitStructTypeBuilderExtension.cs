using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Gehtsoft.Xce.Conio.UnitTest
{
    public static class BitStructTypeBuilderExtension
    {
        public static void AddBitStructClassAttribute(this TypeBuilder typeBuilder)
        {
            var constructor = typeof(BitStructAttribute).GetConstructor(Array.Empty<Type>());
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, Array.Empty<object>()));
        }

        public static void AddBitStructClassAttribute(this TypeBuilder typeBuilder, int size)
        {
            var constructor = typeof(BitStructAttribute).GetConstructor(new Type[] { typeof(int) });
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { size }));
        }

        public static void AddField(this TypeBuilder typeBuilder, string name, Type type)
        {
            _ = typeBuilder.DefineField(name, type, FieldAttributes.Public);
        }

        public static void AddBitField(this TypeBuilder typeBuilder, string name, Type type)
        {
            var fieldBuilder = typeBuilder.DefineField(name, type, FieldAttributes.Public);
            var constructor = typeof(BitFieldAttribute).GetConstructor(Array.Empty<Type>());
            fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, Array.Empty<object>()));
        }

        public static void AddBitField(this TypeBuilder typeBuilder, string name, Type type, int bitLength)
        {
            var fieldBuilder = typeBuilder.DefineField(name, type, FieldAttributes.Public);
            var constructor = typeof(BitFieldAttribute).GetConstructor(new Type[] { typeof(int) });
            fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { bitLength }));
        }
        public static void AddBitField(this TypeBuilder typeBuilder, string name, Type type, int order, int bitLength)
        {
            var fieldBuilder = typeBuilder.DefineField(name, type, FieldAttributes.Public);
            var constructor = typeof(BitFieldAttribute).GetConstructor(new Type[] { typeof(int), typeof(int) });
            fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { order, bitLength }));
        }

        public static void AddField<T>(this TypeBuilder typeBuilder, string name) => AddField(typeBuilder, name, typeof(T));

        public static void AddBitField<T>(this TypeBuilder typeBuilder, string name) => AddBitField(typeBuilder, name, typeof(T));

        public static void AddBitField<T>(this TypeBuilder typeBuilder, string name, int bitLength) => AddBitField(typeBuilder, name, typeof(T), bitLength);

        public static void AddBitField<T>(this TypeBuilder typeBuilder, string name, int order, int bitLength) => AddBitField(typeBuilder, name, typeof(T), order, bitLength);

        public static object GetField(this object x, string name)
        {
            FieldInfo fi = x.GetType().GetField(name);
            return fi.GetValue(x);
        }

        public static T GetField<T>(this object x, string name)
        {
            object v = GetField(x, name);
            if (v == null)
                return default;
            if (v.GetType() != typeof(T))
                v = Convert.ChangeType(v, typeof(T));
            return (T)v;
        }

        public static void SetField(this object x, string name, object value)
        {
            FieldInfo fi = x.GetType().GetField(name);
            if (value != null && value.GetType() != fi.FieldType)
                value = Convert.ChangeType(value, fi.FieldType);
            fi.SetValue(x, value);
        }
    }
}
