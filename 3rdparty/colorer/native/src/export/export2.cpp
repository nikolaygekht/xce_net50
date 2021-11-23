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

typedef void (*GetLineContent)(int lno);

class ColorerLineSourceAdapter : public LineSource
{
  private:
     GetLineContent mGetLineContent;
     String *mLastString = 0;
     String *mEmptyString = new SString("");
  public:

     ColorerLineSourceAdapter(GetLineContent getLineContent)
     {
         mGetLineContent = getLineContent;
     }

     /** Destructor. */
     ~ColorerLineSourceAdapter()
     {
         if (mLastString != 0)
             delete mLastString;
         delete mEmptyString;
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

         if (mLastString != 0)
             delete mLastString;
         mLastString = 0;

         mGetLineContent(lno);

         if (mLastString == 0)
             return mEmptyString;
         else
             return mLastString;

     }

     void AcceptLine(wchar *content, int length)
     {
         if (mLastString != 0)
             delete mLastString;
         mLastString = 0;

         if (content != 0 && length > 0)
             mLastString = new SString(content, 0, length);
     }
};


LineSource *LineSourceFromIntPtr(void *padapter)
{
    return (LineSource *)(ColorerLineSourceAdapter *)padapter;
}

extern "C" {


COLORER_EXPORT(void) CreateColorerLineSourceAdapter(GetLineContent getLineContent, void **ppadapter)
{
    *ppadapter = new ColorerLineSourceAdapter(getLineContent);
    return ;
}

COLORER_EXPORT(void) DeleteColorerLineSourceAdapter(void *padapter)
{
    delete (ColorerLineSourceAdapter *)padapter;
}
COLORER_EXPORT(void) ColorerLineSourceAdapterAcceptLine(void *padapter, wchar *line, int length)
{
     ColorerLineSourceAdapter * adapter = (ColorerLineSourceAdapter *)padapter;
     adapter->AcceptLine(line, length);
}
}