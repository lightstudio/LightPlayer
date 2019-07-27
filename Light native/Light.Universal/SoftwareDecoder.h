#pragma once
#ifdef ENABLE_LEGACY
#include "InternalByteBuffer.h"
#include "AsyncHelper.h"
#include "StringHelper.h"
#include "MusicTrack.h"

using namespace Windows::Media;
using namespace Windows::Media::Core;
using namespace Windows::Media::MediaProperties;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

namespace Light
{
	[Windows::Foundation::Metadata::DeprecatedAttribute(L"This class is deprecated. Use Light.BuiltInCodec.FfmpegCodec instead.", Windows::Foundation::Metadata::DeprecationType::Deprecate, 1)]
	public ref class SoftwareDecoder sealed
	{
	public:
		static void InitializeAll();
		static MediaStreamSource^ GetMediaStreamSourceByFile(IStorageFile^ storageFile);
		static MediaStreamSource^ GetMediaStreamSourceByStream(IRandomAccessStream^ stream, Platform::String^ extension);
	private:
		SoftwareDecoder() { }
		static int read_packet(void *opaque, uint8_t *buf, int buf_size);
		static int write_packet(void *opaque, uint8_t *buf, int buf_size);
		static int64_t seek(void *opaque, int64_t offset, int whence);
	};
}
#endif