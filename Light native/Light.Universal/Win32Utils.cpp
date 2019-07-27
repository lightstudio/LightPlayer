#include <Windows.h>

//HMODULE _cdecl get_krnl_addr()
//{
//	_TEB* teb = NtCurrentTeb();
//	DWORD* peb = (DWORD*) *(DWORD*) ((char*) teb + 0x30);
//	DWORD* pebldr = (DWORD*) *(DWORD*) ((char*) peb + 0xc);
//	DWORD* ioml = (DWORD*) *(DWORD*) ((char*) pebldr + 0x1c);
//	return (HMODULE) *(DWORD*) (*ioml + 8);
//}

inline HMODULE GetKernelAddress()
{
	_TEB* teb = NtCurrentTeb();
	DWORD* peb = (DWORD*) *(DWORD*) ((char*) teb + 0x30);
	DWORD* pebldr = (DWORD*) *(DWORD*) ((char*) peb + 0xc);
	DWORD* ioml = (DWORD*) *(DWORD*) ((char*) pebldr + 0x1c);
	return (HMODULE) *(DWORD*) (*ioml + 8);
}

//HANDLE LightAPI_LoadLibraryExA(_In_ LPCTSTR lpFileName, _Reserved_  HANDLE hFile, _In_ DWORD dwFlags)
//{
//	HMODULE kernel = GetKernelAddress();
//	typedef HMODULE(WINAPI* LL)(_In_ LPCTSTR lpFileName, _Reserved_  HANDLE hFile, _In_ DWORD dwFlags);
//	return ((LL) GetProcAddress(kernel, "LoadLibraryExA"))(lpFileName, hFile, dwFlags);
//}
//
//HANDLE LightAPI_LoadLibraryExW(_In_ LPCWSTR lpLibFileName, _Reserved_ HANDLE hFile, _In_ DWORD dwFlags)
//{
//	HMODULE kernel = GetKernelAddress();
//	typedef HMODULE(WINAPI* LL)(_In_ LPCWSTR lpLibFileName, _Reserved_ HANDLE hFile, _In_ DWORD dwFlags);
//	return ((LL) GetProcAddress(kernel, "LoadLibraryExW"))(lpLibFileName, hFile, dwFlags);
//}
//
DWORD WINAPI LightAPI_SetFilePointer(_In_ HANDLE hFile, _In_ LONG lDistanceToMove, _Inout_opt_ PLONG lpDistanceToMoveHigh, _In_ DWORD dwMoveMethod)
{
	HMODULE kernel = GetKernelAddress();
	typedef DWORD(WINAPI*SFP)(_In_ HANDLE hFile, _In_ LONG lDistanceToMove, _Inout_opt_ PLONG lpDistanceToMoveHigh, _In_ DWORD dwMoveMethod);
	return ((SFP) GetProcAddress(kernel, "SetFilePointer"))(hFile, lDistanceToMove, lpDistanceToMoveHigh, dwMoveMethod);
}


//void Sleep(DWORD timeout)
//{
//	static HANDLE mutex = 0;
//	if (!mutex)
//	{
//		mutex = CreateEventEx(0, 0, 0, EVENT_ALL_ACCESS);
//	}
//	WaitForSingleObjectEx(mutex, timeout, FALSE);
//}

////Extern Code (to use in other projects)
//extern HANDLE LightAPI_LoadLibraryExW(_In_ LPCWSTR lpLibFileName, _Reserved_ HANDLE hFile, _In_ DWORD dwFlags);
//extern HANDLE LightAPI_LoadLibraryExA(_In_ LPCTSTR lpFileName, _Reserved_  HANDLE hFile, _In_ DWORD dwFlags);
//extern void Sleep(DWORD timeout);
//extern "C" uintptr_t __cdecl _beginthreadex(_In_opt_ void * _Security, _In_ unsigned _StackSize,
//	_In_ unsigned(__stdcall * _StartAddress) (void *), _In_opt_ void * _ArgList,
//	_In_ unsigned _InitFlag, _Out_opt_ unsigned * _ThrdAddr);
//extern DWORD __cdecl LightAPI_SetFilePointer(_In_ HANDLE hFile, _In_ LONG lDistanceToMove, _Inout_opt_ PLONG lpDistanceToMoveHigh, _In_ DWORD dwMoveMethod);
//extern "C"
//{
//
//#if !_M_ARM
//	extern float pow(float x, float y);
//	extern float fabs(float x);
//	extern float sqrt(float x);
//	extern float exp(float x);
//	extern float sin(float x);
//	extern float cos(float x);
//	extern float atan2(float y, float x);
//
//	float  __cdecl powf(_In_ float _X, _In_ float _Y)
//	{
//		return (float) pow(_X, _Y);
//	}
//	float __cdecl fabsf(_In_ float _X)
//	{
//		return (float) fabs(_X);
//	}
//	float __cdecl sqrtf(_In_ float _X)
//	{
//		return (float) sqrt(_X);
//	}
//	float  __cdecl expf(_In_ float _X)
//	{
//		return (float) exp(_X);
//	}
//	float __cdecl sinf(_In_ float _X)
//	{
//		return (float) sin(_X);
//	}
//	float  __cdecl cosf(_In_ float _X)
//	{
//		return (float) cos(_X);
//	}
//	float  __cdecl atan2f(_In_ float _Y, _In_ float _X)
//	{
//		return (float) atan2(_Y, _X);
//	}
//#endif
//
//#if _M_ARM || NDEBUG
//	__inline HMODULE  get_krnl_addr()
//	{
//		void* teb;
//		DWORD* peb;
//		DWORD* pebldr;
//		DWORD* ioml;
//		teb = NtCurrentTeb();
//		peb = (DWORD*) *(DWORD*) ((char*) teb + 0x30);
//		pebldr = (DWORD*) *(DWORD*) ((char*) peb + 0xc);
//		ioml = (DWORD*) *(DWORD*) ((char*) pebldr + 0x1c);
//		return (HMODULE) *(DWORD*) (*ioml + 8);
//	}
//
//	uintptr_t __cdecl _beginthreadex(_In_opt_ void * _Security, _In_ unsigned _StackSize,
//		_In_ unsigned(__stdcall * _StartAddress) (void *), _In_opt_ void * _ArgList,
//		_In_ unsigned _InitFlag, _Out_opt_ unsigned * _ThrdAddr)
//	{
//		HMODULE kernel = get_krnl_addr();
//		typedef HMODULE(WINAPI* LL)(_In_opt_ LPSECURITY_ATTRIBUTES lpThreadAttributes, _In_ SIZE_T dwStackSize, _In_ LPTHREAD_START_ROUTINE lpStartAddress, _In_opt_ LPVOID lpParameter, _In_ DWORD dwCreationFlags, _Out_opt_  LPDWORD lpThreadId);
//		return (uintptr_t) ((LL) GetProcAddress(kernel, "CreateThread"))((LPSECURITY_ATTRIBUTES) _Security, _StackSize, (LPTHREAD_START_ROUTINE) _StartAddress, (LPVOID) _ArgList, _InitFlag, (LPDWORD) _ThrdAddr);
//	}
//#endif
//
//#if WINAPI_FAMILY==WINAPI_FAMILY_PHONE_APP
//
//#pragma warning
//	int __imp_CreateSemaphoreA()
//	{
//		return 0;
//	}
//
//	int __imp_GetProcessAffinityMask()
//	{
//		return 0;
//	}
//
//
//#endif
//
//
//}