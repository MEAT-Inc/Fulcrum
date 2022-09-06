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

// FulcrumShim.h : main header file for the FulcrumShim DLL
#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

// Standard Imports
#include <thread>   

// Fulcrum Resource Imports
#include "resource.h"	
#include "fulcrum_jpipe.h"

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
		static CString SetupDebugLogFile();
		static bool PipesConnecting;
		static fulcrum_jpipe* fulcrumPiper;
		
	// Overrides for starting
    public: 
		DECLARE_MESSAGE_MAP()
		virtual BOOL InitInstance();		
};
