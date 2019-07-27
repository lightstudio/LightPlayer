#pragma once


extern "C" void __declspec(dllexport) WINAPI InitializeChardetAPI();

extern "C" int __declspec(dllexport) WINAPI ChardetDetectText(const char* text, int length, char* codePage);

extern "C" int __declspec(dllexport) WINAPI ChardetDetectBytes(unsigned char* text, int length, char* codePage);