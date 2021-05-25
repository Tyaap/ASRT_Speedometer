#pragma once

// For exporting functions without name-mangling
#define DllExport extern "C" __declspec( dllexport )

/* Injects the specified dll into a running process, calls a specific
* method on that dll, then unloads the dll.
*/
DllExport BOOL Inject(DWORD ProcessId, const PWSTR ModuleName, const PSTR ExportName, const PWSTR ExportArgument);

DWORD GetModuleBaseAddr(const DWORD ProcessId, const PWSTR ModulePath);

DWORD GetExportAddr(const PWSTR ModulePath, const PSTR ExportName);

/* Given a pid and remote address of an export, calls the export.
*/
bool RemoteCall(const DWORD ProcessId, const PTHREAD_START_ROUTINE pfnThreadRtn, const PWSTR ExportArgument);

std::string GetLastErrorAsString();