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

#include <cregexp/cregexp1.h>
#include <unicode/String.h>
#include <unicode/EString.h>

#include "export.h"

void wcscpy1_s(wchar *dst, int maxsize, const wchar *src) {
    for (int i = 0; i < maxsize; i++) {
        dst[i] = src[i];
        if (src[i] == 0)
            break;
    }

}
#ifndef _MSC_VER
void wcscpy1_s(wchar *dst, int maxsize, const wchar_t *src) {
    for (int i = 0; i < maxsize; i++) {
        dst[i] = (wchar)src[i];
        if (src[i] == 0)
            break;
    }

}
#endif

extern "C" {

COLORER_EXPORT(int) CreateRegex(wchar *regex, void **ptr)
{
    *ptr = 0;
    SString *__regex = new SString(regex);
    CRegExp1 *pregex = new CRegExp1(__regex);
    delete __regex;
    if (!pregex->isOk())
    {
        EError1 error = pregex->getError();
        delete pregex;
        return (int)error;
    }
    *ptr = pregex;
    return 0;

}

COLORER_EXPORT(void) DeleteRegex(void *reptr)
{
    delete (CRegExp1*)reptr;
}

COLORER_EXPORT(bool) RegexParse(void *reptr, wchar *value, int startPos, int endPos, void **matchesptr)
{
    *matchesptr = 0;
    CRegExp1 *re = (CRegExp1 *)reptr;
    SString *__value = new SString(value);
    SMatches1 *matches = new SMatches1;
    memset(matches, 0, sizeof(SMatches1));
    re->setPositionMoves(false);
    bool rc = re->parse(__value, startPos, endPos, matches);
    delete __value;
    if (!rc)
    {
        delete matches;
        return false;
    }
    *matchesptr = matches;
    return true;
}

COLORER_EXPORT(bool) RegexFind(void *reptr, wchar *value, int startPos, int endPos, void **matchesptr)
{
    *matchesptr = 0;
    CRegExp1 *re = (CRegExp1 *)reptr;
    SString *__value = new SString(value);
    SMatches1 *matches = new SMatches1;
    memset(matches, 0, sizeof(SMatches1));
    re->setPositionMoves(true);
    bool rc = re->parse(__value, startPos, endPos, matches);
    delete __value;
    if (!rc)
    {
        delete matches;
        return false;
    }
    *matchesptr = matches;
    return true;
}

COLORER_EXPORT(void) DeleteMatches(void *mcptr)
{
    delete (SMatches1*)mcptr;
}

COLORER_EXPORT(int) MatchesCount(void *mcptr)
{
    SMatches1* mc = (SMatches1*)mcptr;
    return mc->cMatch;
}



COLORER_EXPORT(void) MatchGet(void *mcptr, int match, int *s, int *e)
{
    SMatches1* mc = (SMatches1*)mcptr;
    *s = mc->s[match];
    *e = mc->e[match];
    return ;
}

typedef bool (*MatchCallback)(int s, int e);

COLORER_EXPORT(void) RegexFindAll(void *reptr, wchar *value, int startPos, int endPos, MatchCallback callback)
{
    CRegExp1 *re = (CRegExp1 *)reptr;
    SString *__value = new SString(value);
    SMatches1 *matches = new SMatches1;
    re->setPositionMoves(true);
    while (startPos < endPos)
    {
        memset(matches, 0, sizeof(SMatches1));
        bool rc = re->parse(__value, startPos, endPos, matches);
        if (!rc)
            break;
        if (!callback(matches->s[0], matches->e[0]))
            break;
        startPos = matches->e[0];
    }
    delete matches;
    delete __value;
}

}