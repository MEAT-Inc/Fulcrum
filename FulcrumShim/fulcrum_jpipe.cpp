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

#include "fulcrum_pipe.h"
#include "fulcrum_jpipe.h"
#include "fulcrum_bitconverter.h"


void fulcrum_jpipe::WriteSByteArray(SBYTE_ARRAY* ary)
{
	if (ary == NULL)
		throw& CPipExceptionNULLParameter();

	WriteUint32(ary->NumOfBytes);
	WriteBytesOut(ary->BytePtr, ary->NumOfBytes);
}

void fulcrum_jpipe::ReadSByteArray(SBYTE_ARRAY* ary)
{
	if (ary == NULL)
		throw& CPipExceptionNULLParameter();

	ary->NumOfBytes = ReadUint32();
	ReadBytes(ary->BytePtr, ary->NumOfBytes);
}

void fulcrum_jpipe::ReadSParam(SPARAM* param)
{
	if (param == NULL)
		throw& CPipExceptionNULLParameter();

	param->Parameter = ReadUint32();
	param->Value = ReadUint32();
	param->Supported = ReadUint32();
}

void fulcrum_jpipe::WriteSParamList(SPARAM_LIST* list)
{
	if (list == NULL)
		throw& CPipExceptionNULLParameter();

	WriteUint32(list->NumOfParams);
	for (unsigned int i = 0; i < list->NumOfParams; i++)
		WriteSParam(&list->ParamPtr[i]);
}

void fulcrum_jpipe::ReadSParamList(SPARAM_LIST* list)
{
	if (list == NULL)
		throw& CPipExceptionNULLParameter();

	list->NumOfParams = ReadUint32();
	for (unsigned int i = 0; i < list->NumOfParams; i++)
		ReadSParam(&list->ParamPtr[i]);
}

// (10/18/19 TAB)
void fulcrum_jpipe::WriteResourceStruct(RESOURCE_STRUCT res)
{
	WriteUint32(res.Connector);
	WriteUint32(res.NumOfResources);
	WriteUint32(res.ResourceListPtr, res.NumOfResources);
}

void fulcrum_jpipe::IssueGetProtocolInfo(unsigned int protocolID, SPARAM_LIST* paramlist)
{
	if (paramlist == NULL)
		throw& CPipExceptionNULLParameter();

	WriteUint32(protocolID);
	WriteSParamList(paramlist);
}

void fulcrum_jpipe::GetGetProtocolInfoResponse(unsigned int* protocolID, SPARAM_LIST* paramlist)
{
	if (protocolID == NULL || paramlist == NULL)
		throw& CPipExceptionNULLParameter();

	*protocolID = ReadUint32();
	ReadSParamList(paramlist);
}


void fulcrum_jpipe::IssueGetDeviceInfo(SPARAM_LIST* paramlist)
{
	if (paramlist == NULL)
		throw& CPipExceptionNULLParameter();

	WriteSParamList(paramlist);
}
void fulcrum_jpipe::GetGetDeviceInfoResponse(SPARAM_LIST* paramlist)
{
	if (paramlist == NULL)
		throw& CPipExceptionNULLParameter();

	ReadSParamList(paramlist);
}

void fulcrum_jpipe::ReadPassThruMessages(PASSTHRU_MSG* pMsgs, uint32_t numMsgs)
{
	if (pMsgs == NULL)
		throw& CPipExceptionNULLParameter();

	for (unsigned int i = 0; i < numMsgs; i++)
	{
		pMsgs[i].ProtocolID = (protocol_id_t)ReadUint32();
		ReadUint32();															// eat msgHandle
		pMsgs[i].RxStatus = (rx_status_t)ReadUint32();
		pMsgs[i].TxFlags = (tx_flag_t)ReadUint32();
		pMsgs[i].Timestamp = ReadUint32();
		pMsgs[i].DataSize = ReadUint32();
		pMsgs[i].ExtraDataIndex = ReadUint32();
		ReadUint32();															// eat dataBufferSize
		ReadBytesIn((byte*)pMsgs[i].Data, (int*)&pMsgs[i].DataSize);
	}
}

void fulcrum_jpipe::WritePassThruMessages(PASSTHRU_MSG* pMsgs, uint32_t numMsgs)
{
	if (pMsgs == NULL)
		throw& CPipExceptionNULLParameter();

	for (unsigned int i = 0; i < numMsgs; i++)
	{
		WriteUint32(pMsgs[i].ProtocolID);
		WriteUint32(0);															// made up handle
		WriteUint32(pMsgs[i].RxStatus);
		WriteUint32(pMsgs[i].TxFlags);
		WriteUint32(pMsgs[i].Timestamp);
		WriteUint32(pMsgs[i].DataSize);
		WriteUint32(pMsgs[i].ExtraDataIndex);
		WriteUint32(pMsgs[i].DataSize);											// don't care, just use size
		WriteBytesOut((byte*)pMsgs[i].Data, pMsgs[i].DataSize);
	}
}