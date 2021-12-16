// Fulcrum_shim.cpp : Defines the initialization routines for the DLL.
//

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

// Init override for app startup
BOOL CFulcrumShim::InitInstance()
{
	// Build instance and show the window
	CWinApp::InitInstance();
	return TRUE;
}

// Build a new init method sequence.
void CFulcrumShim::StartupPipes()
{
	// Check if pipes need to be configured
	if (pipesLoaded) { return; }

	// Build pipe server and store the state of them
	if (fulcrumPiper == nullptr) fulcrumPiper = new fulcrum_jpipe();
	fulcrum_output::fulcrumDebug(_T("%.3fs    Connecting output pipe now...\n", GetTimeSinceInit()));
	bool LoadedPipeOutput = fulcrumPiper->ConnectOutputPipe();

	// Build config app path value here and run the injector application
	TCHAR szPath[MAX_PATH]; CString ConfigAppPath;
	SHGetFolderPath(NULL, CSIDL_PROGRAM_FILESX86, NULL, 0, szPath);
	ConfigAppPath.Format(_T("%s\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumInjector.exe"), szPath);

	// Build config app path value here and run the injector application
	STARTUPINFO StartupInfos; PROCESS_INFORMATION ProcessInfos;
	ZeroMemory(&StartupInfos, sizeof(StartupInfos));
	StartupInfos.cb = sizeof(StartupInfos);
	ZeroMemory(&ProcessInfos, sizeof(ProcessInfos));

	// Run the process here by booting a new instance. Once running connect the input pipe.
	::CreateProcess(ConfigAppPath.GetString(), NULL, NULL, NULL, FALSE, 0, NULL, NULL, &StartupInfos, &ProcessInfos);
	fulcrum_output::fulcrumDebug(_T("%.3fs    Connecting input pipe now...\n", GetTimeSinceInit()));
	fulcrum_output::fulcrumDebug(_T("%.3fs    NOTE: Waiting 2000 miliseconds before this request is sent...\n", GetTimeSinceInit()));
	Sleep(2000); bool LoadedPipeInput = fulcrumPiper->ConnectInputPipe();

	// Now see if we're loaded correctly.
	pipesLoaded = LoadedPipeInput && LoadedPipeOutput;
	if (!pipesLoaded) fulcrum_output::fulcrumDebug(_T("%.3fs    Failed to boot new pipe instances for our FulcrumShim Server!\n", GetTimeSinceInit()));
	else 
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    Booted new pipe instances correctly!\n", GetTimeSinceInit()));
		fulcrum_output::fulcrumDebug(_T("%.3fs    FulcrumInjector should now be running in the background\n", GetTimeSinceInit()));
	}
}
void CFulcrumShim::ShutdownPipes()
{
	// Build pipe server and store the state of them
	if (fulcrumPiper == nullptr)
	{
		fulcrum_output::fulcrumDebug(_T("%.3fs    Building Piper to force shut down now...\n", GetTimeSinceInit()));
		fulcrumPiper = new fulcrum_jpipe();
	}

	// Run the shutdown method
	fulcrum_output::fulcrumDebug(_T("%.3fs    Calling pipe shutdown methods now...\n", GetTimeSinceInit()));
	fulcrumPiper->ShutdownPipe();
	fulcrum_output::fulcrumDebug(_T("%.3fs    Pipe instances have been released OK!\n", GetTimeSinceInit()));
}
