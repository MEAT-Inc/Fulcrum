#include <tchar.h>

// Append adds a line. Write overwrites the entire file content
void appendToLog(LPCTSTR format, ...);
void writeLogfile(LPCTSTR szFilename, bool fUseFileForever);