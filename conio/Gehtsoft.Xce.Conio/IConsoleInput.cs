namespace Gehtsoft.Xce.Conio
{
    public interface IConsoleInput
    {
        ConioInputMode Mode { get; }
        bool MouseSupported { get; }
        int CurrentLayout { get; }
        bool Read(IConsoleInputListener listener, int timeout);
    }
}
