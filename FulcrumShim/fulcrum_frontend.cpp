#include "stdafx.h"

#include <tchar.h>
#include <windows.h> 
#include <chrono>
#include <process.h>
#include <Tlhelp32.h>
#include <winbase.h>
#include <string.h>
#include <comdef.h>  

#include "FulcrumShim.h"
#include "fulcrum_jpipe.h"
#include "fulcrum_j2534.h"
#include "fulcrum_debug.h"
#include "fulcrum_loader.h"
#include "fulcrum_output.h"
#include "SelectionBox.h"

#define fulcrum_CHECK_DLL() \
{ \
	if (! fulcrum_checkAndAutoload()) \
	{ \
		fulcrum_setInternalError(_T("FulcrumShim has not loaded a J2534 DLL")); \
		fulcrum_printretval(ERR_FAILED); \
		return ERR_FAILED; \
	} \
}

#define fulcrum_CHECK_FUNCTION(fcn) \
{ \
	if (__FUNCTION__ == NULL) \
	{ \
		fulcrum_setInternalError(_T("DLL loaded but does not export %s"), __FUNCTION__); \
		fulcrum_printretval(ERR_FAILED); \
		return ERR_FAILED; \
	} \
}


// Used to pulling infor staticly from commands.
static unsigned int lastVBATTVal = 0;
static auto lastVBATTReadTime = std::chrono::steady_clock::now();
using lastVBATTReadResolution = std::chrono::seconds;
static int lastVBATTReadThreshold = 10; // seconds

void PASSTHRU_MSG_ToVOIDPointer(PASSTHRU_MSG* pMsgIn, void* pMsgOut)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	PASSTHRU_MSG* ptmOut = (PASSTHRU_MSG*)pMsgOut;
	ptmOut->ProtocolID = pMsgIn->ProtocolID;
	ptmOut->RxStatus = pMsgIn->RxStatus;
	ptmOut->TxFlags = pMsgIn->TxFlags;
	ptmOut->Timestamp = pMsgIn->Timestamp;
	ptmOut->DataSize = pMsgIn->DataSize;
	ptmOut->ExtraDataIndex = pMsgIn->ExtraDataIndex;
	memcpy_s(ptmOut->Data, 4128, pMsgIn->Data, pMsgIn->DataSize);
}

// Load And Unload Commands
extern "C" long J2534_API PassThruLoadLibrary(char * szFunctionLibrary)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTLoadLibrary(%s)\n"), GetTimeSinceInit(), (szFunctionLibrary==NULL)?_T("*NULL*"):_T("test")/*szLibrary*/);

	if (szFunctionLibrary == NULL)
	{
		// Return an error. Perhaps we want to change NULL to do an autodetect and popup?
		fulcrum_setInternalError(_T("szFunctionLibrary was zero"));
		fulcrum_printretval(ERR_NULL_PARAMETER);
		return ERR_NULL_PARAMETER;
	}

	CStringW cstrLibrary(szFunctionLibrary);
	bool fSuccess;
	fSuccess = fulcrum_loadLibrary(cstrLibrary);
	if (! fSuccess)
	{
		fulcrum_setInternalError(_T("Failed to open '%s'"), cstrLibrary);
		fulcrum_printretval(ERR_FAILED);
		return ERR_FAILED;
	}

	fulcrum_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}
extern "C" long J2534_API PassThruUnloadLibrary()
{
	// Store app state
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	// Unload our library here
	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTUnloadLibrary()\n"), GetTimeSinceInit());
	fulcrum_unloadLibrary();
	fulcrum_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

// Logging commands
extern "C" long J2534_API PassThruWriteToLogA(char *szMsg)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CStringW cstrMsg(szMsg);

	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ** '%s'\n"), GetTimeSinceInit(), cstrMsg);
	return STATUS_NOERROR;
}
extern "C" long J2534_API PassThruWriteToLogW(wchar_t *szMsg)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ** '%s'\n"), GetTimeSinceInit(), szMsg);
	return STATUS_NOERROR;
}
extern "C" long J2534_API PassThruSaveLog(char *szFilename)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTSaveLog(%s)\n"), GetTimeSinceInit(), (szFilename==NULL)?_T("*NULL*"):_T("")/*pName*/);
	CStringW cstrFilename(szFilename);
	fulcrum_output::writeNewLogFile(cstrFilename, false);

	fulcrum_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

// Standard PT Command methods. This will be built out to V0500 soon
extern "C" long J2534_API PassThruOpen(void *pName, unsigned long *pDeviceID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	unsigned long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTOpen(%s, 0x%08X)\n"), GetTimeSinceInit(), (pName==NULL)?_T("*NULL*"):_T("")/*pName*/, pDeviceID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruOpen);

	retval = _PassThruOpen(pName, pDeviceID);
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("  returning DeviceID: %ld\n"), *pDeviceID);
	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruClose(unsigned long DeviceID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs -- PTClose(%ld)\n"), GetTimeSinceInit(), DeviceID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruClose);

	retval = _PassThruClose(DeviceID);
	fulcrum_printretval(retval);

	// Shut off pipes
	CFulcrumShim* fulcrum_app = static_cast<CFulcrumShim*>(AfxGetApp());
	if (!fulcrum_app->pipesLoaded) { fulcrum_app->ShutdownPipes(); }
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs    FulcrumInjector has closed all pipe instances OK!\n"), GetTimeSinceInit());

	// Return output value
	return retval;
}
extern "C" long J2534_API PassThruConnect(unsigned long DeviceID, unsigned long ProtocolID, unsigned long Flags, unsigned long Baudrate, unsigned long *pChannelID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTConnect(%ld, %s, 0x%08X, %ld, 0x%08X)\n"), GetTimeSinceInit(), DeviceID, fulcrumDebug_prot(ProtocolID).c_str(), Flags, Baudrate, pChannelID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruConnect);

	fulcrumDebug_printcflag(Flags);
	retval = _PassThruConnect(DeviceID, ProtocolID, Flags, Baudrate, pChannelID);
	if (pChannelID == NULL) fulcrum_output::fulcrum_output::fulcrumDebug(_T("  pChannelID was NULL\n"));
	else fulcrum_output::fulcrum_output::fulcrumDebug(_T("  returning ChannelID: %ld\n"), *pChannelID);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruDisconnect(unsigned long ChannelID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs -- PTDisconnect(%ld)\n"), GetTimeSinceInit(), ChannelID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruDisconnect);

	retval = _PassThruDisconnect(ChannelID);
	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruReadMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;
	unsigned long reqNumMsgs;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs << PTReadMsgs(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pNumMsgs, Timeout);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruReadMsgs);

	if (pNumMsgs != NULL) reqNumMsgs = *pNumMsgs;
	retval = _PassThruReadMsgs(ChannelID, pMsg, pNumMsgs, Timeout);
	if (pNumMsgs != NULL) fulcrum_output::fulcrum_output::fulcrumDebug(_T("  read %ld of %ld messages\n"), *pNumMsgs, reqNumMsgs);
	fulcrumDebug_printmsg(pMsg, _T("Msg"), pNumMsgs, FALSE);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruWriteMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;
	unsigned long reqNumMsgs = *pNumMsgs;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs >> PTWriteMsgs(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pNumMsgs, Timeout);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruWriteMsgs);

	if (pNumMsgs != NULL) reqNumMsgs = *pNumMsgs;
	fulcrumDebug_printmsg(pMsg, _T("Msg"), pNumMsgs, true);
	retval = _PassThruWriteMsgs(ChannelID, pMsg, pNumMsgs, Timeout);
	if (pNumMsgs != NULL) fulcrum_output::fulcrum_output::fulcrumDebug(_T("  sent %ld of %ld messages\n"), *pNumMsgs, reqNumMsgs);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStartPeriodicMsg(unsigned long ChannelID, PASSTHRU_MSG *pMsg,
                      unsigned long *pMsgID, unsigned long TimeInterval)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTStartPeriodicMsg(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pMsgID, TimeInterval);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStartPeriodicMsg);
	
	fulcrumDebug_printmsg(pMsg, _T("Msg"), 1, true);
	retval = _PassThruStartPeriodicMsg(ChannelID, pMsg, pMsgID, TimeInterval);
	if (pMsgID != NULL)	fulcrum_output::fulcrum_output::fulcrumDebug(_T("  returning PeriodicID: %ld\n"), *pMsgID);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStopPeriodicMsg(unsigned long ChannelID, unsigned long MsgID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs -- PTStopPeriodicMsg(%ld, %ld)\n"), GetTimeSinceInit(), ChannelID, MsgID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStopPeriodicMsg);

	retval = _PassThruStopPeriodicMsg(ChannelID, MsgID);
	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStartMsgFilter(unsigned long ChannelID,
                      unsigned long FilterType, PASSTHRU_MSG *pMaskMsg, PASSTHRU_MSG *pPatternMsg,
					  PASSTHRU_MSG *pFlowControlMsg, unsigned long *pMsgID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ++ PTStartMsgFilter(%ld, %s, 0x%08X, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), ChannelID, fulcrumDebug_filter(FilterType).c_str(),
		pMaskMsg, pPatternMsg, pFlowControlMsg, pMsgID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStartMsgFilter);

	fulcrumDebug_printmsg(pMaskMsg, _T("Mask"), 1, true);
	fulcrumDebug_printmsg(pPatternMsg, _T("Pattern"), 1, true);
	fulcrumDebug_printmsg(pFlowControlMsg, _T("FlowControl"), 1, true);
	retval = _PassThruStartMsgFilter(ChannelID, FilterType, pMaskMsg, pPatternMsg, pFlowControlMsg, pMsgID);
	if (pMsgID != NULL) fulcrum_output::fulcrum_output::fulcrumDebug(_T("  returning FilterID: %ld\n"), *pMsgID);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStopMsgFilter(unsigned long ChannelID, unsigned long MsgID)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs -- PTStopMsgFilter(%ld, %ld)\n"), GetTimeSinceInit(), ChannelID, MsgID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStopMsgFilter);

	retval = _PassThruStopMsgFilter(ChannelID, MsgID);
	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruSetProgrammingVoltage(unsigned long DeviceID, unsigned long Pin, unsigned long Voltage)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ** PTSetProgrammingVoltage(%ld, %ld, %ld)\n"), GetTimeSinceInit(), DeviceID, Pin, Voltage);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruSetProgrammingVoltage);

	switch (Voltage)
	{
	case VOLTAGE_OFF:
		fulcrum_output::fulcrum_output::fulcrumDebug(_T("  Pin %ld remove voltage\n"), Pin);
		break;
	case SHORT_TO_GROUND:
		fulcrum_output::fulcrum_output::fulcrumDebug(_T("  Pin %ld short to ground\n"), Pin);
		break;
	default:
		fulcrum_output::fulcrum_output::fulcrumDebug(_T("  Pin %ld at %f Volts\n"), Pin, Voltage / (float) 1000);
		break;
	}
	retval = _PassThruSetProgrammingVoltage(DeviceID, Pin, Voltage);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruReadVersion(unsigned long DeviceID, char *pFirmwareVersion, char *pDllVersion, char *pApiVersion)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ** PTReadVersion(%ld, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), DeviceID, pFirmwareVersion, pDllVersion, pApiVersion);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruReadVersion);

	retval = _PassThruReadVersion(DeviceID, pFirmwareVersion, pDllVersion, pApiVersion);

	CStringW cstrFirmwareVersion(pFirmwareVersion);
	CStringW cstrDllVersion(pDllVersion);
	CStringW cstrApiVersion(pApiVersion);

	fulcrum_output::fulcrum_output::fulcrumDebug(_T("  Firmware: %s\n"), cstrFirmwareVersion);
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("  DLL:      %s\n"), cstrDllVersion);
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("  API:      %s\n"), cstrApiVersion);

	fulcrum_printretval(retval);
	return retval;
}

// Error helper for pulling error definition from the J2534 error object
long fulcrum_PassThruGetLastError(char* pErrorDescription)
{
	if (fulcrum_hadInternalError())
	{
		if (pErrorDescription == NULL)
			return ERR_NULL_PARAMETER;

		// We'll intercept GetLastError if we're reporting something about the shim
		CStringA cstrInternalLastError((LPCTSTR)fulcrum_getInternalError());
		strncpy_s(pErrorDescription, 80, cstrInternalLastError, _TRUNCATE);
		return STATUS_NOERROR;
	}
	else
	{
		// These macros call fulcrum_setInternalError() which does not work the way
		// this function is documented. They should be replaced with code that
		// prints an error to the debug log and copies the text to pErrorDescription
		// if the pointer is non-NULL
		fulcrum_CHECK_DLL();
		fulcrum_CHECK_FUNCTION(_PassThruGetLastError);

		return _PassThruGetLastError(pErrorDescription);
	}
}
extern "C" long J2534_API PassThruGetLastError(char* pErrorDescription)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	// pErrorDescription returns the text description for an error detected
	// during the last function call (EXCEPT PassThruGetLastError). This
	// function should not modify the last internal error

	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ** PTGetLastError(0x%08X)\n"), GetTimeSinceInit(), pErrorDescription);

	if (pErrorDescription == NULL)
	{
		fulcrum_output::fulcrum_output::fulcrumDebug(_T("  pErrorDescription is NULL\n"));
	}

	retval = fulcrum_PassThruGetLastError(pErrorDescription);

	if (pErrorDescription != NULL)
	{
#ifdef UNICODE
		CStringW cstrErrorDescriptionW(pErrorDescription);
		fulcrum_output::fulcrum_output::fulcrumDebug(_T("  %s\n"), (LPCWSTR)cstrErrorDescriptionW);
#else
		dtDebug(_T("  %s\n"), pErrorDescription);
#endif
	}

	// Log the return value for this function without using dbg_printretval().
	// Even if an error occured inside this function, the error text was not
	// updated to describe the error.
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("  %s\n"), fulcrumDebug_return(retval).c_str());
	return retval;
}
extern "C" long J2534_API PassThruIoctl(unsigned long ChannelID, unsigned long IoctlID, void* pInput, void* pOutput)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;
	long retval;

	fulcrum_clearInternalError();
	fulcrum_output::fulcrum_output::fulcrumDebug(_T("%.3fs ** PTIoctl(%ld, %s, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), ChannelID, fulcrumDebug_ioctl(IoctlID).c_str(), pInput, pOutput);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruIoctl);

	// Print any relevant info before making the call
	switch (IoctlID)
	{
		// Do nothing for GET_CONFIG input
	case SET_CONFIG:
		dbug_printsconfig((SCONFIG_LIST*)pInput);
		break;
		// Do nothing for READ_VBATT input
	case FIVE_BAUD_INIT:
		fulcrumDebug_printsbyte((SBYTE_ARRAY*)pInput, _T("Input"));
		break;
	case FAST_INIT:
		fulcrumDebug_printmsg((PASSTHRU_MSG*)pInput, _T("Input"), 1, true);
		break;
		// Do nothing for CLEAR_TX_BUFFER
		// Do nothing for CLEAR_RX_BUFFER
		// Do nothing for CLEAR_PERIODIC_MSGS
		// Do nothing for CLEAR_MSG_FILTERS
		// Do nothing for CLEAR_FUNCT_MSG_LOOKUP_TABLE
	case ADD_TO_FUNCT_MSG_LOOKUP_TABLE:
		fulcrumDebug_printsbyte((SBYTE_ARRAY*)pInput, _T("Add"));
		break;
	case DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE:
		fulcrumDebug_printsbyte((SBYTE_ARRAY*)pInput, _T("Delete"));
		break;
		// Do nothing for READ_PROG_VOLTAGE
	}

	retval = _PassThruIoctl(ChannelID, IoctlID, pInput, pOutput);

	// Print any changed info after making the call
	switch (IoctlID)
	{
	case GET_CONFIG:
		dbug_printsconfig((SCONFIG_LIST*)pInput);
		break;
		// Do nothing for SET_CONFIG
	case READ_VBATT:
		if (pOutput != NULL)
			fulcrum_output::fulcrum_output::fulcrumDebug(_T("  %f Volts\n"), ((*(unsigned long*)pOutput)) / (float)1000);
		break;
	case FIVE_BAUD_INIT:
		fulcrumDebug_printsbyte((SBYTE_ARRAY*)pInput, _T("Output"));
		break;
	case FAST_INIT:
		fulcrumDebug_printmsg((PASSTHRU_MSG*)pOutput, _T("Input"), 1, false);
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
			fulcrum_output::fulcrum_output::fulcrumDebug(_T("  %f Volts\n"), ((*(unsigned long*)pOutput)) / (float)1000);
		break;
	}

	fulcrum_printretval(retval);
	return retval;
}
