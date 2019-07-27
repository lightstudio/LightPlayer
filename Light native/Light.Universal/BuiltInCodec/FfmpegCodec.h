#pragma once
#include "ICodec.h"

namespace Light {
	namespace BuiltInCodec {
		//[uuid(2D10C61E-4F65-44BD-A40B-6AAB841DCA84)]
		public ref class FfmpegCodec sealed : public ICodec {
		public:
			FfmpegCodec();
			virtual IMediaFile^ LoadFromFile(Windows::Storage::IStorageFile^ file);
			virtual IMediaFile^ LoadFromStream(Windows::Storage::Streams::IRandomAccessStream^ stream);
			property Platform::Array<Platform::String^>^ SupportedFormats {
				virtual Platform::Array<Platform::String^>^ get();
			}
		};
	}
}


