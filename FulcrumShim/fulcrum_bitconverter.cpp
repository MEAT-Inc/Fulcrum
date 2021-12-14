#include "stdafx.h"
#include "fulcrum_bitconverter.h"
#include <vcruntime_string.h>


int fulcrum_bitconverter::bytes_to_int32(byte b[], int offset)
{
	return (b[offset + 3] << 24) | (b[offset + 2] << 16) | (b[offset + 1] << 8) | (b[offset]);
}

unsigned int fulcrum_bitconverter::bytes_to_uint32(byte b[], int offset)
{
	return (b[offset + 3] << 24) | (b[offset + 2] << 16) | (b[offset + 1] << 8) | (b[offset]);
}

void fulcrum_bitconverter::bytes_to_char_pointer(byte b[], int offset, int len, char* out)
{
	memcpy(out, b + offset, len);
}

void fulcrum_bitconverter::bytes_to_ptmsg_header(byte b[], int offset, PASSTHRU_MSG* pMsg)
{
	// need 24 bytes in byte b[]
	pMsg->ProtocolID = (protocol_id_t)bytes_to_int32(b, offset);
	pMsg->RxStatus = (rx_status_t)bytes_to_int32(b, 4 + offset);
	pMsg->TxFlags = (tx_flag_t)bytes_to_int32(b, 8 + offset);
	pMsg->Timestamp = bytes_to_int32(b, 12 + offset);
	pMsg->DataSize = bytes_to_int32(b, 16 + offset);
	pMsg->ExtraDataIndex = bytes_to_int32(b, 20 + offset);
}

void fulcrum_bitconverter::int32_to_bytes(int val, byte b[], int offset)
{
	b[3 + offset] = (byte)((val >> 24) & 0xFF);
	b[2 + offset] = (byte)((val >> 16) & 0xFF);
	b[1 + offset] = (byte)((val >> 8) & 0XFF);
	b[0 + offset] = (byte)((val & 0XFF));
}

void fulcrum_bitconverter::uint32_to_bytes(unsigned int val, byte b[], int offset)
{
	b[3 + offset] = (byte)((val >> 24) & 0xFF);
	b[2 + offset] = (byte)((val >> 16) & 0xFF);
	b[1 + offset] = (byte)((val >> 8) & 0XFF);
	b[0 + offset] = (byte)((val & 0XFF));
}

void fulcrum_bitconverter::ptmsg_header_to_bytes(PASSTHRU_MSG* pMsg, byte b[], int offset)
{
	// need 24 bytes in byte b[]
	uint32_to_bytes(pMsg->ProtocolID, b, offset);
	uint32_to_bytes(pMsg->RxStatus, b, 4 + offset);
	uint32_to_bytes(pMsg->TxFlags, b, 8 + offset);
	uint32_to_bytes(pMsg->Timestamp, b, 12 + offset);
	uint32_to_bytes(pMsg->DataSize, b, 16 + offset);
	uint32_to_bytes(pMsg->ExtraDataIndex, b, 20 + offset);
}