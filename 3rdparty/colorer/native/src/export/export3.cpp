#include<stdio.h>
#include<stdlib.h>
#include<sys/stat.h>
#ifdef __unix__
#include<dirent.h>
#endif
#ifdef _WIN32
#include<io.h>
#include<windows.h>
#endif

#include <colorer/parserFactory.h>
#include <colorer/editor/baseEditor.h>
#include <unicode/String.h>
#include <unicode/EString.h>
#include "export.h"

class LineSourceAdapter : public LineSource
{
 private:
    String *mLastString = 0;
 public:

    LineSourceAdapter()
    {
    }

    /** Destructor. */
    ~LineSourceAdapter()
    {
        if (mLastString != 0)
            delete mLastString;
    }

    /** Overridable: Job started.

        @param lno      First line number.
      */
    virtual void startJob(int lno)
    {
    }

    /** Overridable: Job finished.

        @param lno      Last line number.
      */
    virtual void endJob(int lno)
    {
        if (mLastString != 0)
            delete mLastString;
        mLastString = 0;
    }

    /** Overridable: Get contents of specified string

        @param lno      Line number to get

        @return         String object, which should be valid
                        until next call of getLine() or
                        endJob()
      */
    virtual String *getLine(int lno)
    {
        const char *s;
        if (lno == 0)
        {
            s = "<root>";
        }
        else if (lno == 1)
        {
            s = "</root>";
        }
        else
            s = "";
        if (mLastString != 0)
            delete mLastString;
        mLastString = new SString((char *)s);
        return mLastString;
    }
};

LineSource *LineSourceFromIntPtr(void *padapter);

class ParserAggregate
{
 private:
    ParserFactory *mParserFactory;
    StyledHRDMapper *mRegionMapper;
    HRCParser *mHRCParser;

 public:
    ParserAggregate(ParserFactory *parserFactory, StyledHRDMapper *regionMapper, HRCParser *hRCParser)
    {
        mParserFactory = parserFactory;
        mRegionMapper = regionMapper;
        mHRCParser = hRCParser;
    }

    ~ParserAggregate()
    {
        if (mParserFactory != 0)
            delete mParserFactory;
    }

    ParserFactory *GetParserFactory() { return mParserFactory; }
    StyledHRDMapper *GetStyledHRDMapper() { return mRegionMapper; }
    HRCParser *HRCParser() { return mHRCParser; }

};

extern "C" {

COLORER_EXPORT(bool) RegionIsDerivedFrom(void * region, void * parentRegion)
{
    Region *r1 = (Region *)region;
    Region *r2 = (Region *)parentRegion;
    return r1->hasParent(r2);
}

COLORER_EXPORT(void) RegionName(void * region, wchar * buffer, int bufferSize)
{
    Region *r1 = (Region *)region;
    const String *name = r1->getName();
    wcscpy_s(buffer, bufferSize, name->getWChars());

}

COLORER_EXPORT(bool) RegionsAreEqual(void * region1, void * region2)
{
    Region *r1 = (Region *)region1;
    Region *r2 = (Region *)region2;
    return r1->getID() == r2->getID();
}


COLORER_EXPORT(int) StyledRegionConsoleColor(void * region)
{
    StyledRegion *r = (StyledRegion *)region;
    return (r->cfore & 0xf) | ((r->cback << 4) & 0xf0);
}

COLORER_EXPORT(unsigned int) StyledRegionForegroundColor(void * region)
{
    StyledRegion *r = (StyledRegion *)region;
    return r->fore;
}


COLORER_EXPORT(unsigned int) StyledRegionBackgroundColor(void * region)
{
    StyledRegion *r = (StyledRegion *)region;
    return r->back;
}

COLORER_EXPORT(int) StyledRegionStyle(void * region)
{
    StyledRegion *r = (StyledRegion *)region;
    return r->style;
}

COLORER_EXPORT(bool) CreateColorerFactory(wchar *cat, wchar *hrdClass, wchar *hrdName, void **ppfactory, wchar *error, int maxerrorsize)
{
    *ppfactory = 0;
    String *__path = new ::SString(cat);
    String *__class = new ::SString(hrdClass);
    String *__name = new ::SString(hrdName);
    ParserFactory *parserFactory = 0;
    StyledHRDMapper *regionMapper = 0;
    HRCParser *hrcParser = 0;
    try
    {
        parserFactory = new ::ParserFactory(__path);
        hrcParser = parserFactory->getHRCParser();
        regionMapper = parserFactory->createStyledMapper(__class, __name);

        if (regionMapper == 0)
        {
            delete __class;
            delete __name;
            __class = new ::SString("console");
            __name = new ::SString("default");
            regionMapper = parserFactory->createStyledMapper(__class, __name);
        }

        if (regionMapper == 0)
        {
            delete __path;
            delete __class;
            delete __name;
            delete parserFactory;
            wcscpy_s(error, maxerrorsize, L"Invalid class and name for HRD styles");
            return false;
        }
        delete __path;
        delete __class;
        delete __name;

        ParserAggregate *aggregate = new ParserAggregate(parserFactory, regionMapper, hrcParser);
        parserFactory = 0;
        hrcParser = 0;
        regionMapper = 0;
        *ppfactory = aggregate;
        return true;


    }
    catch (Exception &e)
    {
        wcscpy_s(error, maxerrorsize, const_cast<wchar *>(e.getMessage()->getWChars()));
        delete __path;
        delete __class;
        delete __name;
        if (parserFactory != 0)
            delete parserFactory;
        return false;
    }
}

COLORER_EXPORT(void) DeleteColorerFactory(void *pfactory)
{
    delete (ParserAggregate *)pfactory;
}


COLORER_EXPORT(void *) FindSyntaxRegion(void *pfactory, wchar *name)
{
    ParserAggregate *aggregate = (ParserAggregate *)pfactory;
    String *_name = new SString(name);
    void *r = (void*)aggregate->HRCParser()->getRegion(_name);
    delete _name;
    return r;
}

COLORER_EXPORT(void *) FindStyledRegion(void *pfactory, wchar *name)
{
    ParserAggregate *aggregate = (ParserAggregate *)pfactory;
    SString _name(name);
    void *r = (void*)aggregate->GetStyledHRDMapper()->getRegionDefine(_name);
    return r;
}


COLORER_EXPORT(void *) CreateBaseEditor(void *pfactory, void *padapter, int backparse)
{
    ParserAggregate *aggregate = (ParserAggregate *)pfactory;
    LineSource *source = LineSourceFromIntPtr(padapter);
    BaseEditor *editor = new BaseEditor(aggregate->GetParserFactory(), source);
    editor->setRegionCompact(true);
    editor->setRegionMapper(aggregate->GetStyledHRDMapper());
    editor->setBackParse(backparse);
    return (void *)editor;
}

COLORER_EXPORT(void) DeleteBaseEditor(void *peditor)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    delete editor;
}

COLORER_EXPORT(void) BaseEditorNotifyLineCount(void *peditor, int newLineCount)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    editor->lineCountEvent(newLineCount);
}

COLORER_EXPORT(void) BaseEditorVisibleLine(void *peditor, int line, int linesCount)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    editor->visibleTextEvent(line, linesCount);
}

COLORER_EXPORT(void) BaseEditorLineChange(void *peditor, int line)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    editor->modifyLineEvent(line);
}

COLORER_EXPORT(void) BaseEditorMajorChange(void *peditor, int line)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    editor->modifyEvent(line);
}

COLORER_EXPORT(void) BaseEditorValidateRange(void *peditor, int lineFrom, int lineTo)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    for (int i = lineFrom; i <= lineTo; i++)
        editor->validate(i, false);
}

COLORER_EXPORT(void) BaseEditorIdle(void *peditor)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    editor->idleJob(1);
}

COLORER_EXPORT(void) BaseEditorFileNameChange(void *peditor, wchar *newName)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    ::DString *__name = new ::DString(newName);
    editor->chooseFileType(__name);
    delete __name;
}

COLORER_EXPORT(bool) BaseEditorGetFileType(void *peditor, wchar *buffer, int buffSize)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    FileType *t = editor->getFileType();
    if (t == 0)
        return false;
    const String *n = t->getName();
    if (t == 0)
        return false;
    wcscpy_s(buffer, buffSize, const_cast<wchar *>(n->getWChars()));
    return true;
}

COLORER_EXPORT(int) BaseEditorLastValidLine(void *peditor)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    return editor->lastValidLine();
}

COLORER_EXPORT(void *)BaseEditorFirstLineRegion(void *peditor, int line)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    return (void *)editor->getLineRegions(line);
}


COLORER_EXPORT(void *)BaseEditorPairMatch(void *peditor, int line, int column)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    PairMatch *match = editor->searchGlobalPair(line, column);
    return match;
}

COLORER_EXPORT(void )BaseEditorReleaseMatch(void *peditor, void *pmatch)
{
    BaseEditor *editor = (BaseEditor *)peditor;
    PairMatch *match = (PairMatch *)pmatch;
    editor->releasePairMatch(match);
}



COLORER_EXPORT(void *)LineRegionNext(void *pregion)
{
    LineRegion *region = (LineRegion *)pregion;
    if (region == 0)
        return 0;
        return region->next;
}

COLORER_EXPORT(void *)LineRegionPrev(void *pregion)
{
    LineRegion *region = (LineRegion *)pregion;
    if (region == 0)
        return 0;
    return region->prev;
}

COLORER_EXPORT(int)LineRegionStart(void *pregion)
{
    LineRegion *region = (LineRegion *)pregion;
    return region->start;
}


COLORER_EXPORT(int)LineRegionEnd(void *pregion)
{
    LineRegion *region = (LineRegion *)pregion;
    return region->end;
}


COLORER_EXPORT(void *)LineRegionSyntaxRegion(void *pregion)
{
    LineRegion *region = (LineRegion *)pregion;
    return (void *)region->region;
}

COLORER_EXPORT(void *)LineRegionStyledRegion(void *pregion)
{
    LineRegion *region = (LineRegion *)pregion;
    return (void *)region->styled();
}

COLORER_EXPORT(int)PairMatchStartLine(void *pmatch)
{
    PairMatch *match = (PairMatch *)pmatch;
    return match->sline;
}

COLORER_EXPORT(int)PairMatchEndLine(void *pmatch)
{
    PairMatch *match = (PairMatch *)pmatch;
    return match->eline;
}


COLORER_EXPORT(void *)PairMatchStartRegion(void *pmatch)
{
    PairMatch *match = (PairMatch *)pmatch;
    return match->start;
}

COLORER_EXPORT(void *)PairMatchEndRegion(void *pmatch)
{
    PairMatch *match = (PairMatch *)pmatch;
    return match->end;
}

}