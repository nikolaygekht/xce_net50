#include <stdio.h>
#include<stdio.h>
#include<stdlib.h>
#include<stdarg.h>
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

extern "C" {

void logg(const char *fmt, ...)
{
    va_list va;
    va_start(va, fmt);
    char text[4096];
    vsprintf(text, fmt, va);
    FILE *f = fopen("log.txt", "a");
    fputs(text, f);
    fclose(f);
}

COLORER_EXPORT(void) AllInOneAction()
{
    logg("started...");

    String *spath = new SString("../../data/catalog.xml");
    String *sclass = new SString("console");
    String *sname = new SString("xce");
    String *sfname = new SString("q.xml");

    ParserFactory *parserFactory = 0;
    parserFactory = new ::ParserFactory(spath);
    delete spath;
    StyledHRDMapper *regionMapper = parserFactory->createStyledMapper(sclass, sname);
    logg("%i", regionMapper);
    delete sclass;
    delete sname;

    LineSourceAdapter *adapter = new LineSourceAdapter();
    BaseEditor *editor = new BaseEditor(parserFactory, adapter);
    editor->setRegionCompact(true);
    editor->setRegionMapper(regionMapper);
    editor->setBackParse(5000);

    editor->chooseFileType(sfname);
    delete sfname;
    editor->lineCountEvent(10);
    logg(">>>idleJob");
    editor->idleJob(1);
    logg("<<<idleJob");

    LineRegion *r = editor->getLineRegions(0);
    while (r != 0)
    {
        logg("%i-%i,", r->start, r->end);
        if (r->region == 0)
            logg("null,");
        else
        {
            const char *rname = r->region->getName()->getChars();
            logg("%s,", rname);
        }
        r = r->next;
    }

    r = editor->getLineRegions(1);
    while (r != 0)
    {
        logg("%i-%i,", r->start, r->end);
        r = r->next;
    }

    delete editor;
    delete adapter;
    delete regionMapper;
    delete parserFactory;

    logg("finished");
    return ;
}
}