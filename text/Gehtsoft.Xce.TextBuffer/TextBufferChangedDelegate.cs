using System;
using System.Collections.Generic;
using System.Text;
using Scintilla.CellBuffer;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// The delegate to be invoked when the text buffer content is changed
    /// </summary>
    /// <param name="lineFrom"></param>
    /// <param name="lineTo"></param>
    public delegate void TextBufferChangedDelegate(TextBuffer textBuffer, int lineFrom, int lineTo);
}
