using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The syntax highlighter for one text file. 
    /// 
    /// Use <see cref="ParserFactory.CreateEditor(IColorerLineSource)" to create an instance of the editor. />
    /// 
    /// The class is associated with native resources and must be disposed if it is not used anymore. 
    /// </summary>
    public sealed class ColorerBaseEditor : IDisposable
    {
        private IntPtr mEditor;
        private ColorerLineAdapter mAdapter;

        internal ColorerBaseEditor(IntPtr editor, ColorerLineAdapter adapter)
        {
            mEditor = editor;
            mAdapter = adapter;
        }

        ~ColorerBaseEditor()
        {
            if (mEditor != IntPtr.Zero)
                NativeExports.DeleteBaseEditor(mEditor);
            mEditor = IntPtr.Zero;
            mAdapter?.Dispose();
            mAdapter = null;
        }
        public void Dispose()
        {
            if (mEditor != IntPtr.Zero)
                NativeExports.DeleteBaseEditor(mEditor);
            mEditor = IntPtr.Zero;
            mAdapter?.Dispose();
            mAdapter = null;
        }

        /// <summary>
        /// Notifies the parser that the number of lines is changed.
        /// 
        /// You should also use it at initialization to set initial line(s) count. 
        /// </summary>
        /// <param name="linesCount"></param>
        public void NotifyLineCount(int linesCount) => NativeExports.BaseEditorNotifyLineCount(mEditor, linesCount);
        /// <summary>
        /// Notifes the parser that the file name is changed.
        /// 
        /// You should also use it at initialization to set the file name.
        /// </summary>
        /// <param name="newName"></param>
        public void NotifyNameChange(string newName) => NativeExports.BaseEditorFileNameChange(mEditor, newName);
        /// <summary>
        /// Notifies the parser about idle state. 
        /// 
        /// The parser uses idle state to complete low-prioirty parsing. 
        /// </summary>
        public void NotifyIdle() => NativeExports.BaseEditorIdle(mEditor);
        /// <summary>
        /// Notifies that small changes occurred in the line specified. 
        /// </summary>
        /// <param name="line"></param>
        public void NotifyLineChange(int line) => NativeExports.BaseEditorLineChange(mEditor, line);
        /// <summary>
        /// Notifies that a large change affecting multiple lines occurred starting at the line specified. 
        /// </summary>
        /// <param name="line"></param>
        public void NotifyMajorChange(int line) => NativeExports.BaseEditorMajorChange(mEditor, line);
        /// <summary>
        /// Notifies that the specified range became visible. 
        /// 
        /// This operation rasises priority of parsing for the region.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="linesCount"></param>
        public void NotifyLinesVisible(int line, int linesCount) => NativeExports.BaseEditorVisibleLine(mEditor, line, linesCount);
        /// <summary>
        /// Forces the line range to be validated. 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineTo"></param>
        public void ValidateLines(int line, int lineTo) => NativeExports.BaseEditorValidateRange(mEditor, line, lineTo);

        /// <summary>
        /// Return the name of the syntax scheme used for the file.
        /// </summary>
        public string FileType
        {
            get
            {
                StringBuilder type = new StringBuilder(256);
                if (!NativeExports.BaseEditorGetFileType(mEditor, type, 256))
                    return null;
                return type.ToString();
            }
        }

        /// <summary>
        /// Returns the maxmium (last) line that is already parsed.
        /// </summary>
        public int LastValid => NativeExports.BaseEditorLastValidLine(mEditor);

        /// <summary>
        /// Gets the first syntax region of the line specified. 
        /// 
        /// Returns null if there is no syntax regions.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public LineRegion GetFirstRegionInLine(int line)
        {
            IntPtr r = NativeExports.BaseEditorFirstLineRegion(mEditor, line);
            if (r == IntPtr.Zero)
                return null;
            return new LineRegion(line, r, true);


        }

        /// <summary>
        /// Finds a match between syntax-connected pair (e.g. brackets). 
        /// 
        /// Returns null if there is no part at the position specified. 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public PairMatch FindPairMatch(int line, int position)
        {
            IntPtr r = NativeExports.BaseEditorPairMatch(mEditor, line, position);
            if (r == IntPtr.Zero)
                return null;
            return new PairMatch(mEditor, r);
        }
    }
}
