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

// Standard Imports
#include "stdafx.h"
#include <tchar.h>
#include <windows.h> 
#include <chrono>
#include <process.h>
#include <Tlhelp32.h>
#include <winbase.h>
#include <string.h>
#include <comdef.h>  

// Fulcrum Resource Imports
#include "FulcrumShim.h"
#include "SelectionBox.h"
#include "fulcrum_debug.h"
#include "fulcrum_jpipe.h"
#include "fulcrum_j2534.h"
#include "fulcrum_debug.h"
#include "fulcrum_loader.h"
#include "fulcrum_output.h"

// Check if the DLL is loaded and usable or not
#define fulcrum_CHECK_DLL() \
{ \
	if (! fulcrum_checkAndAutoload()) \
	{ \
		fulcrum_setInternalError(_T("FulcrumShim has not loaded a J2534 DLL")); \
		fulcrum_printretval(ERR_FAILED); \
		return ERR_FAILED; \
	} \
}

// Checks if a function is usable for this given DLL
#define fulcrum_CHECK_FUNCTION(fcn) \
{ \
	if (__FUNCTION__ == NULL) \
	{ \
		fulcrum_setInternalError(_T("DLL loaded but does not export %s"), __FUNCTION__); \
		fulcrum_printretval(ERR_FAILED); \
		return ERR_FAILED; \
	} \
}

// ------------------------------------------------------------------------------------------------

// Used to pulling infor staticly from commands.
static unsigned int lastVBATTVal = 0;
static auto lastVBATTReadTime = std::chrono::steady_clock::now();
using lastVBATTReadResolution = std::chrono::seconds;
static int lastVBATTReadThreshold = 10; // seconds

// ------------------------------------------------------------------------------------------------

// Converts a message into a void pointer object
void PASSTHRU_MSG_ToVOIDPointer(PASSTHRU_MSG* pMsgIn, void* pMsgOut)
{
	// Ensure the module is running in static state and acquire a lock for it.
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

// ------------------------------------------------------------------------------------------------

// Load And Unload Commands
extern "C" long J2534_API PassThruLoadLibrary(char * szFunctionLibrary)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState()); auto_lock lock;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Clear out old error values and print init for method
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTLoadLibrary(%s)\n"), GetTimeSinceInit(), (szFunctionLibrary==NULL)?_T("*NULL*"):_T("test")/*szLibrary*/);

	// If the lib loaded is null, throw error for no DLL
	if (szFunctionLibrary == NULL)
	{
		// Return an error. Perhaps we want to change NULL to do an autodetect and popup?
		fulcrum_setInternalError(_T("szFunctionLibrary was zero"));
		fulcrum_printretval(ERR_NULL_PARAMETER);
		return ERR_NULL_PARAMETER;
	}

	// Run the method, get our output value and print it out to our log file
	CStringW cstrLibrary(szFunctionLibrary); bool fSuccess;
	fSuccess = fulcrum_loadLibrary(cstrLibrary);
	if (!fSuccess)
	{
		fulcrum_setInternalError(_T("Failed to open '%s'"), cstrLibrary);
		fulcrum_printretval(ERR_FAILED);
		return ERR_FAILED;
	}

	// Print output values here
	fulcrum_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}
extern "C" long J2534_API PassThruUnloadLibrary()
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState()); auto_lock lock;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Unload our library here
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTUnloadLibrary()\n"), GetTimeSinceInit());
	fulcrum_unloadLibrary();

	// Unload pipe outputs
	// fulcrum_output::fulcrumDebug(_T("-->       Calling pipe shutdown methods now...\n"));
	// CFulcrumShim::fulcrumPiper->ShutdownInputPipe();
	// CFulcrumShim::fulcrumPiper->ShutdownOutputPipe();
	// fulcrum_output::fulcrumDebug(_T("-->       Pipe instances have been released OK!\n"));

	// Print output result from call
	fulcrum_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

// Logging commands
extern "C" long J2534_API PassThruWriteToLogA(char *szMsg)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CStringW cstrMsg(szMsg);

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Write output information for the log
	fulcrum_output::fulcrumDebug(_T("** %.3fs '%s'\n"), GetTimeSinceInit(), cstrMsg);
	return STATUS_NOERROR;
}
extern "C" long J2534_API PassThruWriteToLogW(wchar_t *szMsg)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Write output information for the log
	fulcrum_output::fulcrumDebug(_T("** %.3fs '%s'\n"), GetTimeSinceInit(), szMsg);
	return STATUS_NOERROR;
}
extern "C" long J2534_API PassThruSaveLog(char *szFilename)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Clear out old errors and print init for method
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTSaveLog(%s)\n"), GetTimeSinceInit(), (szFilename==NULL)?_T("*NULL*"):_T("")/*pName*/);

	// Get log file name and run method
	CStringW cstrFilename(szFilename);
	fulcrum_output::writeNewLogFile(cstrFilename, false);

	// Print output return value
	fulcrum_printretval(STATUS_NOERROR);
	return STATUS_NOERROR;
}

// Commands built out for getting the next possible passthru interface
extern "C" long J2534_API PassThruGetNextCarDAQ(unsigned long* pName, unsigned long* pAddr, unsigned long* pVersion)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; unsigned long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Clear out old error. Ensure DLL supports this method
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTGetNetCarDAQ(%s, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), pName, pAddr, pVersion);
	fulcrum_CHECK_DLL(); fulcrum_CHECK_FUNCTION(_PassThruGetNextCarDAQ);

	// Run the method, get our output value and print it out to our log file
	retval = _PassThruGetNextCarDAQ(pName, pAddr, pVersion);
	fulcrum_output::fulcrumDebug(_T("  %s\n"), retval);
	fulcrum_printretval(retval);
}
extern "C" long J2534_API PassThruReadDetails(unsigned long* pName)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; unsigned long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Clear out old error. Ensure DLL supports this method
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTReadDetails(%s, 0x%08X)\n"), GetTimeSinceInit(), pName);
	fulcrum_CHECK_DLL(); fulcrum_CHECK_FUNCTION(_PassThruReadDetails);

	// Run the method, get our output value and print it out to our log file
	retval = _PassThruReadDetails(pName);
	fulcrum_printretval(retval);
	return retval;
}

// Standard PTOpen and PTClose commands
extern "C" long J2534_API PassThruOpen(void *pName, unsigned long *pDeviceID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; unsigned long retval;
	
	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Now clear out old errors and log method init state then validate it can be run
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTOpen(%s, 0x%08X)\n"), GetTimeSinceInit(), (pName==NULL)?_T("*NULL*"):_T("")/*pName*/, pDeviceID);
	fulcrum_CHECK_DLL(); fulcrum_CHECK_FUNCTION(_PassThruOpen);

	// Invoke the method here and store output
	retval = _PassThruOpen(pName, pDeviceID);
	fulcrum_output::fulcrumDebug(_T("  returning DeviceID: %ld\n"), *pDeviceID);
	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruClose(unsigned long DeviceID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Clear existing error, validate method can be run or not.
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("-- %.3fs PTClose(%ld)\n"), GetTimeSinceInit(), DeviceID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruClose);

	// Close input pipe instance
	retval = _PassThruClose(DeviceID);
	fulcrum_printretval(retval);

	// Unload pipe outputs
	// fulcrum_output::fulcrumDebug(_T("-->       Calling pipe shutdown methods now...\n"));
	// CFulcrumShim::fulcrumPiper->ShutdownInputPipe();
	// fulcrum_output::fulcrumDebug(_T("-->       Pipe instances have been released OK!\n"));
	// CFulcrumShim::fulcrumPiper->ShutdownOutputPipe();

	// Get output value and return it here
	return retval;
}

// Standard PT Connect and Disconnect Methods
extern "C" long J2534_API PassThruConnect(unsigned long DeviceID, unsigned long ProtocolID, unsigned long Flags, unsigned long Baudrate, unsigned long *pChannelID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;	long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// Clear existing error, validate method can be run or not.
	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTConnect(%ld, %s, 0x%08X, %ld, 0x%08X)\n"), GetTimeSinceInit(), DeviceID, fulcrumDebug_prot(ProtocolID).c_str(), Flags, Baudrate, pChannelID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruConnect);

	// Run our method and print out flag information for our call to connect
	fulcrumDebug_printcflag(Flags);
	retval = _PassThruConnect(DeviceID, ProtocolID, Flags, Baudrate, pChannelID);
	if (pChannelID == NULL) fulcrum_output::fulcrumDebug(_T("  pChannelID was NULL\n"));
	else fulcrum_output::fulcrumDebug(_T("  returning ChannelID: %ld\n"), *pChannelID);

	// Print output and return output value
	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruDisconnect(unsigned long ChannelID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;	long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("-- %.3fs PTDisconnect(%ld)\n"), GetTimeSinceInit(), ChannelID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruDisconnect);

	retval = _PassThruDisconnect(ChannelID);
	fulcrum_printretval(retval);
	return retval;
}

// Reading and Writing Messages/Periodic messages
extern "C" long J2534_API PassThruReadMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;	long retval; unsigned long reqNumMsgs;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("<< %.3fs PTReadMsgs(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pNumMsgs, Timeout);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruReadMsgs);

	if (pNumMsgs != NULL) reqNumMsgs = *pNumMsgs;
	retval = _PassThruReadMsgs(ChannelID, pMsg, pNumMsgs, Timeout);
	if (pNumMsgs != NULL) fulcrum_output::fulcrumDebug(_T("  read %ld of %ld messages\n"), *pNumMsgs, reqNumMsgs);
	fulcrumDebug_printmsg(pMsg, _T("Msg"), pNumMsgs, FALSE);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruWriteMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval; unsigned long reqNumMsgs = *pNumMsgs;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T(">> %.3fs PTWriteMsgs(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pNumMsgs, Timeout);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruWriteMsgs);

	if (pNumMsgs != NULL) reqNumMsgs = *pNumMsgs;
	fulcrumDebug_printmsg(pMsg, _T("Msg"), pNumMsgs, true);
	retval = _PassThruWriteMsgs(ChannelID, pMsg, pNumMsgs, Timeout);
	if (pNumMsgs != NULL) fulcrum_output::fulcrumDebug(_T("  sent %ld of %ld messages\n"), *pNumMsgs, reqNumMsgs);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStartPeriodicMsg(unsigned long ChannelID, PASSTHRU_MSG *pMsg,
                      unsigned long *pMsgID, unsigned long TimeInterval)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTStartPeriodicMsg(%ld, 0x%08X, 0x%08X, %ld)\n"), GetTimeSinceInit(), ChannelID, pMsg, pMsgID, TimeInterval);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStartPeriodicMsg);
	
	fulcrumDebug_printmsg(pMsg, _T("Msg"), 1, true);
	retval = _PassThruStartPeriodicMsg(ChannelID, pMsg, pMsgID, TimeInterval);
	if (pMsgID != NULL)	fulcrum_output::fulcrumDebug(_T("  returning PeriodicID: %ld\n"), *pMsgID);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStopPeriodicMsg(unsigned long ChannelID, unsigned long MsgID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("-- %.3fs PTStopPeriodicMsg(%ld, %ld)\n"), GetTimeSinceInit(), ChannelID, MsgID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStopPeriodicMsg);

	retval = _PassThruStopPeriodicMsg(ChannelID, MsgID);
	fulcrum_printretval(retval);
	return retval;
}

// Message Filtering Start/Stop commands
extern "C" long J2534_API PassThruStartMsgFilter(unsigned long ChannelID,
                      unsigned long FilterType, PASSTHRU_MSG *pMaskMsg, PASSTHRU_MSG *pPatternMsg,
					  PASSTHRU_MSG *pFlowControlMsg, unsigned long *pMsgID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("++ %.3fs PTStartMsgFilter(%ld, %s, 0x%08X, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), ChannelID, fulcrumDebug_filter(FilterType).c_str(),
		pMaskMsg, pPatternMsg, pFlowControlMsg, pMsgID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStartMsgFilter);

	fulcrumDebug_printmsg(pMaskMsg, _T("Mask"), 1, true);
	fulcrumDebug_printmsg(pPatternMsg, _T("Pattern"), 1, true);
	fulcrumDebug_printmsg(pFlowControlMsg, _T("FlowControl"), 1, true);
	retval = _PassThruStartMsgFilter(ChannelID, FilterType, pMaskMsg, pPatternMsg, pFlowControlMsg, pMsgID);
	if (pMsgID != NULL) fulcrum_output::fulcrumDebug(_T("  returning FilterID: %ld\n"), *pMsgID);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruStopMsgFilter(unsigned long ChannelID, unsigned long MsgID)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock;	long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("-- %.3fs PTStopMsgFilter(%ld, %ld)\n"), GetTimeSinceInit(), ChannelID, MsgID);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruStopMsgFilter);

	retval = _PassThruStopMsgFilter(ChannelID, MsgID);
	fulcrum_printretval(retval);
	return retval;
}

// Programming Voltage and IOCTls
extern "C" long J2534_API PassThruSetProgrammingVoltage(unsigned long DeviceID, unsigned long Pin, unsigned long Voltage)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("** %.3fs PTSetProgrammingVoltage(%ld, %ld, %ld)\n"), GetTimeSinceInit(), DeviceID, Pin, Voltage);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruSetProgrammingVoltage);

	switch (Voltage)
	{
	case VOLTAGE_OFF:
		fulcrum_output::fulcrumDebug(_T("  Pin %ld remove voltage\n"), Pin);
		break;
	case SHORT_TO_GROUND:
		fulcrum_output::fulcrumDebug(_T("  Pin %ld short to ground\n"), Pin);
		break;
	default:
		fulcrum_output::fulcrumDebug(_T("  Pin %ld at %f Volts\n"), Pin, Voltage / (float) 1000);
		break;
	}
	retval = _PassThruSetProgrammingVoltage(DeviceID, Pin, Voltage);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruReadVersion(unsigned long DeviceID, char *pFirmwareVersion, char *pDllVersion, char *pApiVersion)
{
	// Ensure the module is running in static state and acquire a lock for it.
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("** %.3fs PTReadVersion(%ld, 0x%08X, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), DeviceID, pFirmwareVersion, pDllVersion, pApiVersion);
	fulcrum_CHECK_DLL();
	fulcrum_CHECK_FUNCTION(_PassThruReadVersion);

	retval = _PassThruReadVersion(DeviceID, pFirmwareVersion, pDllVersion, pApiVersion);

	CStringW cstrFirmwareVersion(pFirmwareVersion);
	CStringW cstrDllVersion(pDllVersion);
	CStringW cstrApiVersion(pApiVersion);

	fulcrum_output::fulcrumDebug(_T("  Firmware: %s\n"), cstrFirmwareVersion);
	fulcrum_output::fulcrumDebug(_T("  DLL:      %s\n"), cstrDllVersion);
	fulcrum_output::fulcrumDebug(_T("  API:      %s\n"), cstrApiVersion);

	fulcrum_printretval(retval);
	return retval;
}
extern "C" long J2534_API PassThruIoctl(unsigned long ChannelID, unsigned long IoctlID, void* pInput, void* pOutput)
{
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	fulcrum_clearInternalError();
	fulcrum_output::fulcrumDebug(_T("** %.3fs PTIoctl(%ld, %s, 0x%08X, 0x%08X)\n"), GetTimeSinceInit(), ChannelID, fulcrumDebug_ioctl(IoctlID).c_str(), pInput, pOutput);
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
			fulcrum_output::fulcrumDebug(_T("  %f Volts\n"), ((*(unsigned long*)pOutput)) / (float)1000);
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
			fulcrum_output::fulcrumDebug(_T("  %f Volts\n"), ((*(unsigned long*)pOutput)) / (float)1000);
		break;
	}

	fulcrum_printretval(retval);
	return retval;
}

// Error Reporting Commands and converter for error codes
long fulcrum_PassThruGetLastError(char* pErrorDescription)
{
	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

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
	// Ensure the module is running in static state and acquire a lock for it.
    AFX_MANAGE_STATE(AfxGetStaticModuleState());
	auto_lock lock; long retval;

	// Boot pipes if the need to be started up.
	if (CFulcrumShim::fulcrumPiper == NULL)
		CFulcrumShim::StartupPipes();

	// pErrorDescription returns the text description for an error detected
	// during the last function call (EXCEPT PassThruGetLastError). This
	// function should not modify the last internal error

	fulcrum_output::fulcrumDebug(_T("** %.3fs PTGetLastError(0x%08X)\n"), GetTimeSinceInit(), pErrorDescription);
	if (pErrorDescription == NULL) fulcrum_output::fulcrumDebug(_T("%  pErrorDescription is NULL\n"));

	retval = fulcrum_PassThruGetLastError(pErrorDescription);
	if (pErrorDescription != NULL)
	{
#ifdef UNICODE
		CStringW cstrErrorDescriptionW(pErrorDescription);
		fulcrum_output::fulcrumDebug(_T("  %s\n"), (LPCWSTR)cstrErrorDescriptionW);
#else
		fulcrum_output::fulcrumDebug(_T("  %s\n"), pErrorDescription);
#endif
	}

	// Log the return value for this function without using dbg_printretval().
	// Even if an error occured inside this function, the error text was not
	// updated to describe the error.
	fulcrum_output::fulcrumDebug(_T("  %.3fs %s\n"), GetTimeSinceInit(), fulcrumDebug_return(retval).c_str());
	return retval;
}