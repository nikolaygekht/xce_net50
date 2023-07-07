namespace Gehtsoft.Xce.Conio.Clipboard
{
    public interface ITextClipboard
    {
        bool IsTextAvailable(TextClipboardFormat format);
        string GetText(TextClipboardFormat format);
        string GetText();
        void SetText(string text, TextClipboardFormat format = TextClipboardFormat.UnicodeText);
    }
}
