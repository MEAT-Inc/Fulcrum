#include <tchar.h>

class fulcrum_output {
public:
	// Writes for our output target types
	static void fulcrumDebug(LPCTSTR format, ...);
	static void writeNewLogFile(LPCTSTR szFilename, bool fUseFileForever);
};