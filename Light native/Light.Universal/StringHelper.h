#pragma once
#ifdef ENABLE_LEGACY
#include <string>
using namespace std;


__inline wstring s2ws(const string& s, bool utf8 = false)
{
	int len;
	int slength = (int) s.length() + 1;
	if (utf8)
		len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), slength, 0, 0);
	else
		len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
	wchar_t* buf = new wchar_t[len];
	if (utf8)
		MultiByteToWideChar(CP_UTF8, 0, s.c_str(), slength, buf, len);
	else
		MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
	std::wstring r(buf);
	delete [] buf;
	return r;
}

__inline string ws2s(const wstring& ws)
{
	string curLocale = setlocale(LC_ALL, NULL);
	setlocale(LC_ALL, "");
	const wchar_t* _Source = ws.c_str();
	size_t _Dsize = 2 * ws.size() + 1;
	char *_Dest = new char[_Dsize];
	memset(_Dest, 0, _Dsize);
	wcstombs(_Dest, _Source, _Dsize);
	string result = _Dest;
	delete [] _Dest;
	setlocale(LC_ALL, curLocale.c_str());
	return result;
}
#endif