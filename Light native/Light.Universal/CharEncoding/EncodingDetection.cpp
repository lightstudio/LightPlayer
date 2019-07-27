#include "pch.h"
#include <MLang.h>
#include "EncodingDetection.h"

IMultiLanguage2* iml = nullptr;


HRESULT InitializeMLangAPI() {
	auto hr = CoCreateInstance(CLSID_CMultiLanguage, NULL, CLSCTX_INPROC_SERVER, IID_IMultiLanguage2, (LPVOID*)&iml);
	if (!SUCCEEDED(hr))
		iml = nullptr;
	return hr;
}

HRESULT DetectCodepageText(const char* text, int length, int* codePage) {
	DetectEncodingInfo info;
	if (iml == nullptr)
		return E_FAIL;
	char* c = (char*)malloc(sizeof(char)*(length + 1));
	memcpy(c, text, length);
	c[length] = '\0';
	length++;
	int cnt = 1;
	auto hr = iml->DetectInputCodepage(0, 0, c, &length, &info, &cnt);
	if (SUCCEEDED(hr)) {
		iml->Release();
		*codePage = info.nCodePage;
	}
	free(c);
	return hr;
}

HRESULT DetectCodepageBytes(byte* textBytes, int length, int* codePage) {
	return DetectCodepageText(reinterpret_cast<char const*>(textBytes), length, codePage);
}