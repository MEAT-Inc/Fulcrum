// SelectionBox.cpp : implementation file

#include "stdafx.h"

#include <set>
#include <sstream>
#include <string>
#include <tchar.h>

#include "FulcrumShim.h"
#include "SelectionBox.h"
#include "fulcrum_output.h"

using namespace std;

// SelectionBox dialog
IMPLEMENT_DYNAMIC(CSelectionBox, CDialog)

// Open and close events
CSelectionBox::CSelectionBox(std::set<cPassThruInfo>& connectedList, CWnd* pParent /*=NULL*/)
	: CDialog(CSelectionBox::IDD, pParent), connectedList(connectedList), sel(NULL) { }
CSelectionBox::~CSelectionBox() { }

// On information registered into here
void CSelectionBox::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDOK, m_button_ok);
	DDX_Control(pDX, IDC_LIST1, m_listview);
	DDX_Control(pDX, IDC_J2534REGINFO, m_detailtext);
	DDX_Control(pDX, IDC_BUTTON1, m_button_config);
	DDX_Control(pDX, IDC_EDIT1, m_logfilename);
}

// Open event and init for new content.
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
	// Show window output
	CDialog::OnInitDialog();
	ShowWindow(SW_HIDE);

	// Find the Progfiles folder
	TCHAR szPath[MAX_PATH];
	SHGetFolderPath(NULL, CSIDL_PROGRAM_FILESX86, NULL, 0, szPath);

	// Find the current time value
	SYSTEMTIME LocalTime;
	GetLocalTime(&LocalTime);

	// Build the log filder dir.
	CString logDir;
	logDir.Format(_T("%s\\MEAT Inc\\FulcrumShim\\FulcrumLogs"), szPath);
	if (CreateDirectory(logDir, NULL) || ERROR_ALREADY_EXISTS == GetLastError()) 
		dtDebug(_T("%.3fs    Log file folder exists. Skipping creation for this directory!\n"), GetTimeSinceInit());
	else 
		dtDebug(_T("%.3fs    Built new folder for our output logs!\n"), GetTimeSinceInit());


	// Build the log file path using the log dir above
	CString cstrPath;
	cstrPath.Format(_T("%s\\MEAT Inc\\FulcrumShim\\FulcrumLogs\\FulcrumShim_Logging_%02d%02d%04d-%02d%02d%02d.txt"),
		szPath,
		LocalTime.wMonth,
		LocalTime.wDay,
		LocalTime.wYear,
		LocalTime.wHour,
		LocalTime.wMinute,
		LocalTime.wSecond
	);

	// Log new file name output.
	dtDebug(_T("%.3fs    Configured new log file correctly!\n"), GetTimeSinceInit());

	// Set information about the new output file
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
	// Build PT Info based on args
	cPassThruInfo * item1 = (cPassThruInfo *) lParam1;
	cPassThruInfo * item2 = (cPassThruInfo *) lParam2;

	// Check name values
	if (item1->Name < item2->Name) { return -1; }
	else if (item1->Name > item2->Name) { return 1; }
	else { return 0; }
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
	// Find current PT Device
	POSITION pos = m_listview.GetFirstSelectedItemPosition();
	if (pos == NULL) return;

	// Select it here
	int nItem = m_listview.GetNextSelectedItem(pos);
	DWORD_PTR item_data = m_listview.GetItemData(nItem);
	sel = (cPassThruInfo *) item_data;
	m_logfilename.GetWindowText(cstrDebugFile);

	// Boot the pipes and start the fulcrum injector
	CFulcrumShim* fulcrum_app = static_cast<CFulcrumShim*>(AfxGetApp());
	if (!fulcrum_app->pipesLoaded) { fulcrum_app->InitPipes(); }

	// Check if passed output. If so move on.
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
	if (pos == NULL) return;

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
