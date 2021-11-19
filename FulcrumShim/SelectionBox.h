#pragma once
#include "afxwin.h"
#include "afxcmn.h"

#include "resource.h"
#include "shim_loader.h"
#include <set>
#include <string>

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
