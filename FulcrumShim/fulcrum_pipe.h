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

public:
	bool ConnectInputPipe();
	bool ConnectOutputPipe();
	bool IsLoaded();
	void ShutdownPipe();
	void WriteStringOut(std::string str);
	void WriteStringOut100(std::string str);
	void WriteBytesOut(byte b[], int b_len);
	std::string ReadStringIn();
	void ReadBytesIn(byte b[], int* b_len);
	void ReadBytes(byte b[], int num);				// with length check

	unsigned int ReadUint32();
	int ReadInt32();
	void WriteUint32(unsigned int num);
	void WriteUint32(unsigned int* a, unsigned int len);
	void Writeint32(int num);

private:
	bool Loaded = false;
	HANDLE hPipe1, hPipe2;
};

