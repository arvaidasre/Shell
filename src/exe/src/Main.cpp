
#include <System.h>
#include <Globals.h>
#include <Resource.h>
#include <dwmapi.h>

#include <vector>
#include <commctrl.h>
#include "Control.h"
#include <shlwapi.h>
#include <shlobj.h>
#include <shobjidl.h>
#include <winhttp.h>
#include <cctype>
#include <cstdio>
#include <cstring>
#include <cwchar>
#include <string>
#include <vector>
#include <Library/PlutoVGWrap.h>
#include <RegistryConfig.h>

//#pragma comment(lib, "mincore.lib")
#pragma comment(lib, "UxTheme.lib")
#pragma comment(lib, "Comctl32")
#pragma comment(lib, "dwmapi")
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "winhttp.lib")

#if defined(_M_ARM64)
	#pragma comment(lib, "plutosvg-arm64.lib")
//#elif defined(_M_ARM)
//	#pragma comment(lib, "plutosvg-arm.lib")
#elif defined(_M_X64)
	#pragma comment(lib, "plutosvg-x64.lib")
#else
	#pragma comment(lib, "plutosvg-x86.lib")
#endif

using namespace Nilesoft;
using namespace Nilesoft::Windows::Forms;
using namespace Nilesoft::Shell;

constexpr const wchar_t dll_name[] = L"shell.dll";

BOOL CALLBACK WndProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam);

#define ID_MAINWINDOW 100

// define id for close, minimize & demo buttons
#define ID_CLOSE 0x001
#define ID_MINIMIZE 0x002

#define ID_REG 0x003
#define ID_UNREG 0x004
#define ID_RESTART 0x005
#define ID_WEB 0x006
#define ID_EMAIL 0x008
#define ID_GITHUB 0x009
#define ID_DOCS 0x00d
#define ID_LANG 0x010
#define ID_OPENCFG 0x011
#define ID_BACKUPCFG 0x012
#define ID_UPDATE 0x013
#define ID_STARTMENU 0x014

/////////
#define SetWindowStyle(hwnd, style)	 ::SetWindowLongW((hwnd), GWL_STYLE, (style))
#define SetWindowExStyle(hwnd, style)	::SetWindowLongW((hwnd), GWL_EXSTYLE, (style))

#define GET_X_LPARAM(lp)    ((int)(short)LOWORD(lp))  // windowsx.h
#define GET_Y_LPARAM(lp)    ((int)(short)HIWORD(lp))  // windowsx.h

int __stdcall btn_on_paint(UI::Control *s, int, WPARAM, LPARAM);

LRESULT __stdcall WindowProc(HWND, UINT, WPARAM, LPARAM);

// Forward declarations for the manager helpers defined below.
static UI::Button *g_lang_button;
static void manager_refresh_lang_button();
static void manager_doctor();
static bool manager_create_start_shortcut();

bool is_elevated = false;

// Global Variables:
HINSTANCE _hInstance;    // current instance
Logger *_log;
HANDLE _fonthandle = nullptr;
HFONT _hfont_icon = nullptr;
HFONT _hfont_icon2 = nullptr;
HFONT g_font_ui = nullptr;
UINT _dpi = 96;
RECT rc_window{};
RECT rc_reg{};
RECT rc_unreg{};
POINT p{};
UI::Window *main_window = nullptr;
HBITMAP hbitmap_logo = nullptr;
//0xAA0000FF
COLORREF color_background = 0xffffff;

template<typename T = long>
T dpi(auto value) { return static_cast<T>((value * _dpi) / 96); }

string loadstring(UINT id, HMODULE hmodule = nullptr)
{
	string str(MAX_PATH);
	auto size = ::LoadStringW(hmodule, id, str.buffer(), MAX_PATH);
	if(size > MAX_PATH)
	{
		size = ::LoadStringW(hmodule, id, str.buffer(size + 1), size);
	}
	return str.release(size).move();
}


BOOL EnablePrivilege()
{
	BOOL result = FALSE;
	HANDLE hToken {};
	if(::OpenProcessToken(::GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
	{
		LUID luid {};
		TOKEN_PRIVILEGES tp {};
		if(::LookupPrivilegeValueW(nullptr, SE_TAKE_OWNERSHIP_NAME, &luid))
		{
			tp.PrivilegeCount = 1;
			tp.Privileges[0].Luid = luid;
			tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
			result = ::AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(TOKEN_PRIVILEGES), nullptr, nullptr);
		}
		::CloseHandle(hToken);
	}
	return result == TRUE && (::GetLastError() == ERROR_SUCCESS);
}

bool SetPermissions(HKEY root, const wchar_t *subkey, REGSAM reg_view = KEY_WOW64_64KEY)
{
	bool res = false;
	if(!EnablePrivilege())
		return false;

	auto_regkey Key;

	if(ERROR_SUCCESS != Key.open(root, subkey, 0, WRITE_OWNER | reg_view, Key))
		return false;

	PACL pOldDACL{}, pNewDACL{};
	std::vector<PSID> sid;
	std::vector<EXPLICIT_ACCESS> ea;
	SID_IDENTIFIER_AUTHORITY sia{ SECURITY_NT_AUTHORITY };
	PSID pSid{};

	if(!::AllocateAndInitializeSid(&sia, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &pSid))
	   return false;

	sid.push_back(pSid);

	if(ERROR_SUCCESS == ::SetSecurityInfo(Key.get(), SE_REGISTRY_KEY, OWNER_SECURITY_INFORMATION, pSid, nullptr, nullptr, nullptr))
	{
		Key.close();

		if(ERROR_SUCCESS != Key.open(root, subkey, 0, WRITE_DAC| reg_view, Key))
			return false;

		PSECURITY_DESCRIPTOR sd{};

		if(ERROR_SUCCESS == ::GetSecurityInfo(Key.get(), SE_REGISTRY_KEY,
											  DACL_SECURITY_INFORMATION, nullptr, nullptr, &pOldDACL, nullptr, &sd))
		{

			if(::AllocateAndInitializeSid(&sia, 1, SECURITY_LOCAL_SYSTEM_RID, 0, 0, 0, 0, 0, 0, 0, &pSid))
				sid.push_back(pSid);

			if(::AllocateAndInitializeSid(&sia, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_USERS, 0, 0, 0, 0, 0, 0, &pSid))
				sid.push_back(pSid);

			ea.resize(sid.size());

			for(size_t i = 0; i < sid.size(); i++)
			{
				::ZeroMemory(&ea[i], sizeof(EXPLICIT_ACCESS));
				ea[i].grfAccessMode = GRANT_ACCESS;
				ea[i].grfAccessPermissions = GENERIC_ALL;
				ea[i].grfInheritance = CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE;
				ea[i].Trustee.TrusteeForm = TRUSTEE_IS_SID;
				ea[i].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
				ea[i].Trustee.ptstrName = reinterpret_cast<wchar_t*>(sid[i]);
			}

			if(ERROR_SUCCESS == ::SetEntriesInAclW(static_cast<ULONG>(ea.size()), ea.data(), pOldDACL, &pNewDACL))
			{
				res = ERROR_SUCCESS == ::SetSecurityInfo(Key.get(), SE_REGISTRY_KEY,
														 DACL_SECURITY_INFORMATION, nullptr, nullptr, pNewDACL, nullptr);
				::LocalFree(pNewDACL);
			}
			::LocalFree(sd);
		}
	}

	for(auto &s : sid)
		::FreeSid(s);

	return res;
}

//RRF_RT_REG_SZ | RRF_RT_REG_MULTI_SZ | RRF_RT_REG_EXPAND_SZ | RRF_NOEXPAND
void disable_modern(bool _register)
{
	string k;
	k.format(L"CLSID\\%s", string::ToString(IID_FileExplorerContextMenu, 2).c_str());
	auto setval = [=](auto reg)->LSTATUS
	{
		auto_regkey key{};
		auto res = ::RegOpenKeyExW(HKEY_CLASSES_ROOT, k, 0, KEY_CREATE_SUB_KEY | KEY_WOW64_64KEY, key);
		if(res == ERROR_SUCCESS)
		{
			auto treatAs = L"TreatAs";
			if(reg)
			{
				auto_regkey subkey;
				res = key.create_key(treatAs, subkey, KEY_SET_VALUE | KEY_WOW64_64KEY);
				if(res == ERROR_SUCCESS)
				{
					//IID_ExplorerHostCreator
					string treatas = CLS_ContextMenu;
					res = subkey.set_value(nullptr, REG_SZ,
										   reinterpret_cast<const uint8_t *>(treatas.c_str()),
										   treatas.length<int>() * sizeof(wchar_t));
				}
			}
			else
			{
				key.delete_key(treatAs);
			}
		}
		return res;
	};

	//E_ACCESSDENIED, OLE_E_NOTRUNNING, WS_E_ENDPOINT_ACCESS_DENIED
	if(setval(_register) == ERROR_ACCESS_DENIED)
	{
		if(SetPermissions(HKEY_CLASSES_ROOT, k.c_str()))
			setval(_register);
	}
	/*
	IID treatAs{};
	auto hres = ::CoGetTreatAsClass(IID_WindowsUIFileExplorer, &treatAs);
	if(_register && treatAs != IID_ContextMenu)
		hres = ::CoTreatAsClass(IID_WindowsUIFileExplorer, IID_ContextMenu);
	else if(S_OK == hres && treatAs == IID_ContextMenu)
		hres = ::CoTreatAsClass(IID_WindowsUIFileExplorer, GUID_NULL);
	*/
}

//printf("Please wait shell we'll process your command \n");
// r		register
// u		unregister
// s		silent
// restart	restart explorer
// shx	cmh integrated Shell Extensions ContextMenuHandlers
// Register the COM server and the context menu handler.
bool Registration(REGOP reg)
{
	auto ver = &Windows::Version::Instance();

	try
	{
		string dir = IO::Path::Parent(IO::Path::Module(_hInstance));
		string dll_path = IO::Path::Combine(dir, dll_name).move();

		string path = IO::Path::Module(_hInstance).move();
		//string dir = IO::Path::Parent(path).move();

		if(reg.REGISTER)
		{
			if(!dir.empty())
			{
				Security::Permission::SetFile(dir.c_str());
			}

			_log->close();
			//logger->reset();
			//IO::Path::Delete(logger->path());
		}

		if(!ver->IsWindows7OrGreater())
		{
			//windows compatibility
			_log->error(string::Extract(IDS_WINDOWS_COMPATIBILITY));
			return false;
		}

		string msg;

		if(reg.REGISTER || reg.UNREGISTER)
		{
			if(!is_elevated)
			{
				// Missing administrative privileges!
				msg = string::Extract(IDS_ADMIN_PRIVILEGES).move();
				_log->warning(msg);
				if(!reg.SILENT)
				{
					//You will need to provide administrator permission to run this Shell
					MessageBox::Show(msg, APP_FULLNAME, MessageBoxIcon::Warning);
				}
				return false;
			}

			if(reg.REGISTER)
			{
				//logger->create();

				REGOP regop;
				regop.CONTEXTMENU = regop.ICONOVERLAY = true;

				if(!RegistryConfig::Register(dll_path, regop))
				{
					msg = string::Extract(IDS_REGISTER_NOT_SUCCESS).move();
					_log->error(msg);
					if(!reg.SILENT)
					{
						MessageBox::Show(msg, APP_FULLNAME,
										 MessageBoxIcon::Error, MessageBoxButtons::OK);
					}
					return false;
				}
				
// HKEY_CLASSES_ROOT\LibraryFolder\ShellEx\ContextMenuHandlers
// HKEY_CLASSES_ROOT\LibraryFolder\background\shellex\ContextMenuHandlers
				// is windows 11 or later
				if(ver->IsWindows11OrGreater() && reg.TREAT)
				{
					//SOFTWARE\\Classes\\CLSID"
					disable_modern(true);
				}

				msg = string::Extract(IDS_REGISTER_SUCCESS).move();
			}
			else
			{
				if(!RegistryConfig::IsRegistered())
					return false;

				if(!RegistryConfig::Unregister())
				{
					msg = string::Extract(IDS_UNREGISTER_NOT_SUCCESS).move();
					_log->error(msg);
					if(!reg.SILENT)
					{
						MessageBox::Show(msg, APP_FULLNAME,
										 MessageBoxIcon::Error, MessageBoxButtons::OK);
					}
					return false;
				}

				// is windows 11 or later
				// Always remove TreatAs on unregister, even without -treat:
				// a stale TreatAs pointing at our CLSID breaks the modern
				// menu after uninstall.
				if(ver->IsWindows11OrGreater())
					disable_modern(false);

				msg = string::Extract(IDS_UNREGISTER_SUCCESS).move();
			}

			if(ver->IsWindows8OrGreater())
				_log->info(msg);

			if(reg.RESTART)
			{
				if(!reg.SILENT)
				{
					msg += L"\n\n";
					msg += string::Extract(IDS_RESTART_EXPLORERQ).move();
					reg.RESTART = MessageBox::Show(msg, APP_FULLNAME,
											   MessageBoxIcon::Information,
											   MessageBoxButtons::OKCancel) == DialogResult::OK;
				}
			}

			if(reg.RESTART)
			{
				if(ver->IsWindows8OrGreater())
					_log->info(string::Extract(IDS_RESTART_EXPLORER).move());

				if(Windows::Explorer::Restart())
					::Sleep(1000);
			}
			else
			{
				// https://docs.microsoft.com/en-us/windows/desktop/shell/reg-shell-exts#predefined-shell-objects
				// If you do not call SHChangeNotify, the change might not be recognized until the system is rebooted.
				//::SHChangeNotify(SHCNE_ASSOCCHANGED, 0, 0, 0);
				//Windows::Explorer::Refresh();
			}
			if(reg.REGISTER)
				manager_create_start_shortcut();
			return true;
		}
		else if(reg.RESTART)
		{
			if(ver->IsWindows8OrGreater())
				_log->info(string::Extract(IDS_RESTART_EXPLORER).move());
			return Windows::Explorer::Restart();
		}
	}
	catch(...)
	{
#ifdef _DEBUG
		logger->exception(__func__);
#endif
	}
	return false;
}

bool Register(REGOP reg, HWND hwnd = nullptr)
{
	string path = IO::Path::Combine(IO::Path::Parent(IO::Path::Module(nullptr)), dll_name).move();
	DLL dll(path, true);

	if(reg.REGISTER || reg.UNREGISTER)
	{
		if(!is_elevated)
		{
			// Missing administrative privileges!
			string msg = loadstring(IDS_ADMIN_PRIVILEGES, dll).move();
			_log->error(msg);
			if(!reg.SILENT)
			{
				//You will need to provide administrator permission to run this Shell
				::MessageBoxW(hwnd, msg, APP_FULLNAME, MB_ICONWARNING);
			}
			return false;
		}
	}
	
	if(!dll)
	{
		auto ernf = L"shell.dll not found.";
		_log->error(ernf);
		if(!reg.SILENT)
			MessageBox::Show(ernf, APP_FULLNAME, MessageBoxIcon::Warning);
		return false;
	}

	auto ret = Registration(reg);
	_log->close();

	return ret;
}

/*
class Console
{
	FILE *_handle = nullptr;
	HWND _hwnd = nullptr;

	bool init()
	{
		if(!_handle)
		{
			if(::AttachConsole(ATTACH_PARENT_PROCESS))
			{
				_hwnd = ::GetConsoleWindow();
				if(_hwnd) if(0 == ::freopen_s(&_handle, "CONOUT$", "w", stdout))
					::setvbuf(stdout, nullptr, _IONBF, 0);
			}
		}
		return _handle;
	}

public:
	Console()
	{
		init();
	}

	~Console()
	{
		if(_handle)
		{
			if(_hwnd)
				::PostMessageW(_hwnd, WM_KEYUP, VK_RETURN, 0);
			::fflush(_handle);
			::fclose(_handle);
			::FreeConsole();
		}
	}

	Console &write(const wchar_t *buff = nullptr)
	{
		wprintf(buff);
		return *this;
	}

	Console &writel(const wchar_t *buff = nullptr)
	{
		wprintf(L"%s%c", buff, L'\n');
		return *this;
	}
};

void check()
{
	Console c;
	c.writel(L"\n\nNilesoft Shell\n");

	_log->info(L"BEGIN CHECK");

	auto ver = &Windows::Version::Instance();

	auto clsid =[](const string &s)->string
	{
		return (L"CLSID\\" + s).move();
	};

	auto clsid_shex = [=](const string &s)->string
	{
		return (s + L"\\shellex\\ContextMenuHandlers\\nilesoft.shell").move();
	};

	auto exists_key = [=](HKEY k, const string &name)
	{
		if(!Registry::Exists(k, name))
		{
			wprintf(L"[warning] %s\n", name.c_str());
			_log->warning(name);
		}
	};

	auto exists_value_name = [=](HKEY k, const string &subkey, const string &name)
	{
		if(!Registry::ExistsValue(k, subkey, name))
		{
			wprintf(L"[warning] %s\\%s\n", name.c_str(), name.c_str());
			_log->warning(subkey + L"\\" + name);
		}
	};

	exists_key(HKCR, clsid(CLS_ContextMenu));
	exists_key(HKCR, clsid(CLS_FolderExtensions));
	exists_key(HKCR, clsid(CLS_IconOverlay));

	if(ver->IsWindows11OrGreater())
	{
		exists_key(HKCR, string().format(L"CLSID\\%s\\TreatAs", string::ToString(IID_FileExplorerContextMenu, 2).c_str()));
	}

	exists_key(HKCR, clsid_shex(L"*"));
	exists_key(HKCR, clsid_shex(L"Directory"));
	exists_key(HKCR, clsid_shex(L"Directory\\Background"));
	exists_key(HKCR, clsid_shex(L"Drive"));
	exists_key(HKCR, clsid_shex(L"DesktopBackground"));

	exists_value_name(HKLM, HKLM_APPROVED, CLS_ContextMenu);
	
	_log->info(L"END CHECK");
}
*/

struct Theme
{
	int mode = 1;

	struct 
	{
		uint32_t nor = 0xF1F1F1;
		uint32_t sel = 0xF1F1F1;
		uint32_t dis = 0x808080;
	}text;

	struct
	{
		uint32_t nor = 0x000000;
		uint32_t sel = 0x181818;
		uint32_t dis = 0x000000;
	}back;

	struct{
		uint32_t color = 0x000000;
		uint32_t border = 0x404040;
		uint32_t size = 1;
	}frame;
};

Theme m_theme;

HTHEME _hTheme = nullptr;

int APIENTRY wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE, _In_ LPWSTR, _In_ int)
{
	//auto argc = __argc;
	//auto argv = __wargv;

	is_elevated = Security::Elevation::IsElevated();

	COM_INITIALIZER com_initializer(true);

    CommandLine cmdline;
	REGOP reg{};

	_hInstance = hInstance;
	_log = &Logger::Instance();

	if(cmdline.ShowHelp)
	{
		//::DialogBoxParamW(hInstance, MAKEINTRESOURCEW(IDD_CMDLINE), nullptr, (DLGPROC)WndProc, 0);
		auto hWnd = ::CreateDialogParamW(_hInstance, MAKEINTRESOURCEW(IDD_CMDLINE), NULL, (DLGPROC)WndProc, 0);
		if(hWnd)
		{
			// CenterWindow(hwnd);
			::ShowWindow(hWnd, SW_SHOW);

			MSG msg;
			BOOL bRet;
			while((bRet = ::GetMessageW(&msg, nullptr, 0, 0)) != 0)
			{
				if(bRet == -1)
				{
					// Handle the error and possibly exit
					break;
				}
				else if(!IsWindow(hWnd) || !::IsDialogMessageW(hWnd, &msg))
				{
					::TranslateMessage(&msg);
					::DispatchMessage(&msg);
				}
			}
			::DestroyWindow(hWnd);
		}
		_log->close();
		return 0;
	}
	else if(cmdline.empty())
    {
		auto shell_window = ::FindWindowExW(nullptr, nullptr, UI::WC_Window, APP_FULLNAME);
		if (shell_window)
		{
			::SetForegroundWindow(shell_window);
			::SetActiveWindow(shell_window);
			//::SwitchToThisWindow(shell_window, TRUE);
			return 0;
		}

		auto win = Windows::Version::Instance();

		if(win.IsWindowsVersionOrGreater(10, 0, 14393))
			_dpi = DLL::User32<uint32_t>("GetSystemDpiForProcess", ::GetCurrentProcess());
		else
			_dpi = 96;// DC(GetDesktopWindow()).GetDeviceCapsY();

		_hTheme = ::OpenThemeData(nullptr, L"MENU");


        UI::App app(_hInstance, WindowProc, m_theme.frame.color,
                    LoadIcon(_hInstance, MAKEINTRESOURCE(IDI_RPMICON)), CS_HREDRAW | CS_VREDRAW);

        app.Init();

        RECT rc_screen{};
        ::GetClientRect(::GetDesktopWindow(), &rc_screen);

        rc_window.right = dpi(430);
        rc_window.bottom = dpi(384);

        rc_window.left = (rc_screen.right - rc_window.right) / 2;
        rc_window.top = (rc_screen.bottom - rc_window.bottom) / 2;

        main_window = new UI::Window(APP_FULLNAME, rc_window, 0, nullptr, 0);

        if(!main_window || !main_window->Handle)
        {
            return FALSE;
        }

		string path = IO::Path::Module(hInstance);
		path = path.substr(0, path.length() - 3).append(L"dll");

		DLL d(path, true);
		if(d)
		{
			auto hRes = ::FindResourceW(d, L"FONTICON", RT_RCDATA);
			if(hRes)
			{
				auto cjSize = ::SizeofResource(d, hRes);
				auto hResData = ::LoadResource(d, hRes);
				if(hResData)
				{
					auto lpFileView = ::LockResource(hResData);
					if(lpFileView)
					{
						DWORD numFonts = 0;
						_fonthandle = ::AddFontMemResourceEx(lpFileView, cjSize, nullptr, &numFonts);
						if(_fonthandle)
						{
							_hfont_icon = ::CreateFontW(-dpi(20), 0, 0, 0, FW_NORMAL, 0, 0, 0,
														SYMBOL_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
														ANTIALIASED_QUALITY, DEFAULT_PITCH, L"Nilesoft.Shell");
						}
						UnlockResource(lpFileView);
					}
					::FreeResource(hResData);
				}
			}
		}

        main_window->Color.Background = color_background;

        int btn_w = dpi(250), btn_h = dpi(30), left = dpi(150);
		int offset_2 = dpi(2);

        rc_reg.top = dpi(40);
        rc_unreg.top = rc_reg.top + offset_2 + btn_h;

        rc_reg.bottom = btn_h;
        rc_unreg.bottom = btn_h;

        rc_reg.right = btn_w;
        rc_unreg.right = btn_w;

        rc_reg.left = left;
        rc_unreg.left = rc_reg.left;
		
		std::string dd = "<svg viewBox='0 0 512 512' width='100%' height='100%'><path fill='#14DCFE' d='M200 100L300 0l106 106-100 100z'/><path fill='#14A0FF' d='M100 200l100-100 212 212-100 100z'/><path fill='#1465FF' d='M106 406l100-100 106 106-100 100z'/></svg>";
		PlutoVG pluto(dd.data(), static_cast<int>(dd.length()), dpi(96), dpi(96), 96);
		hbitmap_logo = pluto.tobitmap();

		auto btn_close = new UI::Button(L"\uE256", { rc_window.right - dpi(27), dpi(1), dpi(26), dpi(26) }, ID_CLOSE, main_window, BS_OWNERDRAW, _hfont_icon, L"Close");
        
		auto btn_reg = new UI::Button(L"Register\tCtrl+R", rc_reg, ID_REG, main_window, BS_OWNERDRAW);
        auto btn_unreg = new UI::Button(L"Unregister\tCtrl+U", rc_unreg, ID_UNREG, main_window, BS_OWNERDRAW);
        auto btn_restart = new UI::Button(L"Restart Explorer\tCtrl+X", { rc_reg.left, rc_unreg.top + offset_2 + btn_h, btn_w, btn_h }, ID_RESTART, main_window, BS_OWNERDRAW);

        auto tt = btn_restart->Rect.top + dpi(30) + btn_h + offset_2;
        auto tl = btn_restart->Rect.left;

        auto btn_web = new UI::Button(L"\uE11F", { tl, tt, btn_h, btn_h }, ID_WEB, main_window, BS_OWNERDRAW, _hfont_icon,  L"Website Ctrl+W");
       
        tl += btn_h + offset_2;
        auto btn_email = new UI::Button(L"\uE115", { tl, tt, btn_h, btn_h }, ID_EMAIL, main_window, BS_OWNERDRAW, _hfont_icon, L"Email Ctrl+E");
       
        tl += btn_h + offset_2;
        auto btn_bug = new UI::Button(L"\uE22B", { tl, tt, btn_h, btn_h }, ID_GITHUB, main_window, BS_OWNERDRAW, _hfont_icon, L"Github Ctrl+G");

		//tl += (dpi(50) - btn_h) + btn_h + dpi(12);
		tl += btn_h + offset_2;

		auto btn_top2 = tt + btn_h + dpi(6);
		auto btn_left = rc_reg.left;
		g_lang_button = new UI::Button(L"Language\tCtrl+L", { btn_left, btn_top2, btn_w, btn_h }, ID_LANG, main_window, BS_OWNERDRAW);
		auto btn_opencfg = new UI::Button(L"Open config folder\tCtrl+O", { btn_left, btn_top2 + btn_h + offset_2, btn_w, btn_h }, ID_OPENCFG, main_window, BS_OWNERDRAW);
		auto btn_backup = new UI::Button(L"Backup config\tCtrl+B", { btn_left, btn_top2 + (btn_h + offset_2) * 2, btn_w, btn_h }, ID_BACKUPCFG, main_window, BS_OWNERDRAW);
		auto btn_update = new UI::Button(L"Check for updates\tCtrl+P", { btn_left, btn_top2 + (btn_h + offset_2) * 3, btn_w, btn_h }, ID_UPDATE, main_window, BS_OWNERDRAW);
		auto btn_startmenu = new UI::Button(L"Add Start Menu shortcut\tCtrl+M", { btn_left, btn_top2 + (btn_h + offset_2) * 4, btn_w, btn_h }, ID_STARTMENU, main_window, BS_OWNERDRAW);
		manager_refresh_lang_button();


        main_window->SetColor({ btn_reg, btn_unreg,btn_restart,btn_web,btn_email,btn_bug,g_lang_button,btn_opencfg,btn_backup,btn_update,btn_startmenu }, 
							  m_theme.text.nor, m_theme.back.nor, m_theme.text.sel, m_theme.back.sel);//0xeeeee0
        main_window->SetColor({ btn_close }, 0xFFFFFF, m_theme.back.nor, m_theme.text.nor, 0x2311E8);//E81123

		btn_reg->OnDraw = btn_unreg->OnDraw = btn_restart->OnDraw = btn_on_paint;
		g_lang_button->OnDraw = btn_opencfg->OnDraw = btn_backup->OnDraw = btn_update->OnDraw = btn_startmenu->OnDraw = btn_on_paint;

        auto ret = app.Run(main_window);

		delete main_window; 
		_log->close();

		if(_hfont_icon)
			::DeleteObject(_hfont_icon);

		if(g_font_ui)
			::DeleteObject(g_font_ui);

		if(_fonthandle)
			::RemoveFontMemResourceEx(_fonthandle);
		
		if(_hTheme)
		{
			::CloseThemeData(_hTheme);
			_hTheme = nullptr;
		}

		return ret;
    }
    else
    {
		bool _check = false;
		//bool runglyphs = false;
        string cmd;
        for(auto op : cmdline)
        {
            if(op->Kind == '-' || op->Kind == '/')
            {
				if(op->has_name({ L"g", L"glyphs" }))
				{
					//runglyphs = true;
					//break;
				}
				else  if(op->has_name({ L"r", L"register", L"e", L"enable" }))
				{
					reg.REGISTER = true;;
                }
                else if(op->has_name({ L"u", L"unregister", L"d", L"disable" }))
                {
					reg.UNREGISTER = true;
                }
                else if(op->has_name({ L"t", L"treat" }))
                {
					reg.TREAT = true;
                }
				else if(op->has_name({ L"f", L"force" }))
				{
					reg.FOLDEREXTENSIONS=true;
				}
                else if(op->has_name({ L"s", L"silent" }))
                {
					reg.SILENT=true;
                }
                else if(op->has_name(L"restart"))
                {
					reg.RESTART = true;
                }
                else if(op->has_name(L"cmd"))
                {
                    cmd = op->Value;
                }
            }
			else if(op->has_name(L"check"))
			{
				_check = true;
			}
        }

		if(_check)
		{
			manager_doctor();
			return 0;
		}

		if(reg.REGISTER || reg.UNREGISTER || reg.RESTART || reg.FOLDEREXTENSIONS)
		{
			return Register(reg);
		}
        else
        {
            if(!cmd.empty())
            {
                int run = 1;
                if(auto o = cmdline[L"runas"])
                    run = (int)string::ToInt(o);

				int sw = SW_SHOWNORMAL;
				if(auto o = cmdline[L"show"])
					sw = (int)string::ToInt(o);

                string verb = L"open";
                if(run > 1)
                    verb = L"runas";

                ::ShellExecuteW(nullptr,
                                verb,
                                cmd,
                                cmdline[L"args"],
                                cmdline[L"wd"],
								sw);
            }
        }
    }

	_log->close();
    return 1;
}

HFONT FontSize(HFONT hFont, int size, int weight = 0, byte quality= ANTIALIASED_QUALITY)
{
    LOGFONT lf{};
	if(::GetObject(hFont, sizeof(LOGFONT), &lf))
	{
		lf.lfHeight = -size;
		lf.lfWidth = 0;
		if(weight > 0)
			lf.lfWeight = weight;
		lf.lfQuality = quality;
		return ::CreateFontIndirect(&lf);
	}
    return nullptr;
}

bool hover = false;
using namespace Nilesoft::Drawing;

void DrawPng(HDC hdc, HBITMAP hbitmap, int x, int y, int size = 16, int sx = 0, int sy = 0, int alpha = 255)
{
	if(hdc && hbitmap)
	{
		auto memDC = ::CreateCompatibleDC(hdc);
		if(memDC)
		{
			auto prev_bitmap = ::SelectObject(memDC, hbitmap);
			BLENDFUNCTION bf{};
			bf.BlendOp = AC_SRC_OVER;
			bf.BlendFlags = 0;
			bf.SourceConstantAlpha = (byte)alpha;
			bf.AlphaFormat = AC_SRC_ALPHA;
			::GdiAlphaBlend(hdc, x, y, size, size, memDC, sx, sy, size, size, bf);
			// Clean up
			::SelectObject(memDC, prev_bitmap);
			::DeleteDC(memDC);
		}
	}
}

void DrawString(HDC hdc, HFONT hFont, RECT *rc, COLORREF color, const wchar_t *text, int length, DWORD format, uint8_t opacity)
{
	if(!_hTheme || !hdc || !hFont) return;

	BLENDFUNCTION bf = { AC_SRC_OVER, 0, opacity, AC_SRC_ALPHA };
	BP_PAINTPARAMS params = { sizeof(BP_PAINTPARAMS) };
	params.dwFlags = BPPF_ERASE | BPPF_NOCLIP;
	params.pBlendFunction = &bf;
	HDC hdcPaint = nullptr;
	auto hBufferedPaint = ::BeginBufferedPaint(hdc, rc, BPBF_TOPDOWNDIB, &params, &hdcPaint);
	if(hBufferedPaint)
	{
		if(hdcPaint)
		{
			::SetTextColor(hdcPaint, color);
			::SetBkMode(hdcPaint, TRANSPARENT);

			auto hFontOld = ::SelectObject(hdcPaint, hFont);
			DTTOPTS dttOpts = { sizeof(DTTOPTS),  DTT_COMPOSITED | DTT_TEXTCOLOR, color };
			::BufferedPaintClear(hBufferedPaint, rc);
			::DrawThemeTextEx(_hTheme, hdcPaint, 0, 0, text, length, format, rc, &dttOpts);
			::SelectObject(hdcPaint, hFontOld);
		}
		::EndBufferedPaint(hBufferedPaint, TRUE);
	}
}

int __stdcall btn_on_paint(UI::Control* s, int msg, [[maybe_unused]] WPARAM wp, [[maybe_unused]] LPARAM lp)
{
	if(msg == WM_MOUSEMOVE)
		return 1;

	RECT rect{};
	::GetClientRect(s->Handle, &rect);

	DC dc(s->Handle);

	auto color_txt = msg == WM_MOUSEHOVER ? s->Color.Select.Text : s->Color.Text;
	auto color_bg = msg == WM_MOUSEHOVER ? s->Color.Select.Background : s->Color.Background;

	if(msg == WM_MOUSEHOVER || msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP)
	{
		//if(wp & MK_LBUTTON)
		{
			color_bg = s->Color.Select.Background;
		}
	}

	// if(color_bg == -1)
   //      color_bg = c->Parent->Color.Select.Background;

	if(color_bg != -1)
	{
		auto br = ::CreateSolidBrush(color_bg);
		::FillRect(dc, &rect, br);
		::DeleteObject(br);
	}

	if(!s->Text.empty())
	{
		auto ofont = s->Font.Handle;
		LOGFONT lf{};
		GetObject(ofont, sizeof(LOGFONT), &lf);

		lf.lfQuality = CLEARTYPE_QUALITY;
		//lf.lfWeight = FW_SEMIBOLD;
		lf.lfHeight = -dpi(14);
		auto cfont = ::CreateFontIndirect(&lf);
		auto tf = DT_VCENTER | DT_SINGLELINE | DT_NOCLIP;

		auto pos = s->Text.index_of('\t');
		if(pos != string::npos)
		{
			string left = s->Text.substr(0, pos);
			string right = s->Text.substr(pos++);

			auto rc = rect;
			rc.left = dpi(20);
			DrawString(dc, cfont, &rc, color_txt, left, left.length<int>(), DT_LEFT | tf, 255);

			rc.right -= dpi(20);
			DrawString(dc, cfont, &rc, m_theme.text.dis, right, right.length<int>(), DT_RIGHT | tf, 128);
		}
		else
		{
			DrawString(dc, cfont, &rect, color_txt, s->Text, -1, s->Font.Format.Align | tf, 255);
		}
		::DeleteObject(cfont);
	}
    return 0;
};

void Open(HWND hWnd, const wchar_t* cmd)
{
	::ShellExecuteW(hWnd, L"open", cmd, nullptr, nullptr, SW_NORMAL);
}

// ---- community fork: effective config + language + update helpers ----
static string manager_effective_config()
{
	// Mirrors the dll resolution order: registry override, %AppData%, install dir.
	// The returned path is also the default write target (always writable).
	string cfg;
	if(RegistryConfig::get(nullptr, L"config", cfg) && !cfg.empty() && IO::Path::IsFileExists(cfg))
		return cfg.move();
	string appdata = IO::Path::Join(IO::Path::GetKnownFolder(FOLDERID_RoamingAppData), L"\\Nilesoft\\Shell\\shell.nss").move();
	if(!appdata.empty() && IO::Path::IsFileExists(appdata))
		return appdata.move();
	string exe = IO::Path::Module(nullptr).move();
	string local = IO::Path::Combine(IO::Path::Parent(exe), L"shell.nss").move();
	if(IO::Path::IsFileExists(local))
		return local.move();
	return appdata.move();
}

static string manager_config_lang(const string &cfg)
{
	string lang;
	FILE *f = nullptr;
	if(_wfopen_s(&f, cfg.c_str(), L"rb") == 0 && f)
	{
		fseek(f, 0, SEEK_END);
		long size = ftell(f);
		fseek(f, 0, SEEK_SET);
		if(size > 0 && size < 1024 * 1024)
		{
			std::vector<char> buf((size_t)size + 1, 0);
			if(fread(buf.data(), 1, (size_t)size, f) == (size_t)size)
			{
				const char *p = strstr(buf.data(), "$lang");
				if(p)
				{
					p = strchr(p, '"');
					if(p && isalpha((unsigned char)p[1]) && isalpha((unsigned char)p[2]) && p[3] == '"')
					{
						wchar_t code[3] = { (wchar_t)p[1], (wchar_t)p[2], 0 };
						lang = code;
					}
				}
			}
		}
		fclose(f);
	}
	return lang.move();
}

static bool manager_config_set_lang(const string &cfg, const wchar_t *code)
{
	// Same-length in-place swap of the 2-letter code (encoding-safe).
	// If there is no $lang line yet, one is prepended (single-byte files only).
	FILE *f = nullptr;
	if(_wfopen_s(&f, cfg.c_str(), L"rb") != 0 || !f)
		return false;
	bool ok = false;
	fseek(f, 0, SEEK_END);
	long size = ftell(f);
	fseek(f, 0, SEEK_SET);
	if(size > 0 && size < 1024 * 1024)
	{
		std::vector<char> buf((size_t)size);
		if(fread(buf.data(), 1, (size_t)size, f) == (size_t)size)
		{
			std::string view(buf.data(), (size_t)size);
			size_t pos = view.find("$lang");
			if(pos != std::string::npos)
			{
				size_t q1 = view.find('"', pos);
				if(q1 != std::string::npos && q1 + 3 < view.size() && view[q1 + 3] == '"' &&
				   isalpha((unsigned char)view[q1 + 1]) && isalpha((unsigned char)view[q1 + 2]))
				{
					fclose(f);
					f = nullptr;
					if(_wfopen_s(&f, cfg.c_str(), L"r+b") != 0 || !f)
						return false;
					char narrow[3] = { (char)code[0], (char)code[1], 0 };
					fseek(f, (long)q1 + 1, SEEK_SET);
					ok = fwrite(narrow, 1, 2, f) == 2;
				}
			}
			else
			{
				bool single = true;
				for(size_t i = 0; i < view.size() && i < 1024; i++)
				{
					if(view[i] == '\0') { single = false; break; }
				}
				if(single)
				{
					fclose(f);
					f = nullptr;
					std::string out = "$lang = \"";
					out += (char)code[0];
					out += (char)code[1];
					out += "\";\r\n";
					out += view;
					if(_wfopen_s(&f, cfg.c_str(), L"wb") != 0 || !f)
						return false;
					ok = fwrite(out.data(), 1, out.size(), f) == out.size();
				}
			}
		}
	}
	if(f)
		fclose(f);
	return ok;
}

static const wchar_t *manager_lang_name(const string &code)
{
	if(code.equals(L"lt"))
		return L"Lietuvių";
	if(code.equals(L"ru"))
		return L"Русский";
	return L"English";
}

static const wchar_t *manager_lang_next(const string &code)
{
	if(code.equals(L"en"))
		return L"lt";
	if(code.equals(L"lt"))
		return L"ru";
	return L"en";
}

static void manager_refresh_lang_button()
{
	if(!g_lang_button)
		return;
	string label = L"Language: ";
	label += manager_lang_name(manager_config_lang(manager_effective_config()));
	label += L"\tCtrl+L";
	g_lang_button->Text = label;
	::InvalidateRect(g_lang_button->Handle, nullptr, TRUE);
}

static bool manager_copy_tree(const string &from, const string &to)
{
	string pattern = from + L"\\*";
	WIN32_FIND_DATAW fd{};
	HANDLE h = ::FindFirstFileW(pattern.c_str(), &fd);
	if(h == INVALID_HANDLE_VALUE)
		return false;
	::CreateDirectoryW(to.c_str(), nullptr);
	bool ok = true;
	do
	{
		if(wcscmp(fd.cFileName, L".") == 0 || wcscmp(fd.cFileName, L"..") == 0)
			continue;
		string src = from;
		src += L"\\";
		src += fd.cFileName;
		string dst = to;
		dst += L"\\";
		dst += fd.cFileName;
		if(fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			if(!manager_copy_tree(src, dst))
				ok = false;
		}
		else if(!IO::File::Copy(src.c_str(), dst.c_str()))
			ok = false;
	} while(::FindNextFileW(h, &fd));
	::FindClose(h);
	return ok;
}

static bool manager_create_start_shortcut()
{
	wchar_t programs[MAX_PATH]{};
	if(FAILED(::SHGetFolderPathW(nullptr, CSIDL_PROGRAMS, nullptr, 0, programs)))
		return false;
	std::wstring lnk = programs;
	lnk += L"\\Shell.lnk";
	if(::GetFileAttributesW(lnk.c_str()) != INVALID_FILE_ATTRIBUTES)
		return true; // already there
	string exe = IO::Path::Module(nullptr).move();
	::CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
	IShellLinkW *link = nullptr;
	bool ok = false;
	if(SUCCEEDED(::CoCreateInstance(CLSID_ShellLink, nullptr, CLSCTX_INPROC_SERVER, IID_IShellLinkW, (void**)&link)) && link)
	{
		string dir = IO::Path::Parent(exe);
		if(SUCCEEDED(link->SetPath(exe.c_str())) &&
		   SUCCEEDED(link->SetDescription(L"Shell - context menu manager")) &&
		   SUCCEEDED(link->SetWorkingDirectory(dir.c_str())))
		{
			IPersistFile *file = nullptr;
			if(SUCCEEDED(link->QueryInterface(IID_IPersistFile, (void**)&file)) && file)
			{
				ok = SUCCEEDED(file->Save(lnk.c_str(), TRUE));
				file->Release();
			}
		}
		link->Release();
	}
	::CoUninitialize();
	return ok;
}

static bool manager_latest_tag(std::wstring &tag)
{
	bool ok = false;
	HINTERNET hSession = ::WinHttpOpen(L"ShellUpdateCheck/1.0",
		WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
	if(!hSession)
		return false;
	HINTERNET hConnect = ::WinHttpConnect(hSession, L"api.github.com", INTERNET_DEFAULT_HTTPS_PORT, 0);
	if(hConnect)
	{
		HINTERNET hRequest = ::WinHttpOpenRequest(hConnect, L"GET",
			L"/repos/arvaidasre/Shell/releases/latest",
			nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, WINHTTP_FLAG_SECURE);
		if(hRequest)
		{
			if(::WinHttpSendRequest(hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
				WINHTTP_NO_REQUEST_DATA, 0, 0, 0) &&
			   ::WinHttpReceiveResponse(hRequest, nullptr))
			{
				std::string body;
				char chunk[4096];
				DWORD read = 0;
				while(::WinHttpReadData(hRequest, chunk, sizeof(chunk), &read) && read > 0)
					body.append(chunk, read);
				const char *key = "\"tag_name\":\"";
				size_t pos = body.find(key);
				if(pos != std::string::npos)
				{
					pos += strlen(key);
					size_t end = body.find('"', pos);
					if(end != std::string::npos && end > pos && end - pos < 32)
					{
						tag.assign(body.begin() + pos, body.begin() + end);
						ok = true;
					}
				}
			}
			::WinHttpCloseHandle(hRequest);
		}
		::WinHttpCloseHandle(hConnect);
	}
	::WinHttpCloseHandle(hSession);
	return ok;
}

static void manager_doctor()
{
	string dll = IO::Path::Combine(IO::Path::Parent(IO::Path::Module(nullptr)), dll_name).move();
	string cfg = manager_effective_config();
	string treat = L"no";
	{
		string k;
		k.format(L"CLSID\\%s\\TreatAs", string::ToString(IID_FileExplorerContextMenu, 2).c_str());
		if(Registry::Exists(HKEY_CLASSES_ROOT, k.c_str(), 0))
			treat = L"yes";
	}
	string report = L"Shell ";
	report += APP_VERSION;
	report += L" (";
	report += APP_PROCESS;
	report += L")\r\n";
	string line = L"DLL: ";
	line += dll.c_str();
	line += IO::Path::IsFileExists(dll) ? L" (found)\r\n" : L" (MISSING)\r\n";
	report += line.c_str();
	line = L"Registered: ";
	line += RegistryConfig::IsRegistered() ? L"yes\r\n" : L"no\r\n";
	report += line.c_str();
	line = L"Win11 modern takeover (TreatAs): ";
	line += treat.c_str();
	line += L"\r\n";
	report += line.c_str();
	line = L"Config: ";
	line += cfg.c_str();
	line += IO::Path::IsFileExists(cfg) ? L" (found)\r\n" : L" (MISSING)\r\n";
	report += line.c_str();
	line = L"Menu language: ";
	line += manager_lang_name(manager_config_lang(cfg));
	report += line.c_str();
	::MessageBoxW(nullptr, report.c_str(), L"Shell check", MB_OK | MB_ICONINFORMATION);
}

LRESULT CALLBACK WindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch(message)
    {
		case WM_CREATE:
		{
			if(!g_font_ui)
			{
				// Segoe UI Variable (Win11); GDI falls back automatically on older Windows.
				LOGFONTW lf{};
				lf.lfHeight = -dpi(12);
				lf.lfWeight = FW_NORMAL;
				lf.lfCharSet = DEFAULT_CHARSET;
				lf.lfQuality = CLEARTYPE_NATURAL_QUALITY;
				wcsncpy_s(lf.lfFaceName, LF_FACESIZE, L"Segoe UI Variable Text", _TRUNCATE);
				g_font_ui = ::CreateFontIndirectW(&lf);
			}
			::SendMessageW(hWnd, WM_SETFONT, (WPARAM)(g_font_ui ? g_font_ui : ::GetStockObject(DEFAULT_GUI_FONT)), TRUE);
			// Windows 11 style: rounded corners, Mica-capable backdrop, dark mode follows the system.
			// Numeric constants keep this compiling on older SDKs; unknown attributes are ignored at runtime.
			{
				int corner = 2; // DWMWCP_ROUND
				::DwmSetWindowAttribute(hWnd, (DWMWINDOWATTRIBUTE)33, &corner, sizeof(corner)); // DWMWA_WINDOW_CORNER_PREFERENCE
				int backdrop = 2; // DWMSBT_MAINWINDOW (Mica)
				::DwmSetWindowAttribute(hWnd, (DWMWINDOWATTRIBUTE)38, &backdrop, sizeof(backdrop)); // DWMWA_SYSTEMBACKDROP_TYPE
				BOOL dark = FALSE;
				if(auto key = Registry::CurrentUser.OpenSubKey(L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", true, false))
				{
					dark = !key.GetInt(L"AppsUseLightTheme", 1);
					key.Close();
				}
				::DwmSetWindowAttribute(hWnd, (DWMWINDOWATTRIBUTE)20, &dark, sizeof(dark)); // DWMWA_USE_IMMERSIVE_DARK_MODE
			}
			break;
		}
        case WM_ACTIVATE:// Extend the frame into the client area.
        {
           // MARGINS margins{ 0,0,0,1 };
            //::DwmExtendFrameIntoClientArea(hWnd, &margins);
            break;
        }
        case WM_PAINT:
        {
            PAINTSTRUCT ps{};
            DC dc = BeginPaint(hWnd, &ps);
			{
				RECT r;
				::GetWindowRect(hWnd, &r);
				::OffsetRect(&r, -r.left,- r.top);
				dc.draw_fillrect(r, m_theme.frame.color, m_theme.frame.border);

				BITMAP bmp{};
				GetObject(hbitmap_logo, sizeof(BITMAP), &bmp);
				//draw logo
				DrawPng(dc, hbitmap_logo, dpi(35), dpi(35), bmp.bmHeight, 0, 0, is_elevated ? 240 : 128);

				dc.set_back_mode();
				string text = APP_NAME L" ";
#ifdef APP_CANARY
				text += APP_CANARY L" ";
#endif
				text += APP_VERSION;

				dc.set_text(0x808080);

				auto ofont = (HFONT)::GetStockObject(DEFAULT_GUI_FONT);
				auto cfont = FontSize(ofont, dpi(14), CLEARTYPE_NATURAL_QUALITY);
				ofont = dc.set_font(cfont);

				RECT rect = { 0, dpi(150 + 10), dpi(150), dpi(25 + 128 + 32) };

				DrawString(dc, cfont, &rect, 0x808080, text, text.length<int>(), DT_CENTER, 255);

				text.format(L"\xA9 %d %s", VERSION_YEAR, APP_COMPANY);
				rect = { 0, dpi(150 + 30), dpi(150), dpi(25 + 128 + 32 + 15) };
				DrawString(dc, cfont, &rect, 0x808080, text, text.length<int>(), DT_CENTER, 255);
				::DeleteObject(::SelectObject(dc, ofont));
			}
            ::EndPaint(hWnd, &ps);
            return TRUE;
        }
        //case WM_ERASEBKGND:
        //    return 0;
        case WM_NCCALCSIZE:
            return FALSE;
        case WM_NCHITTEST:
            return HTCAPTION;
		case WM_ERASEBKGND:
			return TRUE;
        case WM_COMMAND:
        {
            switch(wParam)
            {
                case ID_CLOSE:
                    ::DestroyWindow(hWnd);
                    break;
                case ID_MINIMIZE:
                    SendMessage(hWnd, WM_SYSCOMMAND, SC_MINIMIZE, lParam);
                    break;
                case ID_WEB:
					Open(hWnd, APP_WEBSITE);
                    break;
                case ID_EMAIL:
					Open(hWnd, L"https://github.com/arvaidasre/Shell/issues");
                    break;
                case ID_GITHUB:
					Open(hWnd, L"https://github.com/arvaidasre/Shell");
                    break;
				case ID_LANG:
				{
					string cfg = manager_effective_config();
					string cur = manager_config_lang(cfg);
					if(cur.empty())
						cur = L"en";
					const wchar_t *next = manager_lang_next(cur);
					if(manager_config_set_lang(cfg, next))
					{
						manager_refresh_lang_button();
						string msg = L"Menu language: ";
						msg += manager_lang_name(next);
						msg += L".\nTakes effect when the menu opens next.";
						::MessageBoxW(hWnd, msg.c_str(), APP_NAME, MB_OK | MB_ICONINFORMATION);
					}
					else
					{
						::MessageBoxW(hWnd, L"Could not update $lang in the config file.\r\nOpen it manually and set $lang to \"en\", \"lt\" or \"ru\".", APP_NAME, MB_OK | MB_ICONWARNING);
					}
					break;
				}
				case ID_OPENCFG:
				{
					string cfg = manager_effective_config();
					string dir = IO::Path::Parent(cfg);
					Open(hWnd, dir.c_str());
					break;
				}
				case ID_BACKUPCFG:
				{
					string cfg = manager_effective_config();
					string dir = IO::Path::Parent(cfg);
					string parent = IO::Path::Parent(dir);
					SYSTEMTIME st{};
					::GetLocalTime(&st);
					string bak = parent;
					bak += L"\\config-backup-";
					{
						string stamp;
						stamp.format(L"%04d%02d%02d-%02d%02d",
							st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute);
						bak += stamp.c_str();
					}
					bool ok = manager_copy_tree(dir, bak);
					if(ok)
					{
						string msg = L"Config backed up to:\n";
						msg += bak.c_str();
						::MessageBoxW(hWnd, msg.c_str(), APP_NAME, MB_OK | MB_ICONINFORMATION);
					}
					else
					{
						::MessageBoxW(hWnd, L"Backup failed. Is the config folder writable?", APP_NAME, MB_OK | MB_ICONWARNING);
					}
					break;
				}
				case ID_UPDATE:
				{					std::wstring tag;
					if(!manager_latest_tag(tag))
					{
						::MessageBoxW(hWnd, L"Could not reach GitHub releases.\r\nCheck your connection and try again.", APP_NAME, MB_OK | MB_ICONWARNING);
					}
					else
					{
						std::wstring current = L"v";
						current += APP_VERSION;
						if(tag == current)
						{
							string msg;
							msg.format(L"You have the latest version (%s).", APP_VERSION);
							::MessageBoxW(hWnd, msg.c_str(), APP_NAME, MB_OK | MB_ICONINFORMATION);
						}
						else if(::MessageBoxW(hWnd, (std::wstring(L"A new version is available: ") + tag + L"\nInstalled: " + current + L"\n\nOpen the releases page?").c_str(),
							APP_NAME, MB_YESNO | MB_ICONINFORMATION) == IDYES)
						{
							Open(hWnd, L"https://github.com/arvaidasre/Shell/releases");
						}
					}
					break;
				}
				case ID_STARTMENU:
				{
					if(manager_create_start_shortcut())
						::MessageBoxW(hWnd, L"Start Menu shortcut is in place.", APP_NAME, MB_OK | MB_ICONINFORMATION);
					else
						::MessageBoxW(hWnd, L"Could not create the Start Menu shortcut.", APP_NAME, MB_OK | MB_ICONWARNING);
					break;
				}
				case ID_RESTART:
					Windows::Explorer::Restart();
					break;
                case ID_REG:
				case ID_UNREG:
				{
					REGOP reg{};
					reg.RESTART = true;
					reg.TREAT = true;
					if(wParam == ID_REG)
						reg.REGISTER = true;
					else if(wParam == ID_UNREG)
						reg.UNREGISTER = true;
					return Register(reg, hWnd);
				}
            }
            break;
        }
        case WM_SYSKEYUP:
        case WM_KEYUP:
      //      return ::DefWindowProc(hWnd, message, wParam, lParam);
     //   case WM_SYSKEYDOWN:
     //   case WM_KEYDOWN:
        {
            if(::GetAsyncKeyState(VK_CONTROL) < 0)
            {
                //  int count = 0;
                //  for(int i = 0; i < 256; i++)
                //      count += IsAsyncKeyDown(i);

                //  if(count == 2)
                {
                    auto vkCode = LOWORD(wParam); // virtual-key code
                    switch(vkCode)
                    {
                        case 'R':
                            main_window->SendCommand(ID_REG);
                            break;
                        case 'U':
                            main_window->SendCommand(ID_UNREG);
                            break;
                        case 'X':
                            main_window->SendCommand(ID_RESTART);
                            break;
                        case 'W':
                            main_window->SendCommand(ID_WEB);
                            break;
                        case 'E':
                            main_window->SendCommand(ID_EMAIL);
                            break;
                        case 'G':
                            main_window->SendCommand(ID_GITHUB);
                            break;
                        case 'L':
                            main_window->SendCommand(ID_LANG);
                            break;
                        case 'O':
                            main_window->SendCommand(ID_OPENCFG);
                            break;
                        case 'B':
                            main_window->SendCommand(ID_BACKUPCFG);
                            break;
                        case 'P':
                            main_window->SendCommand(ID_UPDATE);
                            break;
                        case 'M':
                            main_window->SendCommand(ID_STARTMENU);
                            break;
                    }
                }
            }
            break;
        }
        case WM_CLOSE:
            ::DestroyWindow(hWnd);
            break;
        case WM_DESTROY:
        case WM_ENDSESSION:
            ::PostQuitMessage(0);
            break;
        default:
            return ::DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}

BOOL CALLBACK WndProc(HWND hwnd, UINT message, WPARAM wParam, [[maybe_unused]] LPARAM lParam)
{
    switch(message)
    {
		case WM_INITDIALOG:
        {
			//WCHAR szTitle[100];
			// LoadString(g_hInstance, IDS_APP_TITLE, szTitle, ARRAYSIZE(szTitle));
            auto usage = L"Command-Line Help\r\n\r\n"
                //L"shell.exe [-[r][u]] [-i] [-s] [-re]\n\n"
                L"-register\t\tRegistering.\r\n"
                L"-unregister\tUnregistering.\r\n"
                L"-treat\t\tDisable Windows 11 context menu.\r\n"
                L"-silent\t\tPrevents displaying messages.\r\n"
                L"-restart\t\tRestart Windows Explorer.\r\n"
                L"check\t\tValidate installation and config.\r\n\r\n"
                //L"-runas:N\t\tLaunch with elevated privileges.\r\n"
                //L"\t\tN=[admin | system | trustedinsaller]\r\n\r\n"
                L"-?\t\tDispay this help message.\r\n\r\n"
                L"Examples:\r\nshell.exe -register -treat\r\n"
              //  L"shell.exe -runas:admin -cmd:'cmd.exe' -args:\"/K echo Hello world!\"\r\n"
                ;
            ::SetDlgItemTextW(hwnd, IDC_CMDLINE_TEXT, usage);
           // SendDlgItemMessage(hwnd, IDC_CMDLINE_TEXT, EM_SETSEL, -1, -1);
            break;
        }
        case WM_CLOSE:
        {
            ::PostQuitMessage(0);
            break;
        }
        case WM_COMMAND:
        {   
            if(LOWORD(wParam) == IDOK)
                ::PostQuitMessage(0);
            break;
        }
       
    }
    return FALSE;
}
