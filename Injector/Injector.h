#pragma once

// For exporting functions without name-mangling
#define DllExport extern "C" __declspec( dllexport )

/* Injects the specified dll into a running process, calls a specific
* method on that dll, then unloads the dll.
*/
DllExport BOOL Inject(DWORD ProcessId, const PSTR ModuleName, const PSTR ExportName, const PWSTR ExportArgument);

/* Given a pid, a dll name, and a method name, walks the export address
* table then gets remote address of the named method.
*/
PTHREAD_START_ROUTINE GetExportRemoteAddr(const DWORD ProcessId, const PSTR ModuleName, const PSTR ExportName);

/* Given a pid and remote address of an export, calls the export.
*/
void CallExport(const DWORD ProcessId, const PTHREAD_START_ROUTINE pfnThreadRtn, const PWSTR ExportArgument);
