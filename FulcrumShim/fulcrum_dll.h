// FulcrumDLL.h : main header file for the Wombat DLL

#pragma once

#ifndef __AFXWIN_H__
#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"			// main symbols
#include "fulcrum_jpipe.h"

// CFulcrumDLL.h
// See FulcrumDLL.cpp for the implementation of this class

class fulcrum_dll : public CWinApp {
    public:
		fulcrum_dll();
		BOOL ExitInstance();		

		void InitPipes();
		bool pipesLoaded;
		fulcrum_jpipe* fulcrumPiper;

	// Overrides
    public:
		virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
