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
#include <rpcndr.h>

// Fulcrum Resource Imports
#include "fulcrum_j2534.h"

class fulcrum_bitconverter
{
public:
	static int bytes_to_int32(byte b[], int offset);
	static unsigned int bytes_to_uint32(byte b[], int offset);
	static void bytes_to_char_pointer(byte b[], int offset, int len, char* out);
	static void bytes_to_ptmsg_header(byte b[], int offset, PASSTHRU_MSG* pMsg);

	static void int32_to_bytes(int val, byte b[], int offset);
	static void uint32_to_bytes(unsigned int val, byte b[], int offset);
	static void ptmsg_header_to_bytes(PASSTHRU_MSG* pMsg, byte b[], int offset);
};

