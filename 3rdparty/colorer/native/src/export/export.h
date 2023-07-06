#ifdef _MSC_VER
#define COLORER_EXPORT(A)  __declspec(dllexport) A
#elif defined(__GNUC__)
#define COLORER_EXPORT(A) A __attribute__((visibility("default")))
#else
#pragma warning Unknown dynamic link import/export semantics.
#endif

void wcscpy1_s(wchar *dst, int maxsize, const wchar *src);
#ifndef _MSC_VER
void wcscpy1_s(wchar *dst, int maxsize, const wchar_t *src);
#endif
