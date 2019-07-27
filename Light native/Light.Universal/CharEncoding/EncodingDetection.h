#pragma once

extern "C" __declspec(dllexport) HRESULT InitializeMLangAPI();

extern "C" __declspec(dllexport) HRESULT DetectCodepageText(char* text, int length, int* codePage);

extern "C" __declspec(dllexport) HRESULT DetectCodepageBytes(byte* text, int length, int* codePage);