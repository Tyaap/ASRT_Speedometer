#pragma once

// For exporting functions without name-mangling
#define DllExport extern "C" __declspec( dllexport )

HMODULE moduleHandle;

// Dll entry point
BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
);

// Displays the pid of the current process
// Mainly included for debugging purposes.
void DisplayPid();

// Our sole export for the time being
DllExport void LoadManagedProject(const PWSTR managedDllLocation);

// Not exporting, so go ahead and name-mangle
void StartCLR(
    LPCWSTR dotNetVersion,
    ICLRMetaHost** ppClrMetaHost,
    ICLRRuntimeInfo** ppClrRuntimeInfo,
    ICLRRuntimeHost** ppClrRuntimeHost);
