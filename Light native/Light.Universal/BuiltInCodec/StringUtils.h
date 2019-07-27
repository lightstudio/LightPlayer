#pragma once 
#include <stdio.h>
#include <Windows.h>
#include <string>
#include <memory>
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}
using namespace Platform;

std::wstring utf8_to_wide(const char* utf8str, bool strict);
HSTRING __fastcall utf8ToHString(char* str);
std::unique_ptr<char[]> __fastcall hStringToUtf8(HSTRING str);

inline ptrdiff_t indexOf(const char* str, const char ch) {
	register const char* ptr = str;
    
	while (*ptr)
	{
		if (*ptr == ch)
			return ptr - str;
		ptr++;
	}
	/*
	while (*ptr && *ptr != ch)
		ptr++;

	if (*ptr == (char)ch)
		return (ptr - str);
	*/
	return -1;
}

inline void substring(char* dest, const char* src, ptrdiff_t length) {
	strncpy(dest, src, (size_t)length);
	dest[length] = '\0';
}

inline void substring(char* dest, const char* src, size_t srcSize, ptrdiff_t from) {
	strncpy(dest, src + from, srcSize - (size_t)from);
}

__forceinline String^ utf8ToString(char* str)
{
	return reinterpret_cast<String^>(utf8ToHString(str));
}

__forceinline std::unique_ptr<char[]> stringToUtf8(::Platform::String ^str)
{
	return hStringToUtf8(reinterpret_cast<HSTRING>(str));
}

__forceinline String^ getDictValue(AVDictionary* dict, const char* key) {
	auto entry = av_dict_get(dict, key, NULL, 0);
	if (!entry)
		return L"";
	return utf8ToString(entry->value);
}

inline void splitDictValue(AVDictionary* dict, const char* key, String^& value1, String^& value2) {
	value1 = value2 = nullptr;

	auto entry = av_dict_get(dict, key, NULL, 0);
	if (!entry) return;

	auto len = strlen(entry->value);
    ptrdiff_t pos;
	if ((pos = indexOf(entry->value, '/')) > -1)
	{
		auto buffer = std::make_unique<char[]>(len);

		substring(buffer.get(), entry->value, pos);
		value1 = utf8ToString(buffer.get());

		substring(buffer.get(), entry->value, len, pos + 1);
		value2 = utf8ToString(buffer.get());
	}
	else value1 = utf8ToString(entry->value);
}