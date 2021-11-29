#pragma once

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

