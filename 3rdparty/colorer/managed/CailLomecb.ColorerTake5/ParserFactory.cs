using System;
using System.Collections.Generic;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The parser factory.
    ///
    /// This class is a main of the Colorer-Take5 library. Use <see cref="Create(string, string, string, int)" /> method to create an instance of the factory.
    ///
    /// The class is associated with native resources and must be disposed when it is not required.
    /// NOTE: You must keep a copy of ParserFactory as long as any object created via parser (e.g. a region or a editor) is
    /// alive.
    /// </summary>
    public sealed class ParserFactory : IDisposable
    {
        private IntPtr mFactory;

        /// <summary>
        /// Creates an instance of the colorer factory.
        /// </summary>
        /// <param name="catalogue">The full path where catalog.xml file is located</param>
        /// <param name="hrdClass">The name of style class (e.g. console or rgb)</param>
        /// <param name="hrdName">The name of stylesheet</param>
        /// <param name="backParse">The maximum number of lines to parse back from the current position</param>
        /// <returns></returns>
        public static ParserFactory Create(string catalogue, string hrdClass, string hrdName, int backParse)
        {
            StringBuilder builder = new StringBuilder(1024);
            if (!NativeExports.CreateColorerFactory(catalogue, hrdClass, hrdName, out IntPtr r, builder, 1024))
                throw new InvalidOperationException(builder.ToString());
            return new ParserFactory(r, backParse);
        }

        private readonly int mBackParse;

        internal ParserFactory(IntPtr factory, int backParse)
        {
            mFactory = factory;
            mBackParse = backParse;
        }

        ~ParserFactory()
        {
            if (mFactory != IntPtr.Zero)
                NativeExports.DeleteColorerFactory(mFactory);
            mFactory = IntPtr.Zero;
        }
        public void Dispose()
        {
            if (mFactory != IntPtr.Zero)
                NativeExports.DeleteColorerFactory(mFactory);
            mFactory = IntPtr.Zero;
        }

        /// <summary>
        /// Finds a syntax region by its name.
        ///
        /// Return null if the syntax region isn't found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SyntaxRegion FindSyntaxRegion(string name)
        {
            IntPtr r = NativeExports.FindSyntaxRegion(mFactory, name);
            if (r == IntPtr.Zero)
                return null;
            return new SyntaxRegion(name, r);
        }

        /// <summary>
        /// Finds a styled region definition by its name.
        ///
        /// Returns null if the styled region definition is not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public StyledRegionDefinition FindStyledRegion(string name)
        {
            IntPtr r = NativeExports.FindStyledRegion(mFactory, name);
            if (r == IntPtr.Zero)
                return null;
            return new StyledRegionDefinition(r, name);
        }

        /// <summary>
        /// Create a editor
        /// </summary>
        /// <param name="source">The interface to the text source</param>
        /// <returns></returns>
        public ColorerBaseEditor CreateEditor(IColorerLineSource source)
        {
            ColorerLineAdapter adapter = new ColorerLineAdapter(source);
            IntPtr peditor = NativeExports.CreateBaseEditor(mFactory, adapter.NativeSource, mBackParse);
            return new ColorerBaseEditor(peditor, adapter);
        }

        public static void DoAllInOneAction() => NativeExports.AllInOneAction();
    }
}
