#include <Windows.h>

typedef BOOL(WINAPI *pSetProcessWorkingSetSize)(
    _In_ HANDLE hProcess,
    _In_ SIZE_T dwMinimumWorkingSetSize,
    _In_ SIZE_T dwMaximumWorkingSetSize);

pSetProcessWorkingSetSize SetProcessWorkingSetSize = nullptr;

typedef HMODULE (WINAPI *pLoadLibraryEx)(
    _In_       LPCWSTR lpFileName,
    _Reserved_ HANDLE  hFile,
    _In_       DWORD   dwFlags
);


extern "C" int __declspec(dllexport) WINAPI ReleaseWorkingSet() {
    if (!SetProcessWorkingSetSize) {
        MEMORY_BASIC_INFORMATION info = {};
        if (VirtualQuery(VirtualQuery, &info, sizeof(info))) {
            auto kernelAddr = (HMODULE)info.AllocationBase; //kernelbase.dll
            auto LoadLibraryEx = (pLoadLibraryEx)GetProcAddress(kernelAddr, "LoadLibraryExW");
            kernelAddr = LoadLibraryEx(L"kernel32.dll", nullptr, 0);
            if (!kernelAddr)
                kernelAddr = LoadLibraryEx(L"C:\\Windows\\System32\\forwarders\\kernel32.dll", nullptr, 0);
            SetProcessWorkingSetSize =
                (pSetProcessWorkingSetSize)GetProcAddress(
                    kernelAddr,
                    "SetProcessWorkingSetSize");
            if (!SetProcessWorkingSetSize) {
                return 0;
            }
        }
        else {
            return 0;
        }
    }
    return SetProcessWorkingSetSize(
        GetCurrentProcess(),
        0xFFFFFFFF,
        0xFFFFFFFF);
}