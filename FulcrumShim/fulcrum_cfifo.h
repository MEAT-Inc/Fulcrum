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

#include "stdafx.h"
#include <tchar.h>
#include <varargs.h>
#include <memory>
#include <stdexcept>

// For the pipes
#include "FulcrumShim.h"
#include "fulcrum_output.h"

// Implementation of a circular buffer. Two simple interfaces:
//   Put(): Add a string to the log
//   Get(): Write the entire log to a file
// Based on DSP Goodies by Alessandro Gallo (http://ag-works.net/)
class fulcrum_cfifo {
public: 
	fulcrum_cfifo()
	: m_nSize(sizeof(data) / sizeof(data[0]))
	, m_nItems(0)
	, m_iWriteNext(0)
	, m_iReadNext(0)
	, m_pBuffer(data) { }
	  void Put(LPCTSTR szMsg);
	  void Get(FILE* fp);

private:
	size_t m_nSize;
	size_t m_nItems;
	size_t m_iWriteNext;
	size_t m_iReadNext;
	LPTSTR m_pBuffer;
	TCHAR data[1024 * 128]; // circular buffer for debug log
};

// Public FIFO members. Used to trigger when to write to file or not.
FILE* fp;
fulcrum_cfifo logFifo;
static bool fLogToFile = false;
static bool fInitalized = false;