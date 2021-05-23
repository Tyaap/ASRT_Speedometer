#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <TlHelp32.h>
#include <stdlib.h>
#include <string>
#include <psapi.h>

#include "Injector.h"
#include "HCommonEnsureCleanup.h"

using namespace Hades;
using namespace std;

BOOL Inject(DWORD ProcessId, const PWSTR ModulePath, const PSTR ExportName, const PWSTR ExportArgument)
{
    cout << "Inject: Attempting module injection." << endl;
    HMODULE hKernel32 = GetModuleHandle(L"kernel32.dll");

    EnsureCloseHandle hProcess = OpenProcess(
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_CREATE_THREAD
        | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, ProcessId);
    if (!hProcess)
    {
        cout << "Inject: OpenProcess() failed: " << GetLastError() << endl;
        return false;
    }

    // LoadLibraryA needs a string as its argument, but it needs to be in
    // the remote Process' memory space.
    size_t StrLength = wcslen(ModulePath);
    LPVOID RemoteString = VirtualAllocEx(hProcess, NULL, StrLength * sizeof(WCHAR), MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    WriteProcessMemory(hProcess, RemoteString, ModulePath, StrLength * sizeof(WCHAR), NULL);

    // Start a remote thread on the targeted Process, using LoadLibraryW
    // as our entry point to load a custom dll. (The W is for wide char)
    EnsureCloseHandle LoadThread = CreateRemoteThread(hProcess, NULL, NULL,
        (LPTHREAD_START_ROUTINE)GetProcAddress(hKernel32, "LoadLibraryW"),
        RemoteString, NULL, NULL);
    WaitForSingleObject(LoadThread, INFINITE);

    // Clean up the remote string
    VirtualFreeEx(hProcess, RemoteString, 0, MEM_RELEASE);

    cout << "GetModuleBaseAddr()" << endl;
    DWORD modBaseAddr = GetModuleBaseAddr(ProcessId, ModulePath);
    cout << "GetExportAddr()" << endl;
    DWORD exportAddr = GetExportAddr(ModulePath, ExportName);
    if (!modBaseAddr || !exportAddr)
    {
        cout << "Module injection failed." << endl;
        return false;
    }
    PTHREAD_START_ROUTINE remoteExportAddr = reinterpret_cast<PTHREAD_START_ROUTINE>(modBaseAddr + exportAddr);

    // Call the function we wanted in the first place
    cout << "RemoteCall()" << endl;
    RemoteCall(ProcessId, remoteExportAddr, ExportArgument);
    return true;
}

bool RemoteCall(const DWORD ProcessId, const LPTHREAD_START_ROUTINE lpfnThreadRtn, const PWSTR ExportArgument)
{
    // Open the process so we can create the remote string
    EnsureCloseHandle hProcess = OpenProcess(
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_CREATE_THREAD 
        | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, ProcessId);

    if (NULL == hProcess)
    {
        cout << "CallExport: OpenProcess() failed: " << GetLastError() << endl;
        return false;
    }

    // Copy the string argument over to the remote process
    size_t StrNumBytes = wcslen(ExportArgument) * sizeof(WCHAR);
    LPVOID RemoteString = VirtualAllocEx(hProcess, NULL, StrNumBytes,
        MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    if (!WriteProcessMemory(hProcess, RemoteString, ExportArgument, StrNumBytes, NULL))
    {
        cout << "CallExport: WriteProcessMemory() failed: " << GetLastError() << endl;
        return false;
    }

    // Create a remote thread that calls the desired export
    EnsureCloseHandle Thread = CreateRemoteThread(hProcess, NULL, NULL,
        lpfnThreadRtn, RemoteString, NULL, NULL);
    if (!Thread)
    {
        cout << "CallExport: Could not create thread in remote process." << endl;
        return false;
    }
    return true;
}

DWORD GetModuleBaseAddr(const DWORD ProcessId, const PWSTR ModulePath)
{
    // Get a handle to the process.
    EnsureCloseHandle hProcess = OpenProcess(PROCESS_QUERY_INFORMATION |
        PROCESS_VM_READ,
        FALSE, ProcessId);
    if (NULL == hProcess)
    {
        cout << "GetModuleBaseAddr: OpenProcess() failed: " << GetLastError() << endl;
        return 0;
    }

    // Get a list of all the modules in this process.
    HMODULE hMods[1024];
    DWORD cbNeeded;
    if (!EnumProcessModules(hProcess, hMods, sizeof(hMods), &cbNeeded))
    {
        cout << "GetModuleBaseAddr: EnumProcessModules() failed: " << GetLastError() << endl;
        return 0;
    }
    for (unsigned int i = 0; i < (cbNeeded / sizeof(HMODULE)); i++)
    {
        WCHAR szModName[MAX_PATH];
        // Get the full path to the module's file.
        if (GetModuleFileNameExW(hProcess, hMods[i], szModName, sizeof(szModName) / sizeof(WCHAR)))
        {
            if (wcscmp(szModName, ModulePath) == 0)
            {
                MODULEINFO mi;
                GetModuleInformation(hProcess, hMods[i], &mi, sizeof(mi));
                return reinterpret_cast<DWORD>(mi.lpBaseOfDll);
            }
        }
    }
    cout << "GetModuleBaseAddr: Could not find module!" << endl;
    return 0;
}

DWORD GetExportAddr(const PWSTR ModulePath, const PSTR ExportName)
{
    // Load module as data so we can read the export address table (EAT) locally.
    EnsureFreeLibrary MyModule(LoadLibraryExW(ModulePath, NULL, DONT_RESOLVE_DLL_REFERENCES));
    return reinterpret_cast<DWORD>(GetProcAddress(MyModule, ExportName)) - reinterpret_cast<DWORD>(static_cast<LPVOID>(MyModule));
}

