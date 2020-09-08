#include "stdafx.h"
#include <Windows.h>
#include <metahost.h>
#pragma comment(lib, "mscoree.lib")

#include "Bootstrapper.h"
#include <sstream>

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            //DisplayPid();
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}

void DisplayPid()
{
    DWORD pid = GetCurrentProcessId();
    wchar_t buf[64];
    wsprintf(buf, L"Hey, it worked! Pid is %d", pid);

    MessageBox(NULL, buf, L"Injected MessageBox", NULL);
}

DllExport void LoadManagedProject(const PWSTR managedDllLocation)
{
    HRESULT hr;
    ICLRMetaHost* pClrMetaHost = NULL;
    ICLRRuntimeInfo* pClrRuntimeInfo = NULL;
    ICLRRuntimeHost* pClrRuntimeHost = NULL;

    StartCLR(L"v4.0.30319", &pClrMetaHost, &pClrRuntimeInfo, &pClrRuntimeHost);
    if (pClrRuntimeHost != NULL)
    {
        DWORD result;
        hr = pClrRuntimeHost->ExecuteInDefaultAppDomain(
            managedDllLocation,
            L"EntryPoint",
            L"Main",
            L"",
            &result);
        pClrRuntimeHost->Release();
        pClrRuntimeInfo->Release();
        pClrMetaHost->Release();
        // We do not clean up the dlls, in case another program is using them.
    }
}

void StartCLR(
    LPCWSTR dotNetVersion,
    ICLRMetaHost** ppClrMetaHost,
    ICLRRuntimeInfo** ppClrRuntimeInfo,
    ICLRRuntimeHost** ppClrRuntimeHost)
{
    HRESULT hr;

    // Get the CLRMetaHost that tells us about .NET on this machine
    hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)ppClrMetaHost);
    if (hr == S_OK)
    {
        // Get the runtime information for the particular version of .NET
        hr = (*ppClrMetaHost)->GetRuntime(dotNetVersion, IID_PPV_ARGS(ppClrRuntimeInfo));
        if (hr == S_OK)
        {
            // Check if the specified runtime can be loaded into the process. This
            // method will take into account other runtimes that may already be
            // loaded into the process and set pbLoadable to TRUE if this runtime can
            // be loaded in an in-process side-by-side fashion.
            BOOL fLoadable;
            hr = (*ppClrRuntimeInfo)->IsLoadable(&fLoadable);
            if ((hr == S_OK) && fLoadable)
            {
                // Load the CLR into the current process and return a runtime interface
                // pointer.
                hr = (*ppClrRuntimeInfo)->GetInterface(CLSID_CLRRuntimeHost,
                    IID_PPV_ARGS(ppClrRuntimeHost));
                if (hr == S_OK)
                {
                    // Start it. This is okay to call even if the CLR is already running
                    (*ppClrRuntimeHost)->Start();
                    return;
                }
            }
        }
    }
    // Cleanup if failed
    if (*ppClrRuntimeHost)
    {
        (*ppClrRuntimeHost)->Release();
        (*ppClrRuntimeHost) = NULL;
    }
    if (*ppClrRuntimeInfo)
    {
        (*ppClrRuntimeInfo)->Release();
        (*ppClrRuntimeInfo) = NULL;
    }
    if (*ppClrMetaHost)
    {
        (*ppClrMetaHost)->Release();
        (*ppClrMetaHost) = NULL;
    }
    return;
}
