cmake_minimum_required(VERSION 2.8)
project(colorertake5)

set(CMAKE_CXX_STANDARD 11)

include(../../cmake/ucm.cmake)

ucm_set_runtime(STATIC)

add_definitions(-DCOLORER_FEATURE_USE_DL_MALLOC=FALSE -DCOLORER_FEATURE_USE_CHUNK_ALLOC=FALSE -DUSE_DL_PREFIX -DCOLORER_FEATURE_HTTPINPUTSOURCE=FALSE)
include_directories(src/shared)
include_directories(src/zlib)

string(TIMESTAMP BUILDTIME "%Y%m%d%H%M")
add_definitions(-DBUILDTIME="${BUILDTIME}")

if(CMAKE_CXX_COMPILER_ID MATCHES "GNU|Clang")
    ucm_add_flags(CXX -fvisibility=default -fpermissive)
endif()

if(CMAKE_CXX_COMPILER_ID MATCHES "MSVC")
    ucm_add_flags(CXX /W3)
endif()


set(SOURCES src/shared/unicode/BitArray.cpp
        src/shared/unicode/Character.cpp
        src/shared/unicode/CharacterClass.cpp
        src/shared/unicode/DString.cpp
        src/shared/unicode/Encodings.cpp
        src/shared/unicode/EString.cpp
        src/shared/unicode/SString.cpp
        src/shared/unicode/String.cpp
        src/shared/unicode/StringBuffer.cpp
        src/shared/unicode/UnicodeTools.cpp
        src/shared/xml/xmldom.cpp
        src/shared/cregexp/cregexp.cpp
        src/shared/cregexp/cregexp1.cpp
        src/shared/colorer/handlers/ErrorHandlerWriter.cpp
        src/shared/common/Exception.cpp
        src/shared/common/io/FileInputSource.cpp
        src/shared/common/io/FileWriter.cpp
        src/shared/common/io/InputSource.cpp
        src/shared/common/io/JARInputSource.cpp
        src/shared/common/Logging.cpp
        src/shared/misc/malloc.c
        src/shared/common/MemoryChunks.cpp
        src/shared/common/io/SharedInputSource.cpp
        src/shared/common/io/StreamWriter.cpp
        src/shared/common/io/Writer.cpp
        src/zlib/adler32.c
        src/zlib/crc32.c
        src/zlib/inffast.c
        src/zlib/inflate.c
        src/zlib/inftrees.c
        src/zlib/uncompr.c
        src/zlib/zutil.c
        src/shared/colorer/ParserFactory.cpp
        src/shared/colorer/viewer/TextLinesStore.cpp
        src/shared/colorer/handlers/FileErrorHandler.cpp
        src/shared/colorer/handlers/LineRegionsCompactSupport.cpp
        src/shared/colorer/handlers/LineRegionsSupport.cpp
        src/shared/colorer/handlers/RegionMapperImpl.cpp
        src/shared/colorer/handlers/StyledHRDMapper.cpp
        src/shared/colorer/handlers/TextHRDMapper.cpp
        src/shared/colorer/editor/BaseEditor.cpp
        src/shared/colorer/editor/Outliner.cpp
        src/shared/colorer/parsers/helpers/HRCParserHelpers.cpp
        src/shared/colorer/parsers/HRCParserImpl.cpp
        src/shared/colorer/parsers/helpers/TextParserHelpers.cpp
        src/shared/colorer/parsers/TextParserImpl.cpp
        src/shared/colorer/_template.h
        src/shared/colorer/editor/BaseEditor.h
        src/shared/unicode/BitArray.h
        src/shared/unicode/Character.h
        src/shared/unicode/CharacterClass.h
        src/shared/common/Common.h
        src/shared/colorer/viewer/ConsoleTools.h
        src/shared/cregexp/cregexp.h
        src/shared/cregexp/cregexp1.h
        src/shared/colorer/handlers/DefaultErrorHandler.h
        src/shared/unicode/DString.h
        src/shared/unicode/Encodings.h
        src/shared/colorer/ErrorHandler.h
        src/shared/colorer/handlers/ErrorHandlerWriter.h
        src/shared/unicode/EString.h
        src/shared/common/Exception.h
        src/shared/common/Features.h
        src/shared/colorer/handlers/FileErrorHandler.h
        src/shared/common/io/FileInputSource.h
        src/shared/colorer/FileType.h
        src/shared/colorer/parsers/helpers/FileTypeImpl.h
        src/shared/common/io/FileWriter.h
        src/shared/common/Hashtable.h
        src/shared/common/HashtableCore.h
        src/shared/colorer/HRCParser.h
        src/shared/colorer/parsers/helpers/HRCParserHelpers.h
        src/shared/colorer/parsers/HRCParserImpl.h
        src/zlib/inffast.h
        src/zlib/inffixed.h
        src/zlib/inftrees.h
        src/shared/common/io/InputSource.h
        src/shared/common/io/JARInputSource.h
        src/shared/colorer/handlers/LineRegion.h
        src/shared/colorer/handlers/LineRegionsCompactSupport.h
        src/shared/colorer/handlers/LineRegionsSupport.h
        src/shared/colorer/LineSource.h
        src/shared/common/Logging.h
        src/shared/common/MemoryChunks.h
        src/shared/common/io/MemoryFile.h
        src/shared/common/MemoryOperator.h
        src/shared/colorer/editor/OutlineItem.h
        src/shared/colorer/editor/Outliner.h
        src/shared/colorer/editor/PairMatch.h
        src/shared/colorer/viewer/ParsedLineWriter.h
        src/shared/colorer/ParserFactory.h
        src/shared/colorer/ParserFactoryException.h
        src/shared/colorer/Region.h
        src/shared/colorer/handlers/RegionDefine.h
        src/shared/colorer/RegionHandler.h
        src/shared/colorer/handlers/RegionMapper.h
        src/shared/colorer/handlers/RegionMapperImpl.h
        src/shared/colorer/Scheme.h
        src/shared/common/io/SharedInputSource.h
        src/shared/unicode/SString.h
        src/shared/common/io/StreamWriter.h
        src/shared/unicode/String.h
        src/shared/unicode/StringBuffer.h
        src/shared/colorer/handlers/StyledHRDMapper.h
        src/shared/colorer/handlers/StyledRegion.h
        src/shared/colorer/viewer/TextConsoleViewer.h
        src/shared/colorer/handlers/TextHRDMapper.h
        src/shared/colorer/TextParser.h
        src/shared/colorer/parsers/helpers/TextParserHelpers.h
        src/shared/colorer/parsers/TextParserImpl.h
        src/shared/colorer/handlers/TextRegion.h
        src/shared/unicode/UnicodeTools.h
        src/shared/common/Vector.h
        src/shared/common/io/Writer.h
        src/shared/unicode/x_charcategory.h
        src/shared/unicode/x_charcategory2.h
        src/shared/unicode/x_charcategory_names.h
        src/shared/unicode/x_defines.h
        src/shared/unicode/x_encodings.h
        src/shared/unicode/x_tables.h
        src/shared/xml/xmldom.h
        src/zlib/zconf.h
        src/zlib/zlib.h
        src/zlib/zutil.h
        src/zlib/contrib/minizip/unzip.h
        src/zlib/contrib/minizip/unzip.c
        src/zlib/contrib/minizip/ioapi.h
        src/zlib/contrib/minizip/ioapi.c
        src/export/export.h
        src/export/export.cpp
        src/export/export1.cpp
        src/export/export2.cpp
        src/export/export3.cpp
        src/export/export4.cpp
   )

add_library(colorertake5 SHARED ${SOURCES})
set_target_properties(colorertake5 PROPERTIES PREFIX "")
set_property(TARGET colorertake5 PROPERTY POSITION_INDEPENDENT_CODE ON)
