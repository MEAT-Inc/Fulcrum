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

#pragma once

// Standard Imports
#include "afxwin.h"
#include "afxcmn.h"
#include <set>
#include <string>

// Fulcrum Resource Imports
#include "resource.h"
#include "fulcrum_loader.h"

// SelectionBox dialog
class CSelectionBox : public CDialog
{
	DECLARE_DYNAMIC(CSelectionBox)

public:
	CSelectionBox(std::set<cPassThruInfo>& connectedList, CWnd* pParent = NULL);   // standard constructor
	virtual ~CSelectionBox();

// Dialog Data
	enum { IDD = IDD_DIALOG1 };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()

private:
	// Reference to the connectedList. The caller has already scanned for devices and
	// determined that it is worth popping-up a dialog box to select from these
	std::set<cPassThruInfo>& connectedList;

	cPassThruInfo * sel;
	CString cstrDebugFile;

	CListCtrl m_listview;
	CEdit m_logfilename;
	CStatic m_detailtext;
	CButton m_button_ok;
	CButton m_button_config;

	void DoPopulateRegistryListbox();

public:

	virtual BOOL OnInitDialog();
	void OnBnClickedOk();
	afx_msg void OnLvnItemchangedList1(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnHdnItemdblclickList1(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnBnClickedConfig();
	afx_msg void OnBnClickedBrowse();

	cPassThruInfo * GetSelectedPassThru();
	CString GetDebugFilename();
};
