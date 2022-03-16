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
#include <stdio.h>
#include <windows.h>
#include <tchar.h>

// Fulcrum Resource Imports
#include "fulcrum_pipe.h"
#include "fulcrum_debug.h"
#include "fulcrum_output.h"

// CTOR and DCTOR for pipe objects
fulcrum_pipe::fulcrum_pipe() { }
fulcrum_pipe::~fulcrum_pipe() {	}

// Checks if pip configuration is good or bad
bool fulcrum_pipe::PipesConnected() {
	return OutputConnected == true || InputConnected == true;
}

// Connection methods and closing methods for our pipe objects
bool fulcrum_pipe::ConnectOutputPipe()
{
	// Check if this pipe is loaded or not
	if (_pipesConnected || OutputConnected)
	{
		// Log information, store state of pipes, and return it.
		fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 1 (Output Pipe) was already open!\n"));
		_pipesConnected = InputConnected;

		// Check if loaded now
		if (_pipesConnected) fulcrum_output::fulcrumDebug(_T("-->       Both Fulcrum Pipes are already open!\n"));
		return true;
	}
	
	// Configure new pipe name object output
	LPTSTR OutputPipeLocation = TEXT("\\\\.\\pipe\\2CC3F0FB08354929BB453151BBAA5A15");
	hFulcrumWriter = CreateNamedPipe(
		OutputPipeLocation,					// Name of the pipe
		PIPE_ACCESS_OUTBOUND,				// Pipe direction (In and Out)
		PIPE_TYPE_MESSAGE | PIPE_WAIT,		// Pipe types for sending output
		100,							    // Number of instances (Set to 100 since we need to be aware of open and closes)
		1024 * 16,							// Output buffer size
		1024 * 16,							// Input buffer size
		NMPWAIT_USE_DEFAULT_WAIT,			// Timeout Time value
		NULL								// Default security wait
	);

	// Check if the pipe was built or not.
	if ((hFulcrumWriter == NULL || hFulcrumWriter == INVALID_HANDLE_VALUE))
	{
		fulcrum_output::fulcrumDebug(_T("-->       ERROR: Fulcrum Pipe 1 (Output Pipe) could not be opened!\n"));
		if (hFulcrumWriter == NULL) { fulcrum_output::fulcrumDebug(_T("-->           \\__ Pipe was NULL! (error % d)\n"), GetLastError()); }
		else {fulcrum_output::fulcrumDebug(_T("-->       \\__ Pipe handle was invalid!(error % d)\n"), GetLastError()); }
		return false;
	}

	// Log information and return output
	fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 1 (Output Pipe) has been opened OK!\n"));
	OutputConnected = true;
	return true;
}
bool fulcrum_pipe::ConnectInputPipe()
{
	// Check if this pipe is loaded or not
	if (_pipesConnected || InputConnected)
	{
		// Log information, store state of pipes, and return it.
		fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 2 (Input Pipe) was already open!\n"));
		_pipesConnected = OutputConnected;

		// Check if loaded now
		if (_pipesConnected) fulcrum_output::fulcrumDebug(_T("-->       Both Fulcrum Pipes are already open!\n"));
		return true;
	}

	// Configure new pipe name object output
	LPTSTR InputPipeLocation = TEXT("\\\\.\\pipe\\1D16333944F74A928A932417074DD2B3");
	hFulcrumReader = CreateFile(InputPipeLocation, GENERIC_READ, 0, NULL, CREATE_ALWAYS, 0, NULL);

	// Check if the pipe was built or not.
	if ((hFulcrumReader == NULL || hFulcrumReader == INVALID_HANDLE_VALUE))
	{
		fulcrum_output::fulcrumDebug(_T("-->       ERROR: Fulcrum Pipe 2 (Input Pipe) could not be opened!\n"));
		if (hFulcrumReader == NULL) { fulcrum_output::fulcrumDebug(_T("-->       \\__ Pipe was NULL! (error % d)\n"), GetLastError()); }
		else { fulcrum_output::fulcrumDebug(_T("-->       \\__ Pipe handle was invalid! (error %d)\n"), GetLastError()); }
		return false;
	}

	// Log information and return output then close our handle output
	fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 2 (Input Pipe) has been opened OK!\n"));
	InputConnected = true;
	return true;
}


void fulcrum_pipe::ShutdownPipes()
{
	// Close out both pipes here
	fulcrum_pipe::ShutdownInputPipe();
	fulcrum_pipe::ShutdownOutputPipe();
	fulcrum_output::fulcrumDebug(_T("-->       Closed output pipe for FulcrumShim Server correctly!\n"));
}
void fulcrum_pipe::ShutdownOutputPipe()
{
	// Check if already closed or not
	if (hFulcrumWriter == NULL) {
		fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 1 (Output Pipe) was already closed!\n"));
		OutputConnected = false; _pipesConnected = false;
		return;
	}

	// Close it out now
	CloseHandle(hFulcrumWriter); hFulcrumWriter = nullptr;
	fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 1 (Output Pipe) has been closed! Pipe handle is now NULL!\n"));
	OutputConnected = false; _pipesConnected = false;
}
void fulcrum_pipe::ShutdownInputPipe()
{
	// Check if already closed or not
	if (hFulcrumReader == NULL) {
		fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 2 (Input Pipe) was already closed!\n"));
		InputConnected = false; _pipesConnected = false;
		return;
	}

	// Close it out now
	CloseHandle(hFulcrumReader); hFulcrumReader = nullptr;
	fulcrum_output::fulcrumDebug(_T("-->       Fulcrum Pipe 2 (Input Pipe) has been closed! Pipe handle is now NULL!\n"));
	InputConnected = false; _pipesConnected = false;
}


// Writes data to our pipe streams
void fulcrum_pipe::WriteStringOut(std::string msgString)
{
	DWORD bytesWritten;
	DWORD bytesToWrite = (DWORD)strlen(msgString.c_str());
	BOOL resultValue = WriteFile(hFulcrumWriter, msgString.c_str(), bytesToWrite, &bytesWritten, NULL);
}
void fulcrum_pipe::WriteBytesOut(byte byteValues[], int byteLength)
{
	DWORD bytesWritten;
	BOOL resultValue = WriteFile(hFulcrumWriter, byteValues, byteLength, &bytesWritten, NULL);
}
void fulcrum_pipe::WriteUint32(unsigned int writeNumber) {
	WriteBytesOut((byte*)&writeNumber, 4);
}
void fulcrum_pipe::WriteUint32(unsigned int* writeNumber, unsigned int uintLen) {
	for (unsigned int i = 0; i < uintLen; i++) WriteUint32(writeNumber[i]);
}
void fulcrum_pipe::Writeint32(int writeNumber) {
	WriteBytesOut((byte*)&writeNumber, 4); 
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
void fulcrum_pipe::ReadBytesIn(byte inputByteBuffer[], int* bufferLength)
{
	DWORD bytes_read = 0;
	DWORD bytes_to_read = *bufferLength;

	// (01/23/18 TAB) - don't try to read 0 bytes, it might block forever if nothing is added at the other end
	if (bytes_to_read > 0) BOOL res = ReadFile(hFulcrumReader, inputByteBuffer, bytes_to_read, &bytes_read, NULL);
	*bufferLength = bytes_read;
}
void fulcrum_pipe::ReadBytes(byte inputByteBuffer[], int bufferLength)
{
	int bytesRead = bufferLength;
	ReadBytesIn(inputByteBuffer, &bytesRead);
	if (bytesRead < bufferLength)
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