/*
**
** Copyright (C) 2009 Drew Technologies Inc.
** Author: Joey Oravec <joravec@drewtech.com>
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


#include <stdafx.h>

#include <tchar.h>
#include <windows.h> 

#include "j2534_v0404.h"
#include "shim_debug.h"
#include "shim_loader.h"
#include "shim_output.h"

#define SHIM_CHECK_DLL() \
{ \
	if (! shim_checkAndAutoload()) \
	{ \
		shim_setInternalError(_T("PassThruShim has not loaded a J2534 DLL")); \
		dbug_printretval(ERR_FAILED); \
		return ERR_FAILED; \
	} \
}

#define SHIM_CHECK_FUNCTION(fcn) \
{ \
	if (__FUNCTION__ == NULL) \
	{ \
		shim_setInternalError(_T("DLL loaded but does not export %s"), __FUNCTION__); \
		dbug_printretval(ERR_FAILED); \
		return ERR_FAILED; \
	} \
}

extern "C" long J2534_API PassThruLoadLibrary(char * szFunctionLibrary)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	shim_clearInternalError();

	dtDebug(_T("%.3fs ++ PTLoadLibrary(%s)\n"), GetTimeSinceInit(), (szFunctionLibrary==NULL)?_T("*NULL*"):_T("test")/*szLibrary*/);

	if (szFunctionLibrary == NULL)
	{
		// Return an error. Perhaps we want to change NULL to do an autodetect and popup?
		shim_setInternalError(_T("szFunctionLibrary was zero"));
		dbug_printretval(ERR_NULL_PARAMETER);
		return ERR_NULL_PARAMETER;
	}

	CStringW cstrLibrary(szFunctionLibrary);
	bool fSuccess;
	fSuccess = shim_loadLibrary(cstrLibrary);
	if (! fSuccess)
	{
		shim_setInternalError(_T("Failed to open '%s'"), cstrLibrary);
		dbug_printretval(ERR_FAILED);
		return ERR_FAILED;
	}

	dbug_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

extern "C" long J2534_API PassThruUnloadLibrary()
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	shim_clearInternalError();

	dtDebug(_T("%.3fs ++ PTUnloadLibrary()\n"), GetTimeSinceInit());

	shim_unloadLibrary();

	dbug_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

extern "C" long J2534_API PassThruWriteToLogA(char *szMsg)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CStringW cstrMsg(szMsg);

	dtDebug(_T("%.3fs ** '%s'\n"), GetTimeSinceInit(), cstrMsg);

	return STATUS_NOERROR;
}

extern "C" long J2534_API PassThruWriteToLogW(wchar_t *szMsg)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	dtDebug(_T("%.3fs ** '%s'\n"), GetTimeSinceInit(), szMsg);

	return STATUS_NOERROR;
}

extern "C" long J2534_API PassThruSaveLog(char *szFilename)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	shim_clearInternalError();

	dtDebug(_T("%.3fs ++ PTSaveLog(%s)\n"), GetTimeSinceInit(), (szFilename==NULL)?_T("*NULL*"):_T("")/*pName*/);

	CStringW cstrFilename(szFilename);
	shim_writeLogfile(cstrFilename, false);

	dbug_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

extern "C" long J2534_API PassThruOpen(void *pName, unsigned long *pDeviceID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	unsigned long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ++ PTOpen(%s, 0x%08X)\n"), GetTimeSinceInit(), (pName==NULL)?_T("*NULL*"):_T("")/*pName*/, pDeviceID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruOpen);

	retval = _PassThruOpen(pName, pDeviceID);
	dtDebug(_T("  returning DeviceID: %ld\n"), *pDeviceID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruClose(unsigned long DeviceID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs -- PTClose(%ld)\n"), GetTimeSinceInit(), DeviceID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruClose);

	retval = _PassThruClose(DeviceID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruConnect(unsigned long DeviceID, unsigned long ProtocolID, unsigned long Flags, unsigned long Baudrate, unsigned long *pChannelID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ++ PTConnect(%ld, %s, 0x%08X, %ld, 0x%08X)\n"), GetTimeSinceInit(), DeviceID, dbug_prot(ProtocolID).c_str(), Flags, Baudrate, pChannelID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruConnect);

	dbug_printcflag(Flags);
	retval = _PassThruConnect(DeviceID, ProtocolID, Flags, Baudrate, pChannelID);
	if (pChannelID == NULL)
		dtDebug(_T("  pChannelID was NULL\n"));
	else
		dtDebug(_T("  returning ChannelID: %ld\n"), *pChannelID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruDisconnect(unsigned long ChannelID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs -- PTDisconnect(%ld)\n"), GetTimeSinceInit(), ChannelID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruDisconnect);

	retval = _PassThruDisconnect(ChannelID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruReadMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;
	unsigned long reqNumMsgs;

	shim_clearInternalError();
	dtDebug(_T("%.3fs << PTReadMsgs(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pNumMsgs, Timeout);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruReadMsgs);

	if (pNumMsgs != NULL)
		reqNumMsgs = *pNumMsgs;
	retval = _PassThruReadMsgs(ChannelID, pMsg, pNumMsgs, Timeout);
	if (pNumMsgs != NULL)
		dtDebug(_T("  read %ld of %ld messages\n"), *pNumMsgs, reqNumMsgs);
	dbug_printmsg(pMsg, _T("Msg"), pNumMsgs, FALSE);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruWriteMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;
	unsigned long reqNumMsgs = *pNumMsgs;

	shim_clearInternalError();
	dtDebug(_T("%.3fs >> PTWriteMsgs(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pNumMsgs, Timeout);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruWriteMsgs);

	if (pNumMsgs != NULL)
		reqNumMsgs = *pNumMsgs;
	dbug_printmsg(pMsg, _T("Msg"), pNumMsgs, true);
	retval = _PassThruWriteMsgs(ChannelID, pMsg, pNumMsgs, Timeout);
	if (pNumMsgs != NULL)
		dtDebug(_T("  sent %ld of %ld messages\n"), *pNumMsgs, reqNumMsgs);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruStartPeriodicMsg(unsigned long ChannelID, PASSTHRU_MSG *pMsg,
                      unsigned long *pMsgID, unsigned long TimeInterval)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ++ PTStartPeriodicMsg(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pMsgID, TimeInterval);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruStartPeriodicMsg);
	
	dbug_printmsg(pMsg, _T("Msg"), 1, true);
	retval = _PassThruStartPeriodicMsg(ChannelID, pMsg, pMsgID, TimeInterval);
	if (pMsgID != NULL)
		dtDebug(_T("  returning PeriodicID: %ld\n"), *pMsgID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruStopPeriodicMsg(unsigned long ChannelID, unsigned long MsgID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs -- PTStopPeriodicMsg(%ld, %ld)\n"), GetTimeSinceInit(), ChannelID, MsgID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruStopPeriodicMsg);

	retval = _PassThruStopPeriodicMsg(ChannelID, MsgID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruStartMsgFilter(unsigned long ChannelID,
                      unsigned long FilterType, PASSTHRU_MSG *pMaskMsg, PASSTHRU_MSG *pPatternMsg,
					  PASSTHRU_MSG *pFlowControlMsg, unsigned long *pMsgID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ++ PTStartMsgFilter(%ld, %s, 0x%08X, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), ChannelID, dbug_filter(FilterType).c_str(),
		pMaskMsg, pPatternMsg, pFlowControlMsg, pMsgID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruStartMsgFilter);

	dbug_printmsg(pMaskMsg, _T("Mask"), 1, true);
	dbug_printmsg(pPatternMsg, _T("Pattern"), 1, true);
	dbug_printmsg(pFlowControlMsg, _T("FlowControl"), 1, true);
	retval = _PassThruStartMsgFilter(ChannelID, FilterType, pMaskMsg, pPatternMsg, pFlowControlMsg, pMsgID);
	if (pMsgID != NULL)
		dtDebug(_T("  returning FilterID: %ld\n"), *pMsgID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruStopMsgFilter(unsigned long ChannelID, unsigned long MsgID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs -- PTStopMsgFilter(%ld, %ld)\n"), GetTimeSinceInit(), ChannelID, MsgID);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruStopMsgFilter);

	retval = _PassThruStopMsgFilter(ChannelID, MsgID);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruSetProgrammingVoltage(unsigned long DeviceID, unsigned long Pin, unsigned long Voltage)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ** PTSetProgrammingVoltage(%ld, %ld, %ld)\n"), GetTimeSinceInit(), DeviceID, Pin, Voltage);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruSetProgrammingVoltage);

	switch (Voltage)
	{
	case VOLTAGE_OFF:
		dtDebug(_T("  Pin %ld remove voltage\n"), Pin);
		break;
	case SHORT_TO_GROUND:
		dtDebug(_T("  Pin %ld short to ground\n"), Pin);
		break;
	default:
		dtDebug(_T("  Pin %ld at %f Volts\n"), Pin, Voltage / (float) 1000);
		break;
	}
	retval = _PassThruSetProgrammingVoltage(DeviceID, Pin, Voltage);

	dbug_printretval(retval);
	return retval;
}

extern "C" long J2534_API PassThruReadVersion(unsigned long DeviceID, char *pFirmwareVersion, char *pDllVersion, char *pApiVersion)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ** PTReadVersion(%ld, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), DeviceID, pFirmwareVersion, pDllVersion, pApiVersion);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruReadVersion);

	retval = _PassThruReadVersion(DeviceID, pFirmwareVersion, pDllVersion, pApiVersion);

	CStringW cstrFirmwareVersion(pFirmwareVersion);
	CStringW cstrDllVersion(pDllVersion);
	CStringW cstrApiVersion(pApiVersion);

	dtDebug(_T("  Firmware: %s\n"), cstrFirmwareVersion);
	dtDebug(_T("  DLL:      %s\n"), cstrDllVersion);
	dtDebug(_T("  API:      %s\n"), cstrApiVersion);

	dbug_printretval(retval);
	return retval;
}

long shim_PassThruGetLastError(char *pErrorDescription)
{
	if (shim_hadInternalError())
	{
		if (pErrorDescription == NULL)
			return ERR_NULL_PARAMETER;

		// We'll intercept GetLastError if we're reporting something about the shim
		CStringA cstrInternalLastError((LPCTSTR) shim_getInternalError());
		strncpy_s(pErrorDescription, 80, cstrInternalLastError, _TRUNCATE);
		return STATUS_NOERROR;
	}
	else
	{
		// These macros call shim_setInternalError() which does not work the way
		// this function is documented. They should be replaced with code that
		// prints an error to the debug log and copies the text to pErrorDescription
		// if the pointer is non-NULL
		SHIM_CHECK_DLL();
		SHIM_CHECK_FUNCTION(_PassThruGetLastError);

		return _PassThruGetLastError(pErrorDescription);
	}
}

extern "C" long J2534_API PassThruGetLastError(char *pErrorDescription)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	// pErrorDescription returns the text description for an error detected
	// during the last function call (EXCEPT PassThruGetLastError). This
	// function should not modify the last internal error

	dtDebug(_T("%.3fs ** PTGetLastError(0x%08X)\n"), GetTimeSinceInit(), pErrorDescription);

	if (pErrorDescription == NULL)
	{
		dtDebug(_T("  pErrorDescription is NULL\n"));
	}

	retval = shim_PassThruGetLastError(pErrorDescription);

	if (pErrorDescription != NULL)
	{
#ifdef UNICODE
		CStringW cstrErrorDescriptionW(pErrorDescription);
		dtDebug(_T("  %s\n"), (LPCWSTR) cstrErrorDescriptionW);
#else
		dtDebug(_T("  %s\n"), pErrorDescription);
#endif
	}

	// Log the return value for this function without using dbg_printretval().
	// Even if an error occured inside this function, the error text was not
	// updated to describe the error.
	dtDebug(_T("  %s\n"), dbug_return(retval).c_str());
	return retval;
}

extern "C" long J2534_API PassThruIoctl(unsigned long ChannelID, unsigned long IoctlID, void *pInput, void *pOutput)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	shim_clearInternalError();
	dtDebug(_T("%.3fs ** PTIoctl(%ld, %s, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), ChannelID, dbug_ioctl(IoctlID).c_str(), pInput, pOutput);
	SHIM_CHECK_DLL();
	SHIM_CHECK_FUNCTION(_PassThruIoctl);

	// Print any relevant info before making the call
	switch (IoctlID)
	{
	// Do nothing for GET_CONFIG input
	case SET_CONFIG:
		dbug_printsconfig((SCONFIG_LIST *) pInput);
		break;
	// Do nothing for READ_VBATT input
	case FIVE_BAUD_INIT:
		dbug_printsbyte((SBYTE_ARRAY *) pInput, _T("Input"));
		break;
	case FAST_INIT:
		dbug_printmsg((PASSTHRU_MSG *) pInput, _T("Input"), 1, true);
		break;
	// Do nothing for CLEAR_TX_BUFFER
	// Do nothing for CLEAR_RX_BUFFER
	// Do nothing for CLEAR_PERIODIC_MSGS
	// Do nothing for CLEAR_MSG_FILTERS
	// Do nothing for CLEAR_FUNCT_MSG_LOOKUP_TABLE
	case ADD_TO_FUNCT_MSG_LOOKUP_TABLE:
		dbug_printsbyte((SBYTE_ARRAY *) pInput, _T("Add"));
		break;
	case DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE:
		dbug_printsbyte((SBYTE_ARRAY *) pInput, _T("Delete"));
		break;
	// Do nothing for READ_PROG_VOLTAGE
	}

	retval = _PassThruIoctl(ChannelID, IoctlID, pInput, pOutput);

	// Print any changed info after making the call
	switch (IoctlID)
	{
	case GET_CONFIG:
		dbug_printsconfig((SCONFIG_LIST *) pInput);
		break;
	// Do nothing for SET_CONFIG
	case READ_VBATT:
		if (pOutput != NULL)
			dtDebug(_T("  %f Volts\n"), ((*(unsigned long*)pOutput)) / (float) 1000);
		break;
	case FIVE_BAUD_INIT:
		dbug_printsbyte((SBYTE_ARRAY *) pInput, _T("Output"));
		break;
	case FAST_INIT:
		dbug_printmsg((PASSTHRU_MSG *) pOutput, _T("Input"), 1, false);
		break;
	// Do nothing for CLEAR_TX_BUFFER
	// Do nothing for CLEAR_RX_BUFFER
	// Do nothing for CLEAR_PERIODIC_MSGS
	// Do nothing for CLEAR_MSG_FILTERS
	// Do nothing for CLEAR_FUNCT_MSG_LOOKUP_TABLE
	// Do nothing for ADD_TO_FUNCT_MSG_LOOKUP_TABLE:
	// Do nothing for DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE:
	case READ_PROG_VOLTAGE:
		if (pOutput != NULL)
			dtDebug(_T("  %f Volts\n"), ((*(unsigned long*)pOutput)) / (float) 1000);
		break;
	}

	dbug_printretval(retval);
	return retval;
}
