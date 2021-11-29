// ptshim.h : main header file for the ptshim DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CptshimApp
// See ptshim.cpp for the implementation of this class
//

class CptshimApp : public CWinApp
{
public:
	CptshimApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
