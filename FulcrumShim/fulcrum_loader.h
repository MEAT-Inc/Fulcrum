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
#include <set>
#include <string>
#include <wtypes.h>

// Fulcrum Resource Imports
#include "fulcrum_j2534.h"

#ifdef _UNICODE
typedef std::wstring tstring;
#else
typedef std::string tstring;
#endif

class cPassThruInfo
{
public:
	cPassThruInfo(tstring Vendor, tstring Name, tstring FunctionLibrary, tstring ConfigApplication)
		: Vendor(Vendor), Name(Name), FunctionLibrary(FunctionLibrary), ConfigApplication(ConfigApplication) {};

	bool operator<(const cPassThruInfo &rhs) const
	{
		return Name < rhs.Name;
	}

	tstring Vendor;
	tstring Name;
	tstring FunctionLibrary;
	tstring ConfigApplication;
	// Should also record the supported protocols
};

class auto_lock
{
public:
    auto_lock();
    ~auto_lock();
};

double GetTimeSinceInit();

bool fulcrum_checkAndAutoload(void);
bool fulcrum_loadLibrary(LPCTSTR szDLL);
void fulcrum_unloadLibrary();
bool fulcrum_hasLibraryLoaded();

extern PTOPEN _PassThruOpen;
extern PTCLOSE _PassThruClose;
extern PTGETNEXTCARDAQ _PassThruGetNextCarDAQ;
extern PTREADDETAILS _PassThruReadDetails;
extern PTCONNECT _PassThruConnect;
extern PTDISCONNECT _PassThruDisconnect;
extern PTREADMSGS _PassThruReadMsgs;
extern PTWRITEMSGS _PassThruWriteMsgs;
extern PTSTARTPERIODICMSG _PassThruStartPeriodicMsg;
extern PTSTOPPERIODICMSG _PassThruStopPeriodicMsg;
extern PTSTARTMSGFILTER _PassThruStartMsgFilter;
extern PTSTOPMSGFILTER _PassThruStopMsgFilter;
extern PTSETPROGRAMMINGVOLTAGE _PassThruSetProgrammingVoltage;
extern PTREADVERSION _PassThruReadVersion;
extern PTGETLASTERROR _PassThruGetLastError;
extern PTIOCTL _PassThruIoctl;
