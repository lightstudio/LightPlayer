#pragma once

namespace Light {
    namespace BuiltInCodec {
        public ref class Interop sealed {
        public:
            static void InitializeFfmpeg();
            static int GetMediaInfoFromFilePath(Platform::String^ Path, Light::IMediaInfo^* out_mediaInfo);
            static int GetMediaInfoFromStream(Windows::Storage::Streams::IRandomAccessStream^ stream,
                Light::IMediaInfo^* out_mediaInfo);
        };
    }
}

extern "C"
HRESULT
__declspec(dllexport)
WINAPI InitializeFfmpeg();

extern "C"
HRESULT
__declspec(dllexport)
WINAPI GetMediaInfoFromFilePath(
    LPCWSTR Path,
    Light::IMediaInfo^* out_mediaInfo);

extern "C"
HRESULT
__declspec(dllexport)
WINAPI GetMediaInfoFromStream(
    Windows::Storage::Streams::IRandomAccessStream^ stream,
    Light::IMediaInfo^* out_mediaInfo);

extern "C"
HRESULT
__declspec(dllexport)
WINAPI GetAudioFormatFromStream(
    Windows::Storage::Streams::IRandomAccessStream^ stream,
    int* AudioFormat);

extern "C"
HRESULT
__declspec(dllexport)
WINAPI GetAudioFormatFromFilePath(
    LPCWSTR Path,
    int* AudioFormat);

extern "C"
HRESULT
__declspec(dllexport)
WINAPI GetAlbumCoverFromStream(
    Windows::Storage::Streams::IRandomAccessStream^ stream,
    Windows::Storage::Streams::IRandomAccessStream^* out_stream);