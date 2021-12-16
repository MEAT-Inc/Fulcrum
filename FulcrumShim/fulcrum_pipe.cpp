#include "stdafx.h"
#include <stdio.h>
#include <windows.h>

#include "fulcrum_pipe.h"
#include "fulcrum_output.h"
#include "fulcrum_debug.h"
#include <tchar.h>

// CTOR and DCTOR for pipe objects
fulcrum_pipe::fulcrum_pipe() { }
fulcrum_pipe::~fulcrum_pipe() {	ShutdownPipe();}
bool fulcrum_pipe::IsLoaded() { return Loaded; }

// Connection methods and closing methods for our pipe objects
bool fulcrum_pipe::ConnectOutputPipe()
{
	// Check if this pipe is loaded or not
	if (Loaded)
	{
		// Log information, store state of pipes, and return it.
		fulcrum_output::fulcrumDebug(_T("%.3fs :: FULCRUM PIPE 1 (OUTPUT PIPE) WAS ALREADY OPEN!\n"), GetTimeSinceInit());
		Loaded = true;
		return true;
	}
	
	// Configure new pipe name object output
	LPTSTR lpszPipename1 = TEXT("\\\\.\\pipe\\2CC3F0FB08354929BB453151BBAA5A15");
	hPipe1 = CreateNamedPipe(lpszPipename1, PIPE_ACCESS_OUTBOUND, PIPE_TYPE_BYTE, 1, 0, 0, 0, NULL);

	// Check if the pipe was built or not.
	if ((hPipe1 == NULL || hPipe1 == INVALID_HANDLE_VALUE))
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    ERROR: FULCRUM PIPE 1 (OUTPUT PIPE) COULD NOT BE OPENED!\n"), GetTimeSinceInit());
		if (hPipe1 == NULL) { fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ PIPE WAS NULL! (error % d)\n"), GetTimeSinceInit(), GetLastError()); }
		else {fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ PIPE HANDLE WAS INVALID! (error %d)\n"), GetTimeSinceInit(), GetLastError()); }
		return false;
	}

	// Log information and return output
	fulcrum_output::fulcrumDebug(_T("%.3fs    FULCRUM PIPE 1 (INPUT PIPE) HAS BEEN OPENED OK!\n"), GetTimeSinceInit());
	return true;
}
bool fulcrum_pipe::ConnectInputPipe()
{
	// Configure new pipe name object output
	LPTSTR lpszPipename2 = TEXT("\\\\.\\pipe\\1D16333944F74A928A932417074DD2B3");
	hPipe2 = CreateFile(lpszPipename2, GENERIC_READ, 0, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);

	// Check if the pipe was built or not.
	if ((hPipe2 == NULL || hPipe2 == INVALID_HANDLE_VALUE))
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    ERROR: FULCRUM PIPE 2 (INPUT PIPE) COULD NOT BE OPENED!\n"), GetTimeSinceInit());
		if (hPipe2 == NULL) { fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ PIPE WAS NULL! (error % d)\n"), GetTimeSinceInit(), GetLastError()); }
		else { fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ PIPE HANDLE WAS INVALID! (error %d)\n"), GetTimeSinceInit(), GetLastError()); }
		return false;
	}

	// Log information and return output
	fulcrum_output::fulcrumDebug(_T("%.3fs    FULCRUM PIPE 2 (INPUT PIPE) HAS BEEN OPENED OK!\n"), GetTimeSinceInit());
	return true;
}
void fulcrum_pipe::ShutdownPipe()
{
	// If not pipe one, don't close down
	if (!hPipe1) { return; }

	// Close it out and log information.
	CloseHandle(hPipe1); hPipe1 = nullptr;
	fulcrum_output::fulcrumDebug(_T("%.3fs    Closed output pipe for FulcrumShim Server correctly!\n", GetTimeSinceInit()));
	Loaded = false;
}

// Writes data to our pipe streams
void fulcrum_pipe::WriteStringOut(std::string str)
{
	DWORD written;
	DWORD bytesToWrite = (DWORD)strlen(str.c_str());
	BOOL res = WriteFile(hPipe1, str.c_str(), bytesToWrite, &written, NULL);
}
void fulcrum_pipe::WriteStringOut100(std::string str)
{
	byte ayPaddedArray[100];
	memset(ayPaddedArray, 0, 100);

	DWORD written;
	DWORD bytesToWrite = (DWORD)strlen(str.c_str());

	for (int i = 0; i < (int)bytesToWrite; i++) ayPaddedArray[i] = str[i];
	BOOL res = WriteFile(hPipe1, ayPaddedArray, 100, &written, NULL);
}
void fulcrum_pipe::WriteBytesOut(byte b[], int b_len)
{
	DWORD written;
	BOOL res = WriteFile(hPipe1, b, b_len, &written, NULL);
}
void fulcrum_pipe::WriteUint32(unsigned int num) { WriteBytesOut((byte*)&num, 4); }
void fulcrum_pipe::WriteUint32(unsigned int* a, unsigned int len) { for (unsigned int i = 0; i < len; i++)	WriteUint32(a[i]); }
void fulcrum_pipe::Writeint32(int num) { WriteBytesOut((byte*)&num, 4); }

// Reads data from our pipe streams
std::string fulcrum_pipe::ReadStringIn()
{
	char read_buffer[100] = { 0 };
	DWORD bytes_read;
	DWORD bytes_to_read = 100;
	BOOL res = ReadFile(hPipe2, read_buffer, bytes_to_read, &bytes_read, NULL);

	std::string str(read_buffer, bytes_read);
	return str;
}
void fulcrum_pipe::ReadBytesIn(byte b[], int* b_len)
{
	DWORD bytes_read = 0;
	DWORD bytes_to_read = *b_len;

	// (01/23/18 TAB) - don't try to read 0 bytes, it might block forever if nothing is added at the other end
	if (bytes_to_read > 0) BOOL res = ReadFile(hPipe2, b, bytes_to_read, &bytes_read, NULL);
	*b_len = bytes_read;
}
void fulcrum_pipe::ReadBytes(byte b[], int num)
{
	int bytesRead = num;
	ReadBytesIn(b, &bytesRead);
	if (bytesRead < num)
		throw& CPipeException(std::string(("not enough bytes")));
}
unsigned int fulcrum_pipe::ReadUint32()
{
	unsigned int num;
	ReadBytes((byte*)&num, 4);
	return num;
}
int fulcrum_pipe::ReadInt32()
{
	int num;
	ReadBytes((byte*)&num, 4);
	return num;
}