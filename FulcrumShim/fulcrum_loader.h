#pragma once

#include "fulcrum_j2534.h"
#include <set>
#include <string>
#include <wtypes.h>

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
