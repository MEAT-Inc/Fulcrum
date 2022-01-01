// FulcrumShim.h : main header file for the Wombat DLL

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"	
#include "fulcrum_jpipe.h"
#include <thread>   

// CFulcrumDLL.h
// See FulcrumShim.cpp for the implementation of this class

class CFulcrumShim : public CWinApp 
{	
	// CTOR and base pipe configuration
    public:
		CFulcrumShim();

		// Pipe configuration
		static void StartupPipes();
		static void ShutdownPipes();
		static bool PipesConnecting;
		static fulcrum_jpipe* fulcrumPiper;
		
	// Overrides for starting
    public: 
		DECLARE_MESSAGE_MAP()
		virtual BOOL InitInstance();		
};
