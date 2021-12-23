#include "stdafx.h"
#include <stdio.h>
#include <windows.h>

#include "fulcrum_pipe.h"
#include "fulcrum_output.h"
#include "fulcrum_debug.h"
#include <tchar.h>

// CTOR and DCTOR for pipe objects
fulcrum_pipe::fulcrum_pipe() { }
fulcrum_pipe::~fulcrum_pipe() {	ShutdownPipes();}
bool fulcrum_pipe::IsLoaded() { return PipesConnected; }

// Connection methods and closing methods for our pipe objects
bool fulcrum_pipe::ConnectOutputPipe()
{
	// Check if this pipe is loaded or not
	if (PipesConnected || OutputConnected)
	{
		// Log information, store state of pipes, and return it.
		fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 1 (Output Pipe) was already open!\n"), GetTimeSinceInit());
		PipesConnected = InputConnected;

		// Check if loaded now
		if (PipesConnected) fulcrum_output::fulcrumDebug(_T("%.3fs    Both Fulcrum Pipes are already open!\n"), GetTimeSinceInit());
		return true;
	}
	
	// Configure new pipe name object output
	LPTSTR lpszPipename1 = TEXT("\\\\.\\pipe\\2CC3F0FB08354929BB453151BBAA5A15");
	hFulcrumWriter = CreateNamedPipe(
		lpszPipename1,						// Name of the pipe
		PIPE_ACCESS_OUTBOUND,				// Pipe direction
		PIPE_TYPE_BYTE | PIPE_WAIT,		    // Pipe types
		1,								    // Number of instances
		1024 * 16,							// Output buffer size
		1024 * 16,							// Input buffer size
		NMPWAIT_USE_DEFAULT_WAIT,			// Timeout Time value
		NULL								// Default security wait
	);

	// Check if the pipe was built or not.
	if ((hFulcrumWriter == NULL || hFulcrumWriter == INVALID_HANDLE_VALUE))
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    ERROR: Fulcrum Pipe 1 (Output Pipe) could not be opened!\n"), GetTimeSinceInit());
		if (hFulcrumWriter == NULL) { fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ Pipe was NULL! (error % d)\n"), GetTimeSinceInit(), GetLastError()); }
		else {fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ Pipe handle was invalid! (error %d)\n"), GetTimeSinceInit(), GetLastError()); }
		return false;
	}

	// Log information and return output
	fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 1 (Output Pipe) has been opened OK!\n"), GetTimeSinceInit());
	return true;
}
bool fulcrum_pipe::ConnectInputPipe()
{
	// Configure new pipe name object output
	LPTSTR lpszPipename2 = TEXT("\\\\.\\pipe\\1D16333944F74A928A932417074DD2B3");
	hFulcrumReader = CreateFile(lpszPipename2, GENERIC_READ, 0, NULL, OPEN_EXISTING, 0, NULL);

	// Check if this pipe is loaded or not
	if (PipesConnected || InputConnected)
	{
		// Log information, store state of pipes, and return it.
		fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 2 (Input Pipe) was already open!\n"), GetTimeSinceInit());
		PipesConnected = OutputConnected;

		// Check if loaded now
		if (PipesConnected) fulcrum_output::fulcrumDebug(_T("%.3fs    Both Fulcrum Pipes are already open!\n"), GetTimeSinceInit());
		return true;
	}

	// Check if the pipe was built or not.
	if ((hFulcrumReader == NULL || hFulcrumReader == INVALID_HANDLE_VALUE))
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    ERROR: Fulcrum Pipe 2 (Input Pipe) could not be opened!\n"), GetTimeSinceInit());
		if (hFulcrumReader == NULL) { fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ Pipe was NULL! (error % d)\n"), GetTimeSinceInit(), GetLastError()); }
		else { fulcrum_output::fulcrumDebug(_T("%.3fs    \\__ Pipe handle was invalid! (error %d)\n"), GetTimeSinceInit(), GetLastError()); }
		return false;
	}

	// Log information and return output then close our handle output
	fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 2 (Input Pipe) has been opened OK!\n"), GetTimeSinceInit());
	return true;
}


void fulcrum_pipe::ShutdownPipes()
{
	// Close out both pipes here
	fulcrum_pipe::ShutdownInputPipe();
	fulcrum_pipe::ShutdownOutputPipe();
	fulcrum_output::fulcrumDebug(_T("%.3fs    Closed output pipe for FulcrumShim Server correctly!\n", GetTimeSinceInit()));
}
void fulcrum_pipe::ShutdownOutputPipe()
{
	// Check if already closed or not
	if (!hFulcrumWriter) {
		fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 1 (Output Pipe) was already closed!\n", GetTimeSinceInit()));
		InputConnected = false;	PipesConnected = false;
		return;
	}

	// Close it out now
	CloseHandle(hFulcrumWriter); hFulcrumWriter = nullptr;
	fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 1 (Output Pipe) has been closed! Pipe handle is now NULL!\n"), GetTimeSinceInit());
	InputConnected = false; PipesConnected = false;
}
void fulcrum_pipe::ShutdownInputPipe()
{
	// Check if already closed or not
	if (!hFulcrumReader) {
		fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 2 (Input Pipe) was already closed!\n", GetTimeSinceInit()));
		OutputConnected = false; PipesConnected = false;
		return;
	}

	// Close it out now
	CloseHandle(hFulcrumReader); hFulcrumReader = nullptr;
	fulcrum_output::fulcrumDebug(_T("%.3fs    Fulcrum Pipe 2 (Input Pipe) has been closed! Pipe handle is now NULL!\n"), GetTimeSinceInit());
	OutputConnected = false; PipesConnected = false;
}


// Writes data to our pipe streams
void fulcrum_pipe::WriteStringOut(std::string str)
{
	DWORD written;
	DWORD bytesToWrite = (DWORD)strlen(str.c_str());
	BOOL res = WriteFile(hFulcrumWriter, str.c_str(), bytesToWrite, &written, NULL);
	FlushFileBuffers(hFulcrumWriter);

	// Removing this for testing purposes
	// CloseHandle(hFulcrumWriter);
}
void fulcrum_pipe::WriteStringOut100(std::string str)
{
	byte ayPaddedArray[100];
	memset(ayPaddedArray, 0, 100);

	DWORD written;
	DWORD bytesToWrite = (DWORD)strlen(str.c_str());

	for (int i = 0; i < (int)bytesToWrite; i++) ayPaddedArray[i] = str[i];
	BOOL res = WriteFile(hFulcrumWriter, ayPaddedArray, 100, &written, NULL);

	// Removing this for testing purposes
	// CloseHandle(hFulcrumWriter);
}
void fulcrum_pipe::WriteBytesOut(byte b[], int b_len)
{
	DWORD written;
	BOOL res = WriteFile(hFulcrumWriter, b, b_len, &written, NULL);

	// Removing this for testing purposes
	// CloseHandle(hFulcrumWriter);
}
void fulcrum_pipe::WriteUint32(unsigned int num) {
	WriteBytesOut((byte*)&num, 4);

	// Removing this for testing purposes
	// CloseHandle(hFulcrumWriter);
}
void fulcrum_pipe::WriteUint32(unsigned int* a, unsigned int len) {
	for (unsigned int i = 0; i < len; i++) WriteUint32(a[i]);

	// Removing this for testing purposes
	// CloseHandle(hFulcrumWriter);
}
void fulcrum_pipe::Writeint32(int num) {
	WriteBytesOut((byte*)&num, 4); 

	// Removing this for testing purposes
	// CloseHandle(hFulcrumWriter);
}

// Reads data from our pipe streams
std::string fulcrum_pipe::ReadStringIn()
{
	char read_buffer[100] = { 0 };
	DWORD bytes_read;
	DWORD bytes_to_read = 100;
	BOOL res = ReadFile(hFulcrumReader, read_buffer, bytes_to_read, &bytes_read, NULL);

	std::string str(read_buffer, bytes_read);
	return str;
}
void fulcrum_pipe::ReadBytesIn(byte b[], int* b_len)
{
	DWORD bytes_read = 0;
	DWORD bytes_to_read = *b_len;

	// (01/23/18 TAB) - don't try to read 0 bytes, it might block forever if nothing is added at the other end
	if (bytes_to_read > 0) BOOL res = ReadFile(hFulcrumReader, b, bytes_to_read, &bytes_read, NULL);
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