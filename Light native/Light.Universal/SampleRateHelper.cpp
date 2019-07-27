#include <Windows.h>
#include "SampleRateHelper.h"
#include "AsyncHelper.h"

using namespace Windows::Media::Audio;
using namespace Windows::Media::Render;

int systemSampleRate = 0;

extern "C" void _declspec(dllexport) SetSystemSampleRate(int value) {
	systemSampleRate = value;
}
