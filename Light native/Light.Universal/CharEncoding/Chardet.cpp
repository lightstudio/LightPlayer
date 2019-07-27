#include "pch.h"
#include "uchardet\uchardet.h"
#include "Chardet.h"

uchardet_t det;

void WINAPI InitializeChardetAPI() {
	det = uchardet_new();
}

int WINAPI ChardetDetectText(const char* text, int length, char* codePage) {
	int ret = uchardet_handle_data(det, text, length);
	uchardet_data_end(det);
	auto cp = uchardet_get_charset(det);
	strcpy(codePage, cp);
	uchardet_reset(det);
	return ret;
}

int WINAPI ChardetDetectBytes(unsigned char* text, int length, char* codePage) {
	return ChardetDetectText(reinterpret_cast<char const*>(text), length, codePage);
}