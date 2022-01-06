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

// Fulcrum Resource Imports
#include "fulcrum_pipe.h"
#include "fulcrum_j2534.h"

class fulcrum_jpipe : public fulcrum_pipe
{
public:
	fulcrum_jpipe() : fulcrum_pipe() {};
	~fulcrum_jpipe() {};

public:
	void WriteSByteArray(SBYTE_ARRAY* ary);
	void ReadSByteArray(SBYTE_ARRAY* ary);

	void WriteSParam(SPARAM* param)
	{
		if (param == NULL)
			throw& CPipExceptionNULLParameter();

		WriteUint32(param->Parameter);
		WriteUint32(param->Value);
		WriteUint32(param->Supported);
	}
	void ReadSParam(SPARAM* param);

	void WriteSParamList(SPARAM_LIST* list);
	void ReadSParamList(SPARAM_LIST* list);

	void WriteResourceStruct(RESOURCE_STRUCT res);

	void IssueGetProtocolInfo(unsigned int protocolID, SPARAM_LIST* paramlist);
	void GetGetProtocolInfoResponse(unsigned int* protocolID, SPARAM_LIST* paramlist);

	void IssueGetDeviceInfo(SPARAM_LIST* paramlist);
	void GetGetDeviceInfoResponse(SPARAM_LIST* paramlist);

	void WritePassThruMessages(PASSTHRU_MSG* pMsgs, uint32_t numMsgs);
	void ReadPassThruMessages(PASSTHRU_MSG* pMsgs, uint32_t numMsgs);
};

