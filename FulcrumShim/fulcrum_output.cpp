#include "stdafx.h"
#include <tchar.h>
#include <varargs.h>

// Implementation of a circular buffer. Two simple interfaces:
//   Put(): Add a string to the log
//   Get(): Write the entire log to a file
// Based on DSP Goodies by Alessandro Gallo (http://ag-works.net/)
class cFifo {
    public:
	cFifo()
	    : m_nSize(sizeof(data) / sizeof(data[0]))
	    , m_nItems(0)
	    , m_iWriteNext(0)
	    , m_iReadNext(0)
	    , m_pBuffer(data)
	{
	}

	void Put(LPCTSTR szMsg);
	void Get(FILE *fp);

    private:
	size_t m_nSize;
	size_t m_nItems;
	size_t m_iWriteNext;
	size_t m_iReadNext;
	LPTSTR m_pBuffer;
	TCHAR data[1024 * 128]; // circular buffer for debug log
};

cFifo logFifo;
FILE *fp;
static bool fInitialized = false;
static bool fLogToFile = false;

void
cFifo::Put(LPCTSTR szMsg)
{
	size_t nSize = _tcslen(szMsg);

	// If the string doesn't fit, start later to get the final maxSize characters
	if (nSize > m_nSize) {
		szMsg = &szMsg[nSize - m_nSize]; // start later, in order to get last m_nSize samples
		nSize = m_nSize; // limit the length to the buffer's length
	}

	// Wrap around buffer end
	if (m_iWriteNext + nSize > m_nSize) {
		bool bReadWasBeforeWrite = m_iReadNext < m_iWriteNext;

		// Fill the end of the buffer
		memcpy((m_pBuffer + m_iWriteNext), szMsg, (m_nSize - m_iWriteNext) * sizeof(szMsg[0]));

		// Restart filling from the beginning of the buffer
		memcpy(m_pBuffer, (szMsg + m_nSize - m_iWriteNext), (nSize - m_nSize + m_iWriteNext) * sizeof(szMsg[0]));

		// Update feed position
		m_iWriteNext = nSize + m_iWriteNext - m_nSize;

		// Check if we have written over read position, and move it
		if (bReadWasBeforeWrite && (m_iReadNext < m_iWriteNext))
			m_iReadNext = m_iWriteNext;
	} else {
		bool bReadWasAfterWrite = m_iReadNext > m_iWriteNext;

		// Copy the entire szMsg to the first free location
		memcpy((m_pBuffer + m_iWriteNext), szMsg, nSize * sizeof(szMsg[0]));

		// Update feed position
		m_iWriteNext = (m_iWriteNext + nSize) % m_nSize;

		// Check if we have written over read position, and move it
		if (bReadWasAfterWrite && (m_iReadNext < m_iWriteNext))
			m_iReadNext = m_iWriteNext;
	}

	// More items in the buffer
	m_nItems = m_nItems + nSize < m_nSize ? m_nItems + nSize : m_nSize;
}

void
cFifo::Get(FILE *fp)
{
	size_t n = m_nItems;

	if ((m_iReadNext + n) <= m_nSize) {
		fwrite(m_pBuffer + m_iReadNext, 1, n * sizeof(TCHAR), fp);
		m_iReadNext = (m_iReadNext + n) % m_nSize;
	} else {
		size_t nPart1 = m_nSize - m_iReadNext;
		size_t nPart2 = n - nPart1;

		fwrite(m_pBuffer + m_iReadNext, sizeof(TCHAR), nPart1, fp); // copy tail
		fwrite(m_pBuffer, sizeof(TCHAR), nPart2, fp); // copy from beginning

		m_iReadNext = nPart2;
	}

	m_nItems -= n;
}

void
fulcrum_writeLogfile(LPCTSTR szFilename, bool in_fLogToFile)
{
	// Write the memory-buffer to a file. Then either close the file, or keep the file open
	// and set a flag that redirects all future log messages directly to the file
	if (in_fLogToFile) {
		_tfopen_s(&fp, szFilename, _T("w, ccs=UTF-8"));
		logFifo.Get(fp);
		fLogToFile = true;
	} else {
		_tfopen_s(&fp, szFilename, _T("w, ccs=UTF-8"));
		logFifo.Get(fp);
		fclose(fp);
	}
}

void
dtDebug(LPCTSTR format, ...)
{
	va_list args;
	va_start(args, format);

	if (fLogToFile) {
		// Send this directly to the file
		_vftprintf_s(fp, format, args);
	} else {
		// Send this to the circular memory-buffer
		TCHAR temp[100];
		_vsntprintf_s(temp, sizeof(temp) / sizeof(temp[0]), _TRUNCATE, format, args);
		logFifo.Put(temp);
	}

	va_end(args);
}
