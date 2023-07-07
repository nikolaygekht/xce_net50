#pragma warning disable S2696 // Instance members should not write to "static" fields


namespace Gehtsoft.Xce.Conio.Clipboard
{
    internal class NetTextClipboard : ITextClipboard
    {
        private static string mClipboardContent;

        public string GetText(TextClipboardFormat format)
        {
            return mClipboardContent;
        }

        public string GetText()
        {
            return mClipboardContent;
        }

        public bool IsTextAvailable(TextClipboardFormat format)
        {
            return !string.IsNullOrEmpty(mClipboardContent);
        }

        public void SetText(string text, TextClipboardFormat format = TextClipboardFormat.UnicodeText)
        {
            mClipboardContent = text;
        }
    }
}
