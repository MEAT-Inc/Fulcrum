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
#include <set>
#include <string>

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

bool shim_checkAndAutoload(void);
bool shim_loadLibrary(LPCTSTR szDLL);
void shim_unloadLibrary();
bool shim_hasLibraryLoaded();

extern PTOPEN _PassThruOpen;
extern PTCLOSE _PassThruClose;
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
