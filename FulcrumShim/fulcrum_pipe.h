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

#pragma once

// Standard Imports
#include <string>

class CPipeException : public CSimpleException
{
public:
	CPipeException(std::string err) : CSimpleException(true), m_error(err) {}
	virtual ~CPipeException() {}
	std::string GetError() { return m_error; }

protected:
	std::string m_error;
};

class CPipExceptionNULLParameter : public CPipeException {
public:
	CPipExceptionNULLParameter() : CPipeException(std::string("NULL input parameter")) { }
	virtual ~CPipExceptionNULLParameter() {}
};

class fulcrum_pipe {
public:
	// Methods for pipe object setup and shutdown.
	fulcrum_pipe(); ~fulcrum_pipe();

	// Bools for states of pipes.
	bool InputConnected = false;
	bool OutputConnected = false;

	// Connect Pipe Routines
	bool PipesConnected();
	bool ConnectInputPipe();
	bool ConnectOutputPipe();

	// Shut down pipe routines.
	void ShutdownPipes();
	void ShutdownInputPipe();
	void ShutdownOutputPipe();

	// Writing operations
	void Writeint32(int writeNumber);
	void WriteStringOut(std::string msgString);
	void WriteUint32(unsigned int writeNumber);
	void WriteBytesOut(byte byteValues[], int byteLength);
	void WriteUint32(unsigned int* writeNumber, unsigned int uintLen);

	// Reading Operations
	int ReadInt32();
	unsigned int ReadUint32();
	std::string ReadStringIn();
	void ReadBytes(byte inputByteBuffer[], int bufferLength);
	void ReadBytesIn(byte inputByteBuffer[], int* bufferLength);

private:
	// Pipe state values.
	bool _pipesConnected = false;
	HANDLE hFulcrumWriter, hFulcrumReader;
};


