#include "pch.h"


// stub

extern "C" void _declspec(dllexport) InitializeChardetAPI() {
	return;
}

extern "C" int _declspec(dllexport)	ChardetDetectBytes(byte* text, int length, byte* codePage) {
	return 0;
}