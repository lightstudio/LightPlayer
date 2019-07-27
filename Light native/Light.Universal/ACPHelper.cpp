#include "pch.h"
#ifdef ENABLE_LEGACY
#include "config.h"
#include "ACPHelper.h"

using namespace Light;

extern bool UAPILoaded;
int (*pGetACP)();

void ACPHelper::CheckOrInitEnv()
{
	if (!UAPILoaded)
	{
		InitUAPIs();
		if (!UAPILoaded)
		{
			throw Platform::COMException::CreateException(-1, L"Cannot load restricted API from kernel");
		}
	}
	auto kernel = LoadLibraryW(L"kernelbase.dll");
	if (!kernel)
		throw Platform::COMException::CreateException(-1, L"cannot load kernelbase.dll");
	pGetACP = (int(*)())GetProcAddress(kernel, "GetACP");
	if (!pGetACP)
		throw Platform::COMException::CreateException(-1, L"cannot load GetACP function");
}

int ACPHelper::GetACP()
{
	if (pGetACP)
		return pGetACP();
	else
		//not loaded
		return -1;
}

// Ref: http://blogs.msdn.com/b/wsdevsol/archive/2014/03/10/how-to-consume-web-response-with-non-utf8-charset-on-windows-phone-8.aspx
Platform::String^ StringConverter::GetUnicodeString(UINT CodePage, const Platform::Array<byte, 1>^ input)
{
	Platform::String^ szOutput;
	WCHAR* output = NULL;
	int cchRequiredSize = 0;
	unsigned int cchActualSize = 0;

	cchRequiredSize = MultiByteToWideChar(CodePage, 0, (char*)input->Data, input->Length, output, cchRequiredSize); // determine required buffer size

	output = (WCHAR*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (cchRequiredSize + 1)*sizeof(wchar_t)); // fix: add 1 to required size and zero memory on alloc
	cchActualSize = MultiByteToWideChar(CodePage, 0, (char*)input->Data, input->Length, output, cchRequiredSize);

	if (cchActualSize > 0)
	{
		szOutput = ref new Platform::String(output);
	}
	else
	{
		szOutput = ref new Platform::String();
	}
	HeapFree(GetProcessHeap(), 0, output);  // fix: release buffer reference to fix memory leak.
	return szOutput;
}
#endif