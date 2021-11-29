// fulcrum_shim.h : main header file for the fulcrum_shim DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// Cfulcrum_shimApp
// See fulcrum_shim.cpp for the implementation of this class
//

class Cfulcrum_shimApp : public CWinApp
{
public:
	Cfulcrum_shimApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
