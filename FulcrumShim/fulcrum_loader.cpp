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
#include "config.h"
#include <afxmt.h>
#include <fstream>
#include <string>
#include <streambuf>
#include <iostream>
#include <sstream>
#include <vector>

// Fulcrum Resource Imports
#include "SelectionBox.h"
#include "fulcrum_j2534.h"
#include "fulcrum_debug.h"
#include "fulcrum_loader.h"
#include "fulcrum_output.h"
#include "FulcrumShim.h"

// Using callout
using namespace std;

// Pointers to J2534 API functions in the loaded library
PTOPEN _PassThruOpen = 0;
PTCLOSE _PassThruClose = 0;
PTGETNEXTCARDAQ _PassThruGetNextCarDAQ = 0;
PTREADDETAILS _PassThruReadDetails = 0;
PTCONNECT _PassThruConnect = 0;
PTDISCONNECT _PassThruDisconnect = 0;
PTREADMSGS _PassThruReadMsgs = 0;
PTWRITEMSGS _PassThruWriteMsgs = 0;
PTSTARTPERIODICMSG _PassThruStartPeriodicMsg = 0;
PTSTOPPERIODICMSG _PassThruStopPeriodicMsg = 0;
PTSTARTMSGFILTER _PassThruStartMsgFilter = 0;
PTSTOPMSGFILTER _PassThruStopMsgFilter = 0;
PTSETPROGRAMMINGVOLTAGE _PassThruSetProgrammingVoltage = 0;
PTREADVERSION _PassThruReadVersion = 0;
PTGETLASTERROR _PassThruGetLastError = 0;
PTIOCTL _PassThruIoctl = 0;

static HINSTANCE hDLL = NULL;

static bool fLibLoaded = false;
static LARGE_INTEGER ticksPerSecond;
static LARGE_INTEGER tick;
static CRITICAL_SECTION mAutoLock;

// Vista-forward has a great function InitOnceExecuteOnce() to thread-safe execute
// a callback exactly once, but we want to support Windows XP. Instead we'll guard
// with a globally initialized CCriticalSection.
static CCriticalSection CritSectionPerformanceCounter;
static bool fPerformanceCounterInitialized = false;
static CCriticalSection CritSectionAutoLock;
static bool fAutoLockInitialized = false;

static void EnumPassThruInterfaces(std::set<cPassThruInfo> &registryList);

auto_lock::auto_lock()
{
	// ONCE -- the first time somebody creates an autolock we need to initialize the mutex
	CritSectionAutoLock.Lock();
	if (!fAutoLockInitialized) {
		InitializeCriticalSection(&mAutoLock);
		fAutoLockInitialized = true;
	}

	// Unlock auto selected object
	CritSectionAutoLock.Unlock();

	// Try and reset the lock state. Fail out if this fails.
	if (TryEnterCriticalSection(&mAutoLock)) return;
	fulcrum_output::fulcrumDebug(_T("Multi-threading error"));
	EnterCriticalSection(&mAutoLock);
}

auto_lock::~auto_lock() { LeaveCriticalSection(&mAutoLock); }

// Find all J2534 v04.04 interfaces listed in the registry
static void EnumPassThruInterfaces(std::set<cPassThruInfo>& registryList)
{
	// Values for reg keys and info for this method.
	HKEY softwareKey, reg0404Key;
	HKEY wowNodeKey, reg0500Key = NULL;
	HKEY VendorKey_v0404, VendorKey_v0500;

	// Other information about key entries as we loop them
	FILETIME FTime_v0404, FTime_v0500;
	long hKey2RetVal_v0404, hKey2RetVal_v0500;
	DWORD VendorIndex_v0404, VendorIndex_v0500;
	DWORD KeyType_v0404, KeyType_v0500;
	DWORD KeySize_v0404, KeySize_v0500;

	// Clear out/init our return list.
	registryList.clear();

	// Open the software key and the WOW6432Node Key objects here. If this fails, return out.
	if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("Software"), 0, KEY_READ, &softwareKey) != ERROR_SUCCESS) return;

	// Store values for the 04.04 DLLs first then try and pull out our v05.00 DLLs.
	RegOpenKeyEx(softwareKey, _T("PassThruSupport.04.04"), 0, KEY_READ, &reg0404Key);
	if (RegOpenKeyEx(softwareKey, _T("WOW6432Node"), 0, KEY_READ, &wowNodeKey) == ERROR_SUCCESS) {
		RegOpenKeyEx(wowNodeKey, _T("PassThruSupport.05.00"), 0, KEY_READ, &reg0500Key);
		RegCloseKey(wowNodeKey);
	}

	// Close the software key entry now.
	RegCloseKey(softwareKey);

	// ------------------------------------------------------------------------------------------------

	// Determine the maximum subkey length for HKLM/Software/PassThruSupport.04.04/*
	VendorIndex_v0404 = 0;
	DWORD lMaxSubKeyLen_v0404;
	RegQueryInfoKey(reg0404Key, NULL, NULL, NULL, NULL, &lMaxSubKeyLen_v0404, NULL, NULL, NULL, NULL, NULL, NULL);
	TCHAR * KeyValue_v0404 = new TCHAR[lMaxSubKeyLen_v0404+1];

	// Iterate through HKLM/Software/PassThruSupport.04.04/*
	do
	{
		// Get the name of HKLM/Software/PassThruSupport.04.04/VendorDevice[i]
		KeySize_v0404 = lMaxSubKeyLen_v0404+1;
		hKey2RetVal_v0404 = RegEnumKeyEx(reg0404Key, VendorIndex_v0404++, KeyValue_v0404, &KeySize_v0404, NULL, NULL, NULL, &FTime_v0404);
		if (hKey2RetVal_v0404 != ERROR_SUCCESS) { continue; }

		// Open HKLM/Software/PassThruSupport.04.04/Vendor[i]
		if (RegOpenKeyEx(reg0404Key, KeyValue_v0404, 0, KEY_READ, &VendorKey_v0404) == ERROR_SUCCESS)
		{
			// Return value state and strings for DLL information
			LSTATUS retval;
			tstring strVendor, strName, strFunctionLibrary, strConfigApplication;

			// Determine the maximum value length for HKLM/Software/PassThruSupport.04.04/VendorDevice[i]/*
			DWORD lMaxValueLen;
			retval = RegQueryInfoKey(VendorKey_v0404, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, &lMaxValueLen, NULL, NULL);

			// Allocate a buffer large enough to hold that name
			TCHAR * KeyValue_v0404 = new TCHAR[lMaxValueLen+1];

			// Query HKLM/Software/PassThruSupport.04.04/VendorDevice[i]/Vendor
			KeySize_v0404 = lMaxValueLen+1;
			if (RegQueryValueEx(VendorKey_v0404, _T("Vendor"), 0, &KeyType_v0404, (LPBYTE)KeyValue_v0404, &KeySize_v0404) == ERROR_SUCCESS)
				strVendor = KeyValue_v0404;

			// Query HKLM/Software/PassThruSupport.04.04/VendorDevice[i]/Name
			KeySize_v0404 = lMaxValueLen+1;
			if (RegQueryValueEx(VendorKey_v0404, _T("Name"), 0, &KeyType_v0404, (LPBYTE)KeyValue_v0404, &KeySize_v0404) == ERROR_SUCCESS)
				strName = KeyValue_v0404; strName += L" (v04.04)";

			// Read HKLM/Software/PassThruSupport.04.04/VendorDevice[i]/FunctionLibrary
			KeySize_v0404 = lMaxValueLen+1;
			if (RegQueryValueEx(VendorKey_v0404, _T("FunctionLibrary"), 0, &KeyType_v0404, (LPBYTE)KeyValue_v0404, &KeySize_v0404) == ERROR_SUCCESS)
				strFunctionLibrary = KeyValue_v0404;

			// Read HKLM/Software/PassThruSupport.04.04/VendorDevice[i]/ConfigApplication
			KeySize_v0404 = lMaxValueLen+1;
			if (RegQueryValueEx(VendorKey_v0404, _T("ConfigApplication"), 0, &KeyType_v0404, (LPBYTE)KeyValue_v0404, &KeySize_v0404) == ERROR_SUCCESS)
				strConfigApplication = KeyValue_v0404;

			// Clsoe our key, clear the memory allocation for it.
			RegCloseKey(VendorKey_v0404); delete KeyValue_v0404;

			// If everything was successful then add it to the list
			cPassThruInfo registryEntry(strVendor, strName, strFunctionLibrary, strConfigApplication);
			registryList.insert(registryEntry);
		}
	} while (hKey2RetVal_v0404 == ERROR_SUCCESS);
	RegCloseKey(reg0404Key);

	// ------------------------------------------------------------------------------------------------

	// Stop here if this key is null
	if (reg0500Key == NULL) return;

	// Determine the maximum subkey length for HKLM/Software/WOW6432Node/PassThruSupport.05.00/*
	VendorIndex_v0500 = 0;
	DWORD lMaxSubKeyLen_v0500;
	RegQueryInfoKey(reg0500Key, NULL, NULL, NULL, NULL, &lMaxSubKeyLen_v0500, NULL, NULL, NULL, NULL, NULL, NULL);
	TCHAR* KeyValue_v0500 = new TCHAR[lMaxSubKeyLen_v0500 + 1];

	// Iterate through HKLM/Software/WOW6432Node/PassThruSupport.05.00/*
	do
	{
		// Get the name of HKLM/Software/WOW6432Node/PassThruSupport.05.00/VendorDevice[i]
		KeySize_v0500 = lMaxSubKeyLen_v0500 + 1;
		hKey2RetVal_v0500 = RegEnumKeyEx(reg0500Key, VendorIndex_v0500++, KeyValue_v0500, &KeySize_v0500, NULL, NULL, NULL, &FTime_v0500);
		if (hKey2RetVal_v0500 != ERROR_SUCCESS) { continue; }

		// HKLM/Software/WOW6432Node/PassThruSupport.05.00/Vendor[i]
		if (RegOpenKeyEx(reg0500Key, KeyValue_v0500, 0, KEY_READ, &VendorKey_v0500) == ERROR_SUCCESS)
		{
			// Return value state and strings for DLL information
			LSTATUS retval;
			tstring strVendor, strName, strFunctionLibrary, strConfigApplication;

			// Determine the maximum value length for HKLM/Software/WOW6432Node/PassThruSupport.05.00/VendorDevice[i]/*
			DWORD lMaxValueLen;
			retval = RegQueryInfoKey(VendorKey_v0500, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, &lMaxValueLen, NULL, NULL);

			// Allocate a buffer large enough to hold that name
			TCHAR* KeyValue_v0500 = new TCHAR[lMaxValueLen + 1];

			// Query HKLM/Software/WOW6432Node/PassThruSupport.05.00/VendorDevice[i]/Vendor
			KeySize_v0500 = lMaxValueLen + 1;
			if (RegQueryValueEx(VendorKey_v0500, _T("Vendor"), 0, &KeyType_v0500, (LPBYTE)KeyValue_v0500, &KeySize_v0500) == ERROR_SUCCESS)
				strVendor = KeyValue_v0500;

			// Query HKLM/Software/WOW6432Node/PassThruSupport.05.00/VendorDevice[i]/Name
			KeySize_v0500 = lMaxValueLen + 1;
			if (RegQueryValueEx(VendorKey_v0500, _T("Name"), 0, &KeyType_v0500, (LPBYTE)KeyValue_v0500, &KeySize_v0500) == ERROR_SUCCESS)
				strName = KeyValue_v0500; strName += L" (v05.00)";

			// Read HKLM/Software/WOW6432Node/PassThruSupport.05.00/VendorDevice[i]/FunctionLibrary
			KeySize_v0500 = lMaxValueLen + 1;
			if (RegQueryValueEx(VendorKey_v0500, _T("FunctionLibrary"), 0, &KeyType_v0500, (LPBYTE)KeyValue_v0500, &KeySize_v0500) == ERROR_SUCCESS)
				strFunctionLibrary = KeyValue_v0500;

			// Read HKLM/Software/WOW6432Node/PassThruSupport.05.00/VendorDevice[i]/ConfigApplication
			KeySize_v0500 = lMaxValueLen + 1;
			if (RegQueryValueEx(VendorKey_v0500, _T("ConfigApplication"), 0, &KeyType_v0500, (LPBYTE)KeyValue_v0500, &KeySize_v0500) == ERROR_SUCCESS)
				strConfigApplication = KeyValue_v0500;

			// Clsoe our key, clear the memory allocation for it.
			RegCloseKey(VendorKey_v0500); delete KeyValue_v0500;

			// If everything was successful then add it to the list
			cPassThruInfo registryEntry(strVendor, strName, strFunctionLibrary, strConfigApplication);
			registryList.insert(registryEntry);
		}
	} while (hKey2RetVal_v0500 == ERROR_SUCCESS);
	RegCloseKey(reg0500Key);
}

double GetTimeSinceInit()
{
	LARGE_INTEGER tock;
    double time;

	// ONCE -- the first time somebody gets a timestamp set the timer to 0.000s
	CritSectionPerformanceCounter.Lock();
	if (fPerformanceCounterInitialized) CritSectionPerformanceCounter.Unlock();
	else
	{
		QueryPerformanceFrequency(&ticksPerSecond);
		QueryPerformanceCounter(&tick);
		fPerformanceCounterInitialized = true;
	}

	// Now find the time value.
	QueryPerformanceCounter(&tock);
	time = (double)(tock.QuadPart-tick.QuadPart)/(double)ticksPerSecond.QuadPart;
	return time;
}

bool fulcrum_checkAndAutoload(void)
{
	// We're OK if a library is loaded
	if (fLibLoaded)
		return true;

	// Read the JSON Configuration file out of the FulcrumInjector Application
#if _DEBUG
	TCHAR szPath[MAX_PATH]; CString dll_config_path;
	SHGetFolderPath(NULL, CSIDL_PROFILE, NULL, 0, szPath);
	dll_config_path.Format(_T("%s\\source\\repos\\MEAT-Inc\\FulcrumShim\\FulcrumInjector\\bin\\Debug\\FulcrumResources\\FulcrumShimDLLConfig.txt"), szPath);
#else 
	TCHAR szPath[MAX_PATH]; CString dll_config_path;
	SHGetFolderPath(NULL, CSIDL_PROGRAM_FILESX86, NULL, 0, szPath);
	dll_config_path.Format(_T("%s\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumResources\\FulcrumShimDLLConfig.txt"), szPath);
#endif

	// Read in file contents here.
	std::ifstream config_file_stream(dll_config_path);
	std::string config_file_content((std::istreambuf_iterator<char>(config_file_stream)), std::istreambuf_iterator<char>());

	// Split contents out into line values. Store settings as needed.
	vector<string> tokens; size_t prev = 0, pos = 0;
	do
	{
		// Find our split content value locations here.
		pos = config_file_content.find("|", prev);
		if (pos == string::npos) pos = config_file_content.length();
		string token = config_file_content.substr(prev, pos - prev);
		if (!token.empty()) tokens.push_back(token); prev = pos + 1;
	} while (pos < config_file_content.length() && prev < config_file_content.length());

	// Now using our built values, we can setup some settings
	if (tokens[1] == "False") return fulcrum_loadLibrary((LPCTSTR)tokens[2].c_str());
	else
	{
		// Check the registry for J2534 interfaces
		std::set<cPassThruInfo> interfaceList;
		EnumPassThruInterfaces(interfaceList);
		if (interfaceList.size() == 0) {
			// No interfaces listed in the registry? Failure!
			return false;
		}
		else
		{
			// Multiple interfaces? Popup a selection box!
			INT_PTR retval;
			CSelectionBox Dlg(interfaceList);

			retval = Dlg.DoModal();
			if (retval == IDCANCEL) { return false; }
			cPassThruInfo* tmp = Dlg.GetSelectedPassThru();

			bool fSuccess;
			fSuccess = fulcrum_loadLibrary(tmp->FunctionLibrary.c_str());
			if (fSuccess) fLibLoaded = true;
			else
			{
				fulcrum_setInternalError(_T("Failed to open '%s'"), tmp->FunctionLibrary.c_str());
				fulcrum_printretval(ERR_FAILED);
				return false;
			}

			// The user specified a debug output file in the dialog. Write any buffered text to this file
			// and start using it from now on
			fulcrum_output::writeNewLogFile(Dlg.GetDebugFilename(), true);
			return true;
		}
	}
}

bool fulcrum_loadLibrary(LPCTSTR szDLL)
{
	// Can't load a library if the string is NULL
	if (szDLL == NULL) return false;

	// Can't load a library if there's one currently loaded
	if (fLibLoaded)	return false;

	hDLL = LoadLibrary(szDLL);
	if (hDLL == NULL)
	{
		// Try to get the error text
		// Set the internal error text based on the win32 message
		return false;
	}

	// Set loaded to true. 
	fLibLoaded = true;

	// Find our method locations via pointers inside the other DLLs
	_PassThruOpen = (PTOPEN)GetProcAddress(hDLL, "PassThruOpen");
	_PassThruClose = (PTCLOSE)GetProcAddress(hDLL, "PassThruClose");
	_PassThruGetNextCarDAQ = (PTGETNEXTCARDAQ)GetProcAddress(hDLL, "PassThruGetNextCarDAQ");
	_PassThruReadDetails = (PTREADDETAILS)GetProcAddress(hDLL, "PassThruReadDetails");
	_PassThruConnect = (PTCONNECT)GetProcAddress(hDLL, "PassThruConnect");
	_PassThruDisconnect = (PTDISCONNECT)GetProcAddress(hDLL, "PassThruDisconnect");
	_PassThruReadMsgs = (PTREADMSGS)GetProcAddress(hDLL, "PassThruReadMsgs");
	_PassThruWriteMsgs = (PTWRITEMSGS)GetProcAddress(hDLL, "PassThruWriteMsgs");
	_PassThruStartPeriodicMsg = (PTSTARTPERIODICMSG)GetProcAddress(hDLL, "PassThruStartPeriodicMsg");
	_PassThruStopPeriodicMsg = (PTSTOPPERIODICMSG)GetProcAddress(hDLL, "PassThruStopPeriodicMsg");
	_PassThruStartMsgFilter = (PTSTARTMSGFILTER)GetProcAddress(hDLL, "PassThruStartMsgFilter");
	_PassThruStopMsgFilter = (PTSTOPMSGFILTER)GetProcAddress(hDLL, "PassThruStopMsgFilter");
	_PassThruSetProgrammingVoltage = (PTSETPROGRAMMINGVOLTAGE)GetProcAddress(hDLL, "PassThruSetProgrammingVoltage");
	_PassThruReadVersion = (PTREADVERSION)GetProcAddress(hDLL, "PassThruReadVersion");
	_PassThruGetLastError = (PTGETLASTERROR)GetProcAddress(hDLL, "PassThruGetLastError");
	_PassThruIoctl = (PTIOCTL)GetProcAddress(hDLL, "PassThruIoctl");

	// Return passed.
	return true;
}

void fulcrum_unloadLibrary()
{
	// Can't unload a library if there's nothing loaded
	if (!fLibLoaded) return;

	// Set loaded to false
	fLibLoaded = false;

	// Invalidate the function pointers
	_PassThruOpen = NULL;
	_PassThruClose = NULL;
	_PassThruGetNextCarDAQ = NULL;
	_PassThruReadDetails = NULL;
	_PassThruConnect = NULL;
	_PassThruDisconnect = NULL;
	_PassThruReadMsgs = NULL;
	_PassThruWriteMsgs = NULL;
	_PassThruStartPeriodicMsg = NULL;
	_PassThruStopPeriodicMsg = NULL;
	_PassThruStartMsgFilter = NULL;
	_PassThruStopMsgFilter = NULL;
	_PassThruSetProgrammingVoltage = NULL;
	_PassThruReadVersion = NULL;
	_PassThruGetLastError = NULL;
	_PassThruIoctl = NULL;

	// Set results out and free the lib.
	BOOL fSuccess;
	fSuccess = FreeLibrary(hDLL);
}

bool fulcrum_hasLibraryLoaded() { return fLibLoaded; }