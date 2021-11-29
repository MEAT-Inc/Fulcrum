// SelectionBox.cpp : implementation file
//

#include "stdafx.h"

#include <set>
#include <sstream>
#include <string>
#include <tchar.h>

#include "ptshim.h"
#include "SelectionBox.h"
using namespace std;

// SelectionBox dialog

IMPLEMENT_DYNAMIC(CSelectionBox, CDialog)

CSelectionBox::CSelectionBox(std::set<cPassThruInfo>& connectedList, CWnd* pParent /*=NULL*/)
	: CDialog(CSelectionBox::IDD, pParent), connectedList(connectedList), sel(NULL)
{

}

CSelectionBox::~CSelectionBox()
{
}

void CSelectionBox::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDOK, m_button_ok);
	DDX_Control(pDX, IDC_LIST1, m_listview);
	DDX_Control(pDX, IDC_J2534REGINFO, m_detailtext);
	DDX_Control(pDX, IDC_BUTTON1, m_button_config);
	DDX_Control(pDX, IDC_EDIT1, m_logfilename);
}


BEGIN_MESSAGE_MAP(CSelectionBox, CDialog)
	ON_BN_CLICKED(IDOK, &CSelectionBox::OnBnClickedOk)
	ON_NOTIFY(LVN_ITEMCHANGED, IDC_LIST1, &CSelectionBox::OnLvnItemchangedList1)
	ON_NOTIFY(HDN_ITEMDBLCLICK, 0, &CSelectionBox::OnHdnItemdblclickList1)
	ON_BN_CLICKED(IDC_BUTTON1, &CSelectionBox::OnBnClickedConfig)
	ON_BN_CLICKED(IDC_BUTTON2, &CSelectionBox::OnBnClickedBrowse)
END_MESSAGE_MAP()

// SelectionBox message handlers
BOOL CSelectionBox::OnInitDialog()
{
	CDialog::OnInitDialog();
	ShowWindow(SW_HIDE);

	TCHAR szPath[MAX_PATH];
	SHGetFolderPath(NULL, CSIDL_MYDOCUMENTS, NULL, 0, szPath);

	SYSTEMTIME LocalTime;
	GetLocalTime(&LocalTime);

	CString cstrPath;
	cstrPath.Format(_T("%s\\%s_%04d-%02d-%02d_%02d-%02d-%02d_%04d.txt"), szPath, _T("ShimDLL"), LocalTime.wYear,
		LocalTime.wMonth, LocalTime.wDay, LocalTime.wHour, LocalTime.wMinute, LocalTime.wSecond,
		LocalTime.wMilliseconds);

	m_logfilename.SetWindowText(cstrPath);

	DoPopulateRegistryListbox();

	ShowWindow(SW_SHOW);
	BringWindowToTop();

	// Return TRUE unless you set focus to a control
	return TRUE;
}

// Callback for sorting the omega ListCtrl. Use the data item to get the corresponding
// class and sort by name. This will make more sense later if it's modified to sort by
// serial number
static int CALLBACK CompareByName(LPARAM lParam1, LPARAM lParam2, LPARAM lParamSort)
{
	cPassThruInfo * item1 = (cPassThruInfo *) lParam1;
	cPassThruInfo * item2 = (cPassThruInfo *) lParam2;

	if (item1->Name < item2->Name)
	{
		return -1;
	}
	else if (item1->Name > item2->Name)
	{
		return 1;
	}
	else
	{
		return 0;
	}
}

void CSelectionBox::DoPopulateRegistryListbox()
{
	m_listview.DeleteAllItems();			// Clear listbox

	// Insert the omegas into the listbox
	std::set<cPassThruInfo>::iterator i;
	for (i = connectedList.begin(); i != connectedList.end(); i++)
	{
		int index;
		index = m_listview.InsertItem(0, i->Name.c_str());
		m_listview.SetItemData(index, (DWORD_PTR) &(*i));
	}

	// Sort the list by the criteria
	m_listview.SortItems(CompareByName, NULL);
}

void CSelectionBox::OnLvnItemchangedList1(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMLISTVIEW pNMLV = reinterpret_cast<LPNMLISTVIEW>(pNMHDR);
	int cursel;

	if (!(pNMLV->uOldState & LVIS_SELECTED) && (pNMLV->uNewState & LVIS_SELECTED))
	{
		// This is a transition from selected to unselected. I'm assuming it'll
		// happen after the previous item gets deselected

		// Determine what the user selected
		DWORD_PTR item_data;
		cursel = pNMLV->iItem;
		item_data = m_listview.GetItemData(cursel);

		cPassThruInfo * iPassThruInfo = (cPassThruInfo *) item_data;

		_WIN32_FILE_ATTRIBUTE_DATA FileInformation;
		BOOL fSuccess;

		// Enable the OK button if the FunctionLibrary is suitable for use
		fSuccess = GetFileAttributesEx(iPassThruInfo->FunctionLibrary.c_str(), GetFileExInfoStandard, &FileInformation);
		m_button_ok.EnableWindow(fSuccess);

		// Enable the Config button if the ConfigApplication is suitable for use
		fSuccess = GetFileAttributesEx(iPassThruInfo->ConfigApplication.c_str(), GetFileExInfoStandard, &FileInformation);
		m_button_config.EnableWindow(fSuccess);

		// Build a string and put in the details box
		std::basic_ostringstream<wchar_t> desc;
		desc << _T("Vendor: ") << iPassThruInfo->Vendor << endl;
		desc << _T("Name: ") << iPassThruInfo->Name << endl;
		desc << _T("FunctionLibrary: ") << iPassThruInfo->FunctionLibrary << endl;
		desc << _T("ConfigApplication: ") << iPassThruInfo->ConfigApplication << endl;
		m_detailtext.SetWindowText(desc.str().c_str());
	}
	else if ((pNMLV->uOldState & LVIS_SELECTED) && !(pNMLV->uNewState & LVIS_SELECTED))
	{
		// This is a transition from selected to unselected. I'm assuming that the deselect
		// will always come before the next selection.

		tstring szDetailHelp = _T("Select J2534 interface then click OK");
		m_button_ok.EnableWindow(FALSE);
		m_button_config.EnableWindow(FALSE);
		m_detailtext.SetWindowText(szDetailHelp.c_str());
	}

	*pResult = 0;
}

void CSelectionBox::OnBnClickedOk()
{
	POSITION pos = m_listview.GetFirstSelectedItemPosition();
	if (pos == NULL)
		return;

	int nItem = m_listview.GetNextSelectedItem(pos);

	DWORD_PTR item_data = m_listview.GetItemData(nItem);

	sel = (cPassThruInfo *) item_data;
	m_logfilename.GetWindowText(cstrDebugFile);

	// Return if you determine that the FunctionLibrary does not exist or
	// is unusable for some reason

	OnOK();
}

void CSelectionBox::OnHdnItemdblclickList1(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMHEADER phdr = reinterpret_cast<LPNMHEADER>(pNMHDR);

	OnBnClickedOk();

	*pResult = 0;
}

void CSelectionBox::OnBnClickedConfig()
{
	POSITION pos = m_listview.GetFirstSelectedItemPosition();
	if (pos == NULL)
		return;

	int nItem = m_listview.GetNextSelectedItem(pos);

	DWORD_PTR item_data = m_listview.GetItemData(nItem);
	cPassThruInfo * iPassThruInfo = (cPassThruInfo *) item_data;

    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );

	::CreateProcess(iPassThruInfo->ConfigApplication.c_str(), NULL, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
	WaitForSingleObject( pi.hProcess, INFINITE );

    // Close process and thread handles. 
    CloseHandle( pi.hProcess );
    CloseHandle( pi.hThread );
}

void CSelectionBox::OnBnClickedBrowse()
{
	CString cstrFileName(_T("filename.txt"));
	CString cstrFilter(_T("J2534 Logfile (*.txt)|*.txt|All Files (*.*)|*.*||"));

	m_logfilename.GetWindowText(cstrFileName);

	INT_PTR retval;
	CFileDialog Dlg(FALSE, _T(".txt"), cstrFileName, 0, cstrFilter, 0, 0, 1);
	retval = Dlg.DoModal();
	if (retval != IDCANCEL)
	{
		m_logfilename.SetWindowText(Dlg.GetFileName());
	}
}

cPassThruInfo * CSelectionBox::GetSelectedPassThru()
{
	return sel;
}

CString CSelectionBox::GetDebugFilename()
{
	return cstrDebugFile;
}
