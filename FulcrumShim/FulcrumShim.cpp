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

// Fulcrum_shim.cpp : Defines the initialization routines for the DLL.
#include "stdafx.h"
#include "FulcrumShim.h"
#include "fulcrum_jpipe.h"
#include "SelectionBox.h"
#include "fulcrum_output.h"


#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
// TODO: If this DLL is dynamically linked against the MFC DLLs,
//		any functions exported from this DLL which call into
//		MFC must have the AFX_MANAGE_STATE macro added at the
//		very beginning of the function.
//
//		For example:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// normal function body here
//		}
//
//		It is very important that this macro appear in each
//		function, prior to any calls into MFC.  This means that
//		it must appear as the first statement within the
//		function, even before any object variable declarations
//		as their constructors may generate calls into the MFC
//		DLL.
//
//		Please see MFC Technical Notes 33 and 58 for additional
//		details.
//

// Message controls for new instance
BEGIN_MESSAGE_MAP(CFulcrumShim, CWinApp)
END_MESSAGE_MAP()

// CTOR init for fulcrum runs
CFulcrumShim::CFulcrumShim() { }	// Builds a new fulcrum app
CFulcrumShim fulcrum_instance;		// Standalone fulcrum instance

// ---------------------------------------------------------------------------------------------------------------------------------

// Init our static members here
bool CFulcrumShim::PipesConnecting;			// Bool to track connection state
fulcrum_jpipe* CFulcrumShim::fulcrumPiper;	// Pipe injection sending logic helper

// ---------------------------------------------------------------------------------------------------------------------------------

// Init override for app startup
BOOL CFulcrumShim::InitInstance()
{
	// Build instance object
	CWinApp::InitInstance();
	
	// THREAD BASED STARTUP
	// std::thread([this] { CFulcrumShim::StartupPipes(); });

	// Build our pipes and return passed
		// Build config app path value here and run the injector application
#if _DEBUG
	TCHAR szPath[MAX_PATH]; CString ConfigAppPath;
	SHGetFolderPath(NULL, CSIDL_PROFILE, NULL, 0, szPath);
	ConfigAppPath.Format(_T("%s\\source\\repos\\MEAT-Inc\\FulcrumShim\\FulcrumInjector\\bin\\Debug\\FulcrumInjector.exe"), szPath);
#else 
	TCHAR szPath[MAX_PATH]; CString ConfigAppPath;
	SHGetFolderPath(NULL, CSIDL_PROGRAM_FILESX86, NULL, 0, szPath);
	ConfigAppPath.Format(_T("%s\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumInjector.exe"), szPath);
#endif

	// Run the process here by booting a new instance. Once running connect the input pipe.
	STARTUPINFO StartupInfos; PROCESS_INFORMATION ProcessInfos;
	ZeroMemory(&StartupInfos, sizeof(StartupInfos));
	StartupInfos.cb = sizeof(StartupInfos);
	ZeroMemory(&ProcessInfos, sizeof(ProcessInfos));
	::CreateProcess(ConfigAppPath.GetString(), NULL, NULL, NULL, FALSE, 0, NULL, NULL, &StartupInfos, &ProcessInfos);
	CloseHandle(ProcessInfos.hProcess);	CloseHandle(ProcessInfos.hThread);
	return TRUE;
}

// Build a new init method sequence.
void CFulcrumShim::StartupPipes()
{
	// Check if pipes need to be configured
	if (CFulcrumShim::fulcrumPiper == NULL) CFulcrumShim::fulcrumPiper = new fulcrum_jpipe();

	// If they're connected, drop out and stop working on connection routines
	if (CFulcrumShim::fulcrumPiper->PipesConnected()) { return; }
	if (PipesConnecting) 
	{
		// Join the thread to finish setup and return
		fulcrum_output::fulcrumDebug(_T("%.3fs    WARNING: Pipes were already connecting!\n", GetTimeSinceInit()));
		fulcrum_output::fulcrumDebug(_T("%.3fs    Forcing execution of the setup thread to synchronize now...\n", GetTimeSinceInit()));
		return;
	}

	// Set connecting to true
	PipesConnecting = true;

	// Connect our pipe instances for the reader and writer objects now
	fulcrum_output::fulcrumDebug(_T("------------------------------------------------------------------------------------\n"));
	bool LoadedPipeInput = CFulcrumShim::fulcrumPiper->ConnectInputPipe();
	bool LoadedPipeOutput = CFulcrumShim::fulcrumPiper->ConnectOutputPipe();

	// Log heading information so we see this on boot
	fulcrum_output::fulcrumDebug(_T("------------------------------------------------------------------------------------\n"));
	fulcrum_output::fulcrumDebug(_T("%.3fs    FulcrumShim DLL - Sniffin CAN, And Crushing Neo's Morale Since 2021\n"), GetTimeSinceInit());

	// Now see if we're loaded correctly.
	LoadedPipeInput && LoadedPipeOutput;
	if (!LoadedPipeInput || !LoadedPipeOutput) fulcrum_output::fulcrumDebug(_T("%.3fs    Failed to boot new pipe instances for our FulcrumShim Server!\n", GetTimeSinceInit()));
	else 
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    Booted new pipe instances correctly!\n", GetTimeSinceInit()));
		fulcrum_output::fulcrumDebug(_T("%.3fs    FulcrumInjector should now be running in the background\n", GetTimeSinceInit()));
	}

	// Log closing line output
	fulcrum_output::fulcrumDebug(_T("------------------------------------------------------------------------------------\n"));
	PipesConnecting = false;
}
void CFulcrumShim::ShutdownPipes()
{
	// Run the shutdown method
	if (CFulcrumShim::fulcrumPiper->PipesConnected())
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    Calling pipe shutdown methods now...\n", GetTimeSinceInit()));
		CFulcrumShim::fulcrumPiper->ShutdownPipes();
		fulcrum_output::fulcrumDebug(_T("%.3fs    Pipe instances have been released OK!\n", GetTimeSinceInit()));
	}
	else { fulcrum_output::fulcrumDebug(_T("%.3fs    Pipe instances were already closed!\n", GetTimeSinceInit())); }
}
