#include "stdafx.h"

#include <stdio.h>
#include <windows.h>

#include "fulcrum_pipe.h"
#include "fulcrum_output.h"
#include "fulcrum_debug.h"
#include <tchar.h>

fulcrum_pipe::fulcrum_pipe()
{
	Loaded = false;
	Startup();
}
fulcrum_pipe::~fulcrum_pipe()
{
	ShutdownPipe();
}


bool fulcrum_pipe::ConnectPipe1()
{
	LPTSTR lpszPipename1 = TEXT("\\\\.\\pipe\\2CC3F0FB08354929BB453151BBAA5A15");
	hPipe1 = CreateFile(lpszPipename1, GENERIC_WRITE, 0, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);

	if ((hPipe1 == NULL || hPipe1 == INVALID_HANDLE_VALUE))
	{
		dtDebug(_T("%.3fs :: FULCRUM PIPE 1 COULD NOT BE OPENED!\n"), GetTimeSinceInit());
		if (hPipe1 == NULL) { dtDebug(_T("%.3fs    \\__ PIPE WAS NULL! (error % d)\n"), GetTimeSinceInit(), GetLastError()); }
		else {dtDebug(_T("%.3fs    \\__ PIPE HANDLE WAS INVALID! (error %d)\n"), GetTimeSinceInit(), GetLastError()); }
		return false;
	}

	dtDebug(_T("%.3fs :: FULCRUM PIPE 1 --> OPENED OK!\n"), GetTimeSinceInit());
	return true;
}
bool fulcrum_pipe::ConnectPipe2()
{
	LPTSTR lpszPipename2 = TEXT("\\\\.\\pipe\\1D16333944F74A928A932417074DD2B3");
	hPipe2 = CreateFile(lpszPipename2, GENERIC_READ, 0, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);

	if ((hPipe2 == NULL || hPipe2 == INVALID_HANDLE_VALUE))
	{
		dtDebug(_T("%.3fs :: FULCRUM PIPE 2 COULD NOT BE OPENED!\n"), GetTimeSinceInit());
		if (hPipe2 == NULL) { dtDebug(_T("%.3fs    \\__ PIPE WAS NULL! (error % d)\n"), GetTimeSinceInit(), GetLastError()); }
		else { dtDebug(_T("%.3fs    \\__ PIPE HANDLE WAS INVALID! (error %d)\n"), GetTimeSinceInit(), GetLastError()); }
		return false;
	}

	dtDebug(_T("%.3fs :: FULCRUM PIPE 2 --> OPENED OK!\n"), GetTimeSinceInit());
	return true;
}


bool fulcrum_pipe::Startup()
{
	// If pipes are open, return
	if (Loaded) return Loaded;

	// Boot pipe 1
	bool res_pipe1 = ConnectPipe1();
	if (!res_pipe1)
	{
		Loaded = false;
		return Loaded;
	}

	// Boot pipe 2 
	bool res_pipe2 = ConnectPipe2();
	if (!res_pipe2)
	{
		Loaded = false;
		return Loaded;
	}

	dtDebug(_T("%.3fs :: FULCRUM PIPES ARE LOOKIN GOOD LETS SEND THIS BITCH\n"), GetTimeSinceInit());
	Loaded = true;
	return Loaded;
}

void fulcrum_pipe::ShutdownPipe()
{
	if (hPipe1)
	{
		CloseHandle(hPipe1);
		hPipe1 = nullptr;
	}

	// (06/09/18 TAB)
	Loaded = false;
}


bool fulcrum_pipe::IsLoaded()
{
	return Loaded;
}

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

	for (int i = 0; i < (int)bytesToWrite; i++)
		ayPaddedArray[i] = str[i];

	BOOL res = WriteFile(hPipe1, ayPaddedArray, 100, &written, NULL);
}

void fulcrum_pipe::WriteBytesOut(byte b[], int b_len)
{
	DWORD written;
	BOOL res = WriteFile(hPipe1, b, b_len, &written, NULL);
}

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
	if (bytes_to_read > 0)
		BOOL res = ReadFile(hPipe2, b, bytes_to_read, &bytes_read, NULL);

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

void fulcrum_pipe::WriteUint32(unsigned int num)
{
	WriteBytesOut((byte*)&num, 4);
}
void fulcrum_pipe::WriteUint32(unsigned int* a, unsigned int len)
{
	for (unsigned int i = 0; i < len; i++)
		WriteUint32(a[i]);
}

void fulcrum_pipe::Writeint32(int num)
{
	WriteBytesOut((byte*)&num, 4);
}
