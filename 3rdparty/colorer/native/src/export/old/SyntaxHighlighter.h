class ParserFactory;
class BaseEditor;
class StyledHRDMapper;
class LineRegion;
class PairMatch;

namespace gehtsoft
{
namespace xce
{
namespace colorer
{

interface class ILineSource;
ref class LineSourceAdapter;
ref class SyntaxRegion;

public ref class SyntaxHighlighterRegion
{
 private:
    const ::Region *mHrcRegion;
    int mLine;
    int mStartColumn;
    int mEndColumn;
    bool mIsSyntaxRegion;
    bool mHasColor;
    short mConsoleColor;
    int mForeColor;
    int mBackColor;
    int mStyle;
    System::String ^mName;
    System::String ^mDerivedFrom;
 internal:
    void Set(int line, ::LineRegion *region);
    SyntaxHighlighterRegion();
 public:
    property int Line
    {
        int get();
    }

    property int StartColumn
    {
        int get();
    };

    property int EndColumn
    {
        int get();
    };

    property int Length
    {
        int get();
    };

    property bool IsSyntaxRegion
    {
        bool get();
    };

    property bool HasColor
    {
        bool get();
    };

    property short ConsoleColor
    {
        short get();
    };

    property int ForeColor
    {
        int get();
    };

    property int Style
    {
        int get();
    };

    property int BackColor
    {
        int get();
    };

    property System::String ^Name
    {
        System::String ^get();
    }

    property System::String ^DerivedFrom
    {
        System::String ^get();
    };


    bool Is(SyntaxRegion ^type);
};

public ref class SyntaxHighlighterPair
{
 private:
    ::BaseEditor *mEditor;
    SyntaxHighlighterRegion ^mStart;
    SyntaxHighlighterRegion ^mEnd;

 internal:
    SyntaxHighlighterPair(::BaseEditor *editor, ::PairMatch *match);
 public:
    ~SyntaxHighlighterPair();
    !SyntaxHighlighterPair();

    property SyntaxHighlighterRegion ^Start
    {
        SyntaxHighlighterRegion ^get();
    }

    property SyntaxHighlighterRegion ^End
    {
        SyntaxHighlighterRegion ^get();
    }
};

public ref class SyntaxHighlighter
{
 internal:
    SyntaxHighlighter(::ParserFactory *factory, StyledHRDMapper *regionMapper, ILineSource ^lineSource, int backParse);
    ::BaseEditor *mEditor;
 public:
    ~SyntaxHighlighter();
    !SyntaxHighlighter();

    void Colorize(int lineFrom, int lineCount);
    void NotifyLineChange(int line);
    void NotifyMajorChange(int lineFrom);
    void NotifyIdle();
    void NotifyFileNameChange();
    bool GetFirstRegion(int line);
    bool GetNextRegion();
    SyntaxHighlighterPair ^MatchPair(int line, int column);
    void IdleJob(int timeout);
    void ValidateRange(int from, int to);
    void SetVisibleRange(int from, int length);

    property SyntaxHighlighterRegion ^CurrentRegion
    {
        SyntaxHighlighterRegion ^get();
    };

    property System::String ^FileType
    {
        System::String ^get();
    }

    property int LastValidLine
    {
        int get();
    }
 private:
    LineSourceAdapter ^mAdapter;
    ILineSource ^mSource;
    cli::array<SyntaxHighlighterRegion ^> ^mLastRegionGet;
    int mLastRegionGetCount;
    int mLastRegionGetInt;
};

}
}
}
