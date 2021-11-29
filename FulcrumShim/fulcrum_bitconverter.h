#pragma once

#include "fulcrum_j2534.h"
#include <string>
#include <rpcndr.h>

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

