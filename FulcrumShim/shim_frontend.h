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

#pragma once

#include "j2534_v0404.h"

extern "C"
{
	long J2534_API PassThruOpen(void *pName, unsigned long *pDeviceID);
	long J2534_API PassThruClose(unsigned long DeviceID);
	long J2534_API PassThruConnect(unsigned long DeviceID, unsigned long ProtocolID, unsigned long Flags, unsigned long Baudrate, unsigned long *pChannelID);
	long J2534_API PassThruDisconnect(unsigned long ChannelID);
	long J2534_API PassThruReadMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout);
	long J2534_API PassThruWriteMsgs(unsigned long ChannelID, PASSTHRU_MSG *pMsg, unsigned long *pNumMsgs, unsigned long Timeout);
	long J2534_API PassThruStartPeriodicMsg(unsigned long ChannelID, PASSTHRU_MSG * pMsg, unsigned long *pMsgID, unsigned long TimeInterval);
	long J2534_API PassThruStopPeriodicMsg(unsigned long ChannelID, unsigned long MsgID);
	long J2534_API PassThruStartMsgFilter(unsigned long ChannelID, unsigned long FilterType, PASSTHRU_MSG *pMaskMsg, PASSTHRU_MSG *pPatternMsg, PASSTHRU_MSG *pFlowControlMsg, unsigned long *pFilterID);
	long J2534_API PassThruStopMsgFilter(unsigned long ChannelID, unsigned long FilterID);
	long J2534_API PassThruSetProgrammingVoltage(unsigned long DeviceID, unsigned long PinNumber, unsigned long Voltage);
	long J2534_API PassThruReadVersion(unsigned long DeviceID, char *pFirmwareVersion, char *pDllVersion, char *pApiVersion);
	long J2534_API PassThruGetLastError(char *pErrorDescription);
	long J2534_API PassThruIoctl(unsigned long ChannelID, unsigned long IoctlID, void *pInput, void *pOutput);

	long J2534_API PassThruLoadLibrary(char *szFunctionLibrary);
	long J2534_API PassThruWriteToLogA(char *szMsg);
	long J2534_API PassThruWriteToLogW(wchar_t *szMsg);
	long J2534_API PassThruSaveLog(char *szFilename);
	long J2534_API PassThruUnloadLibrary();
}

long shim_PassThruGetLastError(char *pErrorDescription);