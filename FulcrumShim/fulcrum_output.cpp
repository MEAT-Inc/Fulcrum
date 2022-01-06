/*
**
** Copyright (C) 2022 MEAT Inc
** Author: Zack Walsh <neo.smith@motorengineeringandtech.com>
**
** This library is free software; you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License as published
** by the Free Software Foundation, either version 3 of the License, or (at
** your option) any later version.
**
** This library is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
** Lesser General Public License for more details.
**
** You should have received a copy of the GNU Lesser General Public
** License along with this library; if not, <http://www.gnu.org/licenses/>.
**
*/

// Standard Imports
#include "stdafx.h"
#include <tchar.h>
#include <varargs.h>
#include <memory>
#include <stdexcept>

// Fulcrum Resource Imports
#include "FulcrumShim.h"
#include "fulcrum_cfifo.h"
#include "fulcrum_output.h"

// Public FIFO members. Used to trigger when to write to file or not.
FILE* fp;
fulcrum_cfifo logFifo;
static bool fLogToFile = false;
static bool fInitalized = false;

// Logging Methods Appends are for single targets
void fulcrum_output::writeNewLogFile(LPCTSTR szFilename, bool in_fLogToFile)
{
	// Write the memory-buffer to a file. Then either close the file, or keep the file open
	// and set a flag that redirects all future log messages directly to the file
	if (in_fLogToFile)
	{
		// Open the file object here and get the handle from it
		_tfopen_s(&fp, szFilename, _T("w, ccs=UTF-8"));
		logFifo.Get(fp); fLogToFile = true;
	}
	else
	{
		// Open the file object handle here and close the stream to it.
		_tfopen_s(&fp, szFilename, _T("w, ccs=UTF-8"));
		logFifo.Get(fp);
		fclose(fp);
	}
}
void fulcrum_output::fulcrumDebug(LPCTSTR format, ...)
{
	// Args formating for log output
	va_list args; va_start(args, format);

	// If logging directly to file
	if (fLogToFile) { _vftprintf_s(fp, format, args); }
	else
	{
		// Send this to the circular memory-buffer
		TCHAR temp[100];
		_vsntprintf_s(temp, sizeof(temp) / sizeof(temp[0]), _TRUNCATE, format, args);
		logFifo.Put(temp);
	}

	// Convert our input string to a std::string and buffer of char[]
	std::string fmt_str = CT2A(format);
	std::unique_ptr<char[]> formatted;
	int final_n, n = ((int)fmt_str.size()) * 2;

	// Now run thru each char object and find where the arguments are. 
	while (1) {
		// Format new output for this argument object
		formatted.reset(new char[n]);
		strcpy(&fmt_str[0], fmt_str.c_str());
		final_n = _vsnprintf(&formatted[0], n, fmt_str.c_str(), args);

		// If we're at the end of the line or no more args appear, return
		if (final_n < 0 || final_n >= n) { n += abs(final_n - n + 1); }
		else break;
	}

	// Send to pipe server only if our pipe instances are currently open and connected
	 if (CFulcrumShim::fulcrumPiper->OutputConnected)
	 {
	 	// Convert into a string object and write to pipes
	 	std::string built_string = std::string(formatted.get());
	 	CFulcrumShim::fulcrumPiper->WriteStringOut(built_string);
	 }
	
	// Stop arg fprmatting session
	va_end(args);
}