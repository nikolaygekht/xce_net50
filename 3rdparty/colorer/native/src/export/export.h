#ifdef _MSC_VER
#define COLORER_EXPORT(A)  __declspec(dllexport) A
#elif defined(__GNUC__)
#define COLORER_EXPORT(A) A __attribute__((visibility("default")))
#else
#pragma warning Unknown dynamic link import/export semantics.
#endif

#ifdef __GNUC__
void wcscpy_s(wchar *dst, int maxsize, wchar *src);
void wcscpy_s(wchar *dst, int maxsize, wchar_t *src);
#endif