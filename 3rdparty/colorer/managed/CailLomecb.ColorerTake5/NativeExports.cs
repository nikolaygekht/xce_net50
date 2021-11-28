using NativeLibraryManager;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    internal class NativeExports
    {
        static NativeExports()
        {
            var accessor = new ResourceAccessor(typeof(NativeExports).Assembly);
            var libManager = new LibraryManager(
                new LibraryItem(Platform.Linux, Bitness.x64,
                    new LibraryFile("libcolorertake5.so", accessor.Binary("runtimes.linux_x64.native.colorertake5.so"))),
                new LibraryItem(Platform.Windows, Bitness.x64,
                    new LibraryFile("colorertake5.dll", accessor.Binary("runtimes.win_x64.native.colorertake5.dll"))),
                new LibraryItem(Platform.Windows, Bitness.x32,
                    new LibraryFile("colorertake5.dll", accessor.Binary("runtimes.win_x86.native.colorertake5.dll"))));
            libManager.LoadNativeLibrary();
        }

        #region Regex
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int CreateRegex(string regex, out IntPtr regexPointer);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void DeleteRegex(IntPtr regexPointer);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool RegexParse(IntPtr regexPointer, string textToParse, int start, int end, out IntPtr matchesPtr);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool RegexFind(IntPtr regexPointer, string textToParse, int start, int end, out IntPtr matchesPtr);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void DeleteMatches(IntPtr matchesPtr);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int MatchesCount(IntPtr matchesPtr);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void MatchGet(IntPtr matchesPtr, int index, out int s, out int e);

        internal delegate bool RegexFindCallback(int start, int end);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void RegexFindAll(IntPtr regexPointer, string textToParse, int start, int end, RegexFindCallback callback);
        #endregion

        #region SyntaxRegion 
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool RegionIsDerivedFrom(IntPtr region, IntPtr parentRegion);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool RegionsAreEqual(IntPtr region, IntPtr parentRegion);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool RegionName(IntPtr region, StringBuilder name, int nameMax);
        #endregion

        #region StyledRegion
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int StyledRegionConsoleColor(IntPtr region);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern uint StyledRegionForegroundColor(IntPtr region);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern uint StyledRegionBackgroundColor(IntPtr region);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int StyledRegionStyle(IntPtr region);

        #endregion

        #region Line Source
        internal delegate void LineSourceLineContent(int lineNo);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void CreateColorerLineSourceAdapter(LineSourceLineContent contentAccessor, out IntPtr nativeAdapter);

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void DeleteColorerLineSourceAdapter(IntPtr nativeAdapter);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void ColorerLineSourceAdapterAcceptLine(IntPtr nativeAdapter, string line, int length);
        #endregion

        #region Parser Factory
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool CreateColorerFactory(string catalogue, string hrdClass, string hrdName, out IntPtr factory, StringBuilder outputError, int outputErrorMaxLength);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void DeleteColorerFactory(IntPtr colorerFactory);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindSyntaxRegion(IntPtr colorerFactory, string name);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindStyledRegion(IntPtr colorerFactory, string name);
        #endregion

        #region BaseEditor
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateBaseEditor(IntPtr factory, IntPtr adapter, int backparse);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr DeleteBaseEditor(IntPtr editor);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorNotifyLineCount(IntPtr editor, int lineCount);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorVisibleLine(IntPtr editor, int line, int lineCount);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorLineChange(IntPtr editor, int line);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorMajorChange(IntPtr editor, int line);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorValidateRange(IntPtr editor, int lineFrom, int lineTo);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorIdle(IntPtr editor);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void BaseEditorFileNameChange(IntPtr editor, string newName);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool BaseEditorGetFileType(IntPtr editor, StringBuilder buffer, int length);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int BaseEditorLastValidLine(IntPtr editor);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr BaseEditorFirstLineRegion(IntPtr editor, int line);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr BaseEditorPairMatch(IntPtr editor, int line, int position);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr BaseEditorReleaseMatch(IntPtr editor, IntPtr pairMatch);
        #endregion

        #region LineRegion
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LineRegionNext(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LineRegionPrev(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int LineRegionStart(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int LineRegionEnd(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LineRegionSyntaxRegion(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LineRegionStyledRegion(IntPtr pobj);
        #endregion

        #region PairMatch
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int PairMatchStartLine(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int PairMatchEndLine(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr PairMatchStartLineRegion(IntPtr pobj);
        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr PairMatchEndLineRegion(IntPtr pobj);
        #endregion

        [DllImport("colorertake5", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void AllInOneAction();
    }
}
