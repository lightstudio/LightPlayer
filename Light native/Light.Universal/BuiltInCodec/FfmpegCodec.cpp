#include "pch.h"

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswresample/swresample.h>
}
#include <wrl.h>
#include <queue>
using namespace Microsoft::WRL;
#include "IMediaInfo.h"
#include "AudioIndexCue.h"
#include "IMediaFile.h"
#include "FfmpegMediaInfo.h"
#include "PcmSampleInfo.h"
#include "FfmpegAudioReader.h"
#include "FfmpegMediaFile.h"
#include "FfmpegCodec.h"
#include "AsyncHelper.h"

#define IO_USE_WINRT_FILE_IO
#define PLATFORM_WINDOWS

#ifdef ENABLE_LEGACY_APE
#include "All.h"
#include "WinRTPlatformIO.h"
#include "MACLib.h"
#include "APETag.h"
#include "APEMediaFile.h"
#endif
#include "InternalByteBuffer.h"

using namespace Light;
using namespace Light::BuiltInCodec;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Platform;


FfmpegCodec::FfmpegCodec() {
	av_register_all();
}

IMediaFile^ FfmpegCodec::LoadFromFile(IStorageFile^ file) {
	return ref new FfmpegMediaFile(file);
}

IMediaFile^ FfmpegCodec::LoadFromStream(IRandomAccessStream^ stream) {
	return ref new FfmpegMediaFile(stream);
}

Array<String^>^ FfmpegCodec::SupportedFormats::get() {
	String^ formats[] = { L"mp3",L"aac",L"m4a",L"mp4",L"flac",L"alac",L"ogg",L"wav",L"tak",L"tta",L"ape" };
	return ref new Array<String^>(formats,10);
}
