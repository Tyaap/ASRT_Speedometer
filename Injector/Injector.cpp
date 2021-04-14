#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <TlHelp32.h>
#include <stdlib.h>
#include <string>

#include "Injector.h"
#include "HCommonEnsureCleanup.h"

using namespace Hades;
using namespace std;

BOOL Inject(DWORD ProcessId, const PWSTR ModulePath, const PSTR ExportName, const PWSTR ExportArgument)
{
    cout << "Inject: Attempting module injection." << endl;
    HMODULE hKernel32 = GetModuleHandle(L"kernel32.dll");

    EnsureCloseHandle Proc = OpenProcess(
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_CREATE_THREAD
        | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, ProcessId);
    if (!Proc)
    {
        cout << "Process found, but OpenProcess() failed: " << GetLastError() << endl;
        return false;
    }

    // LoadLibraryA needs a string as its argument, but it needs to be in
    // the remote Process' memory space.
    size_t StrLength = wcslen(ModulePath);
    LPVOID RemoteString = (LPVOID)VirtualAllocEx(Proc, NULL, StrLength * sizeof(WCHAR), MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    WriteProcessMemory(Proc, RemoteString, ModulePath, StrLength * sizeof(WCHAR), NULL);

    // Start a remote thread on the targeted Process, using LoadLibraryW
    // as our entry point to load a custom dll. (The W is for wide char)
    EnsureCloseHandle LoadThread = CreateRemoteThread(Proc, NULL, NULL,
        (LPTHREAD_START_ROUTINE)GetProcAddress(hKernel32, "LoadLibraryW"),
        RemoteString, NULL, NULL);
    WaitForSingleObject(LoadThread, INFINITE);

    // Clean up the remote string
    VirtualFreeEx(Proc, RemoteString, 0, MEM_RELEASE);

    wstring tmpStr(ModulePath);
    size_t nameStart = tmpStr.find_last_of(L"\\") + 1;

    PTHREAD_START_ROUTINE exportAddr = GetExportRemoteAddr(ProcessId, ModulePath + nameStart, ExportName);
    if (!exportAddr)
    {
        cout << "Module injection failed." << endl;
        return false;
    }

    // Call the function we wanted in the first place
    CallExport(ProcessId, exportAddr, ExportArgument);
    return true;
}

void CallExport(const DWORD ProcessId, const PTHREAD_START_ROUTINE pfnThreadRtn, const PWSTR ExportArgument)
{
    // Open the process so we can create the remote string
    EnsureCloseHandle Proc = OpenProcess(
        PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_CREATE_THREAD 
        | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, ProcessId);

    // Copy the string argument over to the remote process
    size_t StrNumBytes = wcslen(ExportArgument) * sizeof(WCHAR);
    LPVOID RemoteString = (LPVOID)VirtualAllocEx(Proc, NULL, StrNumBytes,
        MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    WriteProcessMemory(Proc, RemoteString, ExportArgument, StrNumBytes, NULL);

    // Create a remote thread that calls the desired export
    EnsureCloseHandle Thread = CreateRemoteThread(Proc, NULL, NULL,
        (LPTHREAD_START_ROUTINE)pfnThreadRtn, RemoteString, NULL, NULL);
    if (!Thread)
    {
        cout << "CallExport: Could not create thread in remote process." << endl;
        return;
    }
}
PTHREAD_START_ROUTINE GetExportRemoteAddr(const DWORD ProcessId, const PWSTR ModuleName, const PSTR ExportName)
{
    // Grab a new Snapshot of the process
    EnsureCloseHandle Snapshot(CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, ProcessId));
    if (Snapshot == INVALID_HANDLE_VALUE)
    {
        cout << "GetExportRemoteAddr: Could not get module snapshot for remote process." << endl;
        return 0;
    }

    // Get the ModuleEntry structure of the desired library
    MODULEENTRY32W ModEntry = { sizeof(ModEntry) };
    bool Found = false;
    BOOL bMoreMods = Module32FirstW(Snapshot, &ModEntry);
    for (; bMoreMods; bMoreMods = Module32NextW(Snapshot, &ModEntry))
    {
        // For debug
        wcout << ModEntry.szModule << endl;
        Found = wcscmp(ModEntry.szModule, ModuleName) == 0;
        if (Found)
            break;
    }
    if (!Found)
    {
        cout << "GetExportRemoteAddr: Could not find module in remote process." << endl;
        return 0;
    }

    // Get module base address
    PBYTE ModuleBase = ModEntry.modBaseAddr;

    // Load module as data so we can read the export address table (EAT) locally.
    EnsureFreeLibrary MyModule(LoadLibraryEx(ModuleName, NULL,
        DONT_RESOLVE_DLL_REFERENCES));

    // Get module pointer
    PVOID Module = static_cast<PVOID>(MyModule);

    // Get pointer to DOS header
    PIMAGE_DOS_HEADER pDosHeader = reinterpret_cast<PIMAGE_DOS_HEADER>(
        static_cast<HMODULE>(Module));
    if (!pDosHeader || pDosHeader->e_magic != IMAGE_DOS_SIGNATURE)
    {
        cout << "GetExportRemoteAddr: DOS PE header is invalid." << endl;
        return 0;
    }

    // Get pointer to NT header
    PIMAGE_NT_HEADERS pNtHeader = reinterpret_cast<PIMAGE_NT_HEADERS>(
        reinterpret_cast<PCHAR>(Module) + pDosHeader->e_lfanew);
    if (pNtHeader->Signature != IMAGE_NT_SIGNATURE)
    {
        cout << "GetExportRemoteAddr: NT PE header is invalid." << endl;
        return 0;
    }

    // Get pointer to image export directory
    PVOID pExportDirTemp = reinterpret_cast<PBYTE>(Module) +
        pNtHeader->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].
        VirtualAddress;
    PIMAGE_EXPORT_DIRECTORY pExportDir =
        reinterpret_cast<PIMAGE_EXPORT_DIRECTORY>(pExportDirTemp);

    // Symbol names could be missing entirely
    if (pExportDir->AddressOfNames == NULL)
    {
        cout << "GetExportRemoteAddr: Symbol names missing entirely." << endl;
        return 0;
    }

    // Get pointer to export names table, ordinal table, and address table
    PDWORD pNamesRvas = reinterpret_cast<PDWORD>(
        reinterpret_cast<PBYTE>(Module) + pExportDir->AddressOfNames);
    PWORD pNameOrdinals = reinterpret_cast<PWORD>(
        reinterpret_cast<PBYTE>(Module) + pExportDir->AddressOfNameOrdinals);
    PDWORD pFunctionAddresses = reinterpret_cast<PDWORD>(
        reinterpret_cast<PBYTE>(Module) + pExportDir->AddressOfFunctions);

    // Variable to hold the export address
    FARPROC pExportAddr = 0;

    // Walk the array of this module's function names
    for (DWORD n = 0; n < pExportDir->NumberOfNames; n++)
    {
        // Get the function name
        PSTR CurrentName = reinterpret_cast<PSTR>(
            reinterpret_cast<PBYTE>(Module) + pNamesRvas[n]);


        // If not the specified function, try the next one
        if (strcmp(ExportName, CurrentName) != 0) continue;

        // We found the specified function
        // Get this function's Ordinal value
        WORD Ordinal = pNameOrdinals[n];

        // Get the address of this function's address
        pExportAddr = reinterpret_cast<FARPROC>(reinterpret_cast<PBYTE>(Module)
            + pFunctionAddresses[Ordinal]);

        // We got the func. Break out.
        break;
    }

    // Nothing found, throw exception
    if (!pExportAddr)
    {
        cout << "GetExportRemoteAddr: Could not find " << ExportName << "." << endl;
        return 0;
    }

    // Convert local address to remote address
    PTHREAD_START_ROUTINE pfnThreadRtn =
        reinterpret_cast<PTHREAD_START_ROUTINE>((reinterpret_cast<DWORD_PTR>(
            pExportAddr) - reinterpret_cast<DWORD_PTR>(Module)) +
            reinterpret_cast<DWORD_PTR>(ModuleBase));
    return pfnThreadRtn;
}
