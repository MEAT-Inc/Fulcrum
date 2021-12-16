#include <tchar.h>

class fulcrum_output {
public:
	// Writes for our output target types
	static void fulcrumDebug(LPCTSTR format, ...);
	static void appendToLog(LPCTSTR format, ...);
	static void appendToPipes(LPCTSTR format, ...);

	// Writes an entire new log file output
	static void writeNewLogFile(LPCTSTR szFilename, bool fUseFileForever);
};