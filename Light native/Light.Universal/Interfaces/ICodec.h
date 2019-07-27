#pragma once

namespace Light {
	//[uuid(9585BC1E-EF3A-4865-BD88-8856731A8013)]
	public interface class ICodec {
		IMediaFile^ LoadFromFile(Windows::Storage::IStorageFile^ file);
		IMediaFile^ LoadFromStream(Windows::Storage::Streams::IRandomAccessStream^ stream);
		property Platform::Array<Platform::String^>^ SupportedFormats {
			Platform::Array<Platform::String^>^ get();
		}
	};
}