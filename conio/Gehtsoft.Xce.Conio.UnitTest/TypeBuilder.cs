using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Xunit;

namespace Gehtsoft.Xce.Conio.UnitTest
{
    public class TypeBuilderFixture : IDisposable
    {
        private AssemblyBuilder mAssemblyBuilder;
        private ModuleBuilder mModuleBuilder;

        public TypeBuilderFixture()
        {
            var typeSignature = "DynamicTestTypes";
            var an = new AssemblyName(typeSignature);
            mAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            mModuleBuilder = mAssemblyBuilder.DefineDynamicModule("MainModule");
        }

        public TypeBuilder CreateTypeBuilder(string name)
        {
            return mModuleBuilder.DefineType(name,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition("TypeBuilder")]
    public class TypeBuilderFixtureCollection : ICollectionFixture<TypeBuilderFixture>
    {
    }
}
