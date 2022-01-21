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
	if (in_fLogToFile) {
		// Open the file object here and get the handle from it
		_tfopen_s(&fp, szFilename, _T("w, ccs=UTF-8"));
		logFifo.Get(fp); fLogToFile = true;
	}
	else {
		// Open the file object handle here and close the stream to it.
		_tfopen_s(&fp, szFilename, _T("w, ccs=UTF-8"));
		logFifo.Get(fp); fclose(fp);
	}
}
void fulcrum_output::fulcrumDebug(LPCTSTR format_string, ...)
{
	// Setup constant values for this method.
	TCHAR bufferOutputArray[10240];							// Char array for output string. (This value may need work)
	va_list str_args; va_start(str_args, format_string);	// Args formating for log output. List of args and setup command

	// If logging directly to file write it out here.
	if (fLogToFile) { _vftprintf_s(fp, format_string, str_args); }
	
	// Send this to the circular memory-buffer
	size_t bufferSize = sizeof(bufferOutputArray) / sizeof(bufferOutputArray[0]);
	_vsntprintf_s(
		bufferOutputArray,	// Output Array
		bufferSize,			// Size to add in
		_TRUNCATE,			// Truncate Mode.
		format_string,		// Format input string
		str_args			// Args being formatted.
	);

	// Put this entry into our buffer output.
	logFifo.Put(bufferOutputArray);	va_end(str_args);

	// Send to pipe server only if our pipe instances are currently open and connected
	if (!CFulcrumShim::fulcrumPiper->OutputConnected) return;
	std::wstring charString(bufferOutputArray);
	std::string outputString(charString.begin(), charString.end());
	CFulcrumShim::fulcrumPiper->WriteStringOut(outputString);
}