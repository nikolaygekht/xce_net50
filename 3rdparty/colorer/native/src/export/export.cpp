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

#include<common/Logging.h>

#include<colorer/ParserFactory.h>
#include<colorer/viewer/TextLinesStore.h>
#include<colorer/handlers/DefaultErrorHandler.h>
#include<colorer/handlers/FileErrorHandler.h>
#include<colorer/parsers/HRCParserImpl.h>
#include<colorer/parsers/TextParserImpl.h>

#include "export.h"


