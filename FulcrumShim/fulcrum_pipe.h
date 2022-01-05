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
	fulcrum_pipe();
	~fulcrum_pipe();
	bool PipesConnected();
	bool ConnectInputPipe();
	bool ConnectOutputPipe();
	void ShutdownPipes();
	void ShutdownInputPipe();
	void ShutdownOutputPipe();

	// Writing operations
	void WriteStringOut(std::string str);
	void WriteBytesOut(byte b[], int b_len);

	// Reading Operations
	std::string ReadStringIn();
	void ReadBytesIn(byte b[], int* b_len);
	void ReadBytes(byte b[], int num);			
	unsigned int ReadUint32();
	int ReadInt32();
	void WriteUint32(unsigned int num);
	void WriteUint32(unsigned int* a, unsigned int len);
	void Writeint32(int num);

	// Bools for states
	bool InputConnected = false;
	bool OutputConnected = false;

private:
	bool _pipesConnected = false;
	HANDLE hFulcrumWriter, hFulcrumReader;
};


