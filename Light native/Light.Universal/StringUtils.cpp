#include "pch.h"
using namespace Microsoft::WRL;

HSTRING __fastcall utf8ToHString(char* str)
{
	if (*str == '\0')
		return nullptr;
	HSTRING hString = nullptr;
	HRESULT hr = ERROR_SUCCESS;

	int cch =
		::MultiByteToWideChar(
			CP_UTF8, MB_ERR_INVALID_CHARS,
			str, -1, nullptr, 0);

	if (cch == 0) hr = HRESULT_FROM_WIN32(GetLastError());

	WCHAR *charBuffer = nullptr;
	HSTRING_BUFFER bufferHandle = nullptr;
	if (SUCCEEDED(hr))
	{
		hr = WindowsPreallocateStringBuffer(cch - 1, &charBuffer, &bufferHandle);
	}

	if (SUCCEEDED(hr))
	{
		cch =
			::MultiByteToWideChar(
				CP_UTF8, MB_ERR_INVALID_CHARS,
				str, -1, charBuffer, cch);

		if (cch == 0) hr = HRESULT_FROM_WIN32(GetLastError());
	}

	if (SUCCEEDED(hr))
	{
		hr = WindowsPromoteStringBuffer(bufferHandle, &hString);
	}

	if (FAILED(hr))
	{
		if (bufferHandle != nullptr)
			WindowsDeleteStringBuffer(bufferHandle);
		return ::Platform::StringReference(L"").GetHSTRING();
	}

	return hString;
}

std::unique_ptr<char[]> __fastcall hStringToUtf8(HSTRING str)
{
	HRESULT hr = ERROR_SUCCESS;

	UINT32 strLength = 0;
	const WCHAR *strBuffer =
		WindowsGetStringRawBuffer(str, &strLength);

	int cch =
		::WideCharToMultiByte(
			CP_UTF8, WC_ERR_INVALID_CHARS,
			strBuffer, strLength, nullptr, 0,
			nullptr, nullptr);

	if (cch == 0 && strLength) hr = HRESULT_FROM_WIN32(GetLastError());

	std::unique_ptr<char[]> charBuffer;

	if (SUCCEEDED(hr))
	{
		charBuffer = std::make_unique<char[]>(cch + 1);

		cch =
			::WideCharToMultiByte(
				CP_UTF8, WC_ERR_INVALID_CHARS,
				strBuffer, strLength,
				charBuffer.get(), cch,
				nullptr, nullptr);

		if (cch == 0 && strLength) hr = HRESULT_FROM_WIN32(GetLastError());
	}

	if (FAILED(hr))
	{
		charBuffer.reset();
		return std::make_unique<char[]>(0);
	}

	return std::move(charBuffer);
}

class unicode_conversion_error : public std::runtime_error {
public:
	unicode_conversion_error(const char *what) : std::runtime_error(what) {}
	unicode_conversion_error() : std::runtime_error("Can not convert string to Unicode") {}
};

static bool utf8_check_continuation(const std::string &utf8str, size_t start, size_t check_length) {
	if (utf8str.size() > start + check_length) {
		while (check_length--)
			if ((uint8_t(utf8str[++start]) & 0xc0) != 0x80)
				return false;
		return true;
	}
	else
		return false;
}

std::wstring utf8_to_wide(const char* utf8str, bool strict) {
	std::wstring widestr;
	size_t i = 0;
	size_t utf8length = strlen(utf8str);
	widestr.reserve(utf8length);
	while (i < utf8length) {
		if (uint8_t(utf8str[i]) < 0x80) {
			widestr.push_back(utf8str[i]);
			++i;
			continue;
		}
		else if (uint8_t(utf8str[i]) < 0xc0) {
		}
		else if (uint8_t(utf8str[i]) < 0xe0) {
			if (utf8_check_continuation(utf8str, i, 1)) {
				uint32_t ucs4 = uint32_t(utf8str[i] & 0x1f) << 6 | uint32_t(utf8str[i + 1] & 0x3f);
				if (ucs4 >= 0x80) {
					widestr.push_back(wchar_t(ucs4));
					i += 2;
					continue;
				}
			}
		}
		else if (uint8_t(utf8str[i]) < 0xf0) {
			if (utf8_check_continuation(utf8str, i, 2)) {
				uint32_t ucs4 = uint32_t(utf8str[i] & 0xf) << 12 | uint32_t(utf8str[i + 1] & 0x3f) << 6 | (utf8str[i + 2] & 0x3f);
				if (ucs4 >= 0x800 && (ucs4 & 0xf800) != 0xd800) {
					widestr.push_back(wchar_t(ucs4));
					i += 3;
					continue;
				}
			}
		}
		else if (uint8_t(utf8str[i]) < 0xf8) {
			if (utf8_check_continuation(utf8str, i, 3)) {
				uint32_t ucs4 = uint32_t(utf8str[i] & 0x7) << 18 | uint32_t(utf8str[i + 1] & 0x3f) << 12 | uint32_t(utf8str[i + 2] & 0x3f) << 6 | uint32_t(utf8str[i + 3] & 0x3f);
				if (ucs4 >= 0x10000 && ucs4 < 0x110000) {
					//if (sizeof(wchar_t) >= 4)
					//	widestr.push_back(wchar_t(ucs4));
					//else {
					ucs4 -= 0x10000;
					widestr.append({
						wchar_t(ucs4 >> 10 | 0xd800),
						wchar_t((ucs4 & 0x3ff) | 0xdc00)
					});
					//}
					i += 4;
					continue;
				}
			}
		}
		if (strict)
			throw unicode_conversion_error();
		else {
			widestr.push_back(0xfffd);
			++i;
		}
	}
	widestr.shrink_to_fit();
	return widestr;
}