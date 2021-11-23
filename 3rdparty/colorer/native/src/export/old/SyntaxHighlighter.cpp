#pragma unmanaged
#include "colorer/parserFactory.h"
#include "colorer/editor/baseEditor.h"
#include "ColorerAdapterNative.h"
#include <string>

#pragma managed
#using <mscorlib.dll>
using namespace System::Runtime::InteropServices;
#include "ColorerFactory.h"
#include "ColorerAdapter.h"
#include "SyntaxHighlighter.h"

namespace gehtsoft
{
namespace xce
{
namespace colorer
{

SyntaxHighlighterRegion::SyntaxHighlighterRegion()
{
    mHrcRegion = 0;
    mLine = 0;
    mStartColumn = 0;
    mEndColumn = 0;
    mIsSyntaxRegion = false;
    mHasColor = false;
    mConsoleColor = 0x0;
    mForeColor = -1;
    mBackColor = -1;
    mStyle = 0;
    mName = nullptr;
    mDerivedFrom = nullptr;
}

void SyntaxHighlighterRegion::Set(int line, ::LineRegion *region)
{
    mLine = line;
    if (region != 0)
    {
        mHrcRegion = region->region;
        mStartColumn = region->start;
        mEndColumn = region->end;
        mIsSyntaxRegion = mHrcRegion != 0;
        mHasColor = region->rdef != 0 && region->special == 0;
        if (mHasColor)
        {
            const ::StyledRegion *styled = region->styled();
            if (styled == 0)
            {
                mConsoleColor = 0x00;
                mForeColor = -1;
                mBackColor = -1;
            }
            else
            {
                mConsoleColor = (styled->cfore & 0xf) | ((styled->cback << 4) & 0xf0);
                mForeColor = styled->fore;
                mBackColor = styled->back;
                mStyle = styled->style;
            }
        }
        else
        {
            mConsoleColor = 0;
            mBackColor = -1;
            mForeColor = -1;
            mStyle = 0;
        }
        mName = nullptr;
        mDerivedFrom = nullptr;
    }
    else
    {
        mHrcRegion = 0;
        mLine = 0;
        mStartColumn = 0;
        mEndColumn = 0;
        mIsSyntaxRegion = false;
        mHasColor = false;
        mConsoleColor = 0x0;
        mForeColor = -1;
        mBackColor = -1;
        mStyle = 0;
        mName = nullptr;
        mDerivedFrom = nullptr;
    }
}

int SyntaxHighlighterRegion::StartColumn::get()
{
    return mStartColumn;
};

int SyntaxHighlighterRegion::Line::get()
{
    return mLine;
};

int SyntaxHighlighterRegion::EndColumn::get()
{
    return mEndColumn;
};

int SyntaxHighlighterRegion::Length::get()
{
    return mEndColumn - mStartColumn;
}

bool SyntaxHighlighterRegion::IsSyntaxRegion::get()
{
    return mIsSyntaxRegion;
}

bool SyntaxHighlighterRegion::HasColor::get()
{
    return mHasColor;
}

short SyntaxHighlighterRegion::ConsoleColor::get()
{
    return mConsoleColor;
}

int SyntaxHighlighterRegion::ForeColor::get()
{
    return mForeColor;
}

int SyntaxHighlighterRegion::BackColor::get()
{
    return mBackColor;
}

int SyntaxHighlighterRegion::Style::get()
{
    return mStyle;
}

System::String ^SyntaxHighlighterRegion::Name::get()
{
    if (mHrcRegion == 0)
        return nullptr;
    if (mName == nullptr)
    {
        System::IntPtr ptr((void *)mHrcRegion->getName()->getChars());
        mName = Marshal::PtrToStringAnsi(ptr);
    }
    return mName;
}

System::String ^SyntaxHighlighterRegion::DerivedFrom::get()
{
    if (mDerivedFrom != nullptr)
        return mDerivedFrom;

    if (mHrcRegion != 0)
    {
        const Region *r = mHrcRegion;
        std::string res;
        while (true)
        {
            if (res.length() != 0)
                res += "<-";
            res += r->getName()->getChars();
            if (r->getParent())
                r = r->getParent();
            else
                break;
        }
        System::IntPtr ptr((void *)(res.c_str()));
        mDerivedFrom = Marshal::PtrToStringAnsi(ptr);
        return mDerivedFrom;
    }
    else
        return "";
}

bool SyntaxHighlighterRegion::Is(SyntaxRegion ^type)
{
    if (type == nullptr)
        throw gcnew System::ArgumentNullException("type");
    if (mHrcRegion == 0)
        return false;
    return mHrcRegion->getID() == type->Native()->getID() || mHrcRegion->hasParent(type->Native());
};

SyntaxHighlighterPair::SyntaxHighlighterPair(::BaseEditor *editor, ::PairMatch *match)
{
    mEditor = editor;
    mStart = gcnew SyntaxHighlighterRegion();
    mStart->Set(match->sline, match->start);
    mEnd = gcnew SyntaxHighlighterRegion();
    mEnd->Set(match->eline, match->end);
}

SyntaxHighlighterPair::~SyntaxHighlighterPair()
{
    this->!SyntaxHighlighterPair();
}

SyntaxHighlighterPair::!SyntaxHighlighterPair()
{
    mStart->Set(0, 0);
    mEnd->Set(0, 0);
}

SyntaxHighlighterRegion ^SyntaxHighlighterPair::Start::get()
{
    return mStart;
}

SyntaxHighlighterRegion ^SyntaxHighlighterPair::End::get()
{
    return mEnd;
}

SyntaxHighlighter::SyntaxHighlighter(::ParserFactory *factory, StyledHRDMapper *regionMapper, ILineSource ^lineSource, int backParse)
{
    mAdapter = gcnew LineSourceAdapter(lineSource);
    mSource = lineSource;
    mEditor = new BaseEditor(factory, mAdapter->getNativeAdapter());
    mLastRegionGet = gcnew cli::array<SyntaxHighlighterRegion ^>(1024);
    mLastRegionGetCount = 0;
    mLastRegionGetInt = 0;

    System::String ^name = lineSource->GetFileName();
    if (name != nullptr)
    {
        System::IntPtr _name = Marshal::StringToCoTaskMemUni(name);
        ::DString *__name = new ::DString((const wchar *)_name.ToPointer());
        mEditor->chooseFileType(__name);
        delete __name;
        Marshal::FreeCoTaskMem(_name);
    }
    mEditor->setRegionCompact(true);
    mEditor->setRegionMapper(regionMapper);
    mEditor->setBackParse(backParse);
    mEditor->lineCountEvent(mSource->GetLinesCount());
}

SyntaxHighlighter::~SyntaxHighlighter()
{
    this->!SyntaxHighlighter();
}

SyntaxHighlighter::!SyntaxHighlighter()
{
    if (mEditor != 0)
        delete mEditor;
    mEditor = 0;
    if (mAdapter != nullptr)
        delete mAdapter;
    mAdapter = nullptr;
}

void SyntaxHighlighter::Colorize(int lineFrom, int lineCount)
{
    if (mEditor != 0)
    {
        mEditor->lineCountEvent(mSource->GetLinesCount());
        mEditor->visibleTextEvent(lineFrom, lineCount);
    }
}

void SyntaxHighlighter::NotifyLineChange(int line)
{
    if (mEditor != 0)
    {
        mEditor->modifyLineEvent(line);
        mEditor->lineCountEvent(mSource->GetLinesCount());
    }
}

void SyntaxHighlighter::NotifyMajorChange(int lineFrom)
{
    if (mEditor != 0)
    {
        mEditor->modifyEvent(lineFrom);
        mEditor->lineCountEvent(mSource->GetLinesCount());
    }
}

void SyntaxHighlighter::NotifyIdle()
{
    if (mEditor != 0)
    {
        mEditor->idleJob(1);
    }
}

void SyntaxHighlighter::NotifyFileNameChange()
{
    if (mEditor != null)
    {
        System::String ^name = mSource->GetFileName();
        if (name != nullptr)
        {
            System::IntPtr _name = Marshal::StringToCoTaskMemUni(name);
            ::DString *__name = new ::DString((const wchar *)_name.ToPointer());
            mEditor->chooseFileType(__name);
            delete __name;
            Marshal::FreeCoTaskMem(_name);
        }
    }
}

System::String ^SyntaxHighlighter::FileType::get()
{
    if (mEditor == null)
        return nullptr;
    ::FileType *t = mEditor->getFileType();
    if (t == 0)
        return nullptr;
    const ::String *n = t->getName();
    if (n == 0)
        return nullptr;
    const char *n1 = n->getChars();
    if (n1 == 0)
        return nullptr;
    System::IntPtr ptr((void *)n1);
    return Marshal::PtrToStringAnsi(ptr);
}

bool SyntaxHighlighter::GetFirstRegion(int line)
{
    if (mEditor != null)
    {
        mLastRegionGetCount = 0;
        mLastRegionGetInt = 0;
        int i;
        ::LineRegion *region;
        for (i = 0, region = mEditor->getLineRegions(line); region != 0 && i < mLastRegionGet->Length; region = region->next, mLastRegionGetCount++, i++)
        {
            if (mLastRegionGet[i] == nullptr)
                mLastRegionGet[i] = gcnew SyntaxHighlighterRegion();
            mLastRegionGet[i]->Set(line, region);
        }
        return mLastRegionGetCount != 0;
    }
    else
    {
        mLastRegionGetCount = 0;
        mLastRegionGetInt = 0;
        return false;
    }
}

bool SyntaxHighlighter::GetNextRegion()
{
    if (mLastRegionGetInt < mLastRegionGetCount)
        mLastRegionGetInt++;
    return mLastRegionGetInt < mLastRegionGetCount;
}

SyntaxHighlighterPair ^SyntaxHighlighter::MatchPair(int line, int column)
{
    if (mEditor != 0)
    {
        ::PairMatch *match = mEditor->searchGlobalPair(line, column);

        if (match == 0)
            return nullptr;

        if (match->sline == -1 || match->eline == -1)
        {
            mEditor->releasePairMatch(match);
            return nullptr;
        }
        SyntaxHighlighterPair ^p = gcnew SyntaxHighlighterPair(mEditor, match);
        mEditor->releasePairMatch(match);
        return p;
    }
    return nullptr;
}

SyntaxHighlighterRegion ^SyntaxHighlighter::CurrentRegion::get()
{
    if (mLastRegionGetInt >= 0 && mLastRegionGetInt < mLastRegionGetCount)
        return mLastRegionGet[mLastRegionGetInt];
    else
        return nullptr;
}

void SyntaxHighlighter::IdleJob(int timeout)
{
    if (mEditor != 0)
    {
        mEditor->idleJob(timeout);
    }
}

void SyntaxHighlighter::ValidateRange(int from, int to)
{
    if (mEditor != 0)
    {
        for (int i = from; i <= to; i++)
            mEditor->validate(i, false);
    }
}

void SyntaxHighlighter::SetVisibleRange(int from, int length)
{
    if (mEditor != 0)
    {
        mEditor->visibleTextEvent(from, length);
        ValidateRange(from, from + length - 1);
    }
}

int SyntaxHighlighter::LastValidLine::get()
{
    if (mEditor != 0)
        return mEditor->lastValidLine();
    else
        return -1;
}

}
}
}
