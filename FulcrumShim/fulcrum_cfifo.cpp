/*
* 
** Author: Zack Walsh <neo.smith@motorengineeringandtech.com>
**
** This library is free software; you can redistribute itand /or modify
* *it under the terms of the GNU Lesser General Public License as published
* *by the Free Software Foundation, either version 3 of the License, or (at
	* *your option) any later version.
	* *
	**This library is distributed in the hope that it will be useful,
	** but WITHOUT ANY WARRANTY; without even the implied warranty of
	** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU
	* *Lesser General Public License for more details.
	**
	** You should have received a copy of the GNU Lesser General Public
	** License along with this library; if not, < http://www.gnu.org/licenses/>.
**
*/

// Standard includes
#include "stdafx.h"
#include <tchar.h>
#include <varargs.h>
#include <memory>
#include <stdexcept>

// For the pipes
#include "fulcrum_cfifo.h"
#include "FulcrumShim.h"
#include "fulcrum_output.h"

/// <summary>
/// Puts a new entry into our log output file
/// </summary>
/// <param name="szMsg"></param>
void fulcrum_cfifo::Put(LPCTSTR szMsg)
{
	// Build size string
	size_t nSize = _tcslen(szMsg);

	// If the string doesn't fit, start later to get the final maxSize characters
	if (nSize > m_nSize) {
		szMsg = &szMsg[nSize - m_nSize]; // start later, in order to get last m_nSize samples
		nSize = m_nSize; // limit the length to the buffer's length
	}

	// Wrap around buffer end
	if (m_iWriteNext + nSize > m_nSize) {

		// Fill the end of the buffer then restart filling from the beginning of the buffer
		bool bReadWasBeforeWrite = m_iReadNext < m_iWriteNext;
		memcpy((m_pBuffer + m_iWriteNext), szMsg, (m_nSize - m_iWriteNext) * sizeof(szMsg[0]));
		memcpy(m_pBuffer, (szMsg + m_nSize - m_iWriteNext), (nSize - m_nSize + m_iWriteNext) * sizeof(szMsg[0]));

		// Update feed position and check if we have written over read position, and move it
		m_iWriteNext = nSize + m_iWriteNext - m_nSize;
		if (bReadWasBeforeWrite && (m_iReadNext < m_iWriteNext))
			m_iReadNext = m_iWriteNext;
	}
	else {
		// Copy the entire szMsg to the first free location
		bool bReadWasAfterWrite = m_iReadNext > m_iWriteNext;
		memcpy((m_pBuffer + m_iWriteNext), szMsg, nSize * sizeof(szMsg[0]));

		// Update feed position then check if we're over the read spot.
		m_iWriteNext = (m_iWriteNext + nSize) % m_nSize;
		if (bReadWasAfterWrite && (m_iReadNext < m_iWriteNext))
			m_iReadNext = m_iWriteNext;
	}

	// More items in the buffer and return
	m_nItems = m_nItems + nSize < m_nSize ? m_nItems + nSize : m_nSize;
}

/// <summary>
/// Gets a file object to write into
/// </summary>
/// <param name="fp"></param>
void fulcrum_cfifo::Get(FILE* fp)
{
	// Build size string value
	size_t n = m_nItems;

	// Check if the next reader spot is less than the current reader spot
	if ((m_iReadNext + n) <= m_nSize) {
		fwrite(m_pBuffer + m_iReadNext, 1, n * sizeof(TCHAR), fp);
		m_iReadNext = (m_iReadNext + n) % m_nSize;
	}
	else {
		// Build part values by substring splitting
		size_t nPart1 = m_nSize - m_iReadNext;
		size_t nPart2 = n - nPart1;

		// Copy the tail value then copy the start of our string output.
		fwrite(m_pBuffer + m_iReadNext, sizeof(TCHAR), nPart1, fp);
		fwrite(m_pBuffer, sizeof(TCHAR), nPart2, fp);
		m_iReadNext = nPart2;
	}

	// Remove the item from our queue of outputs
	m_nItems -= n;
}