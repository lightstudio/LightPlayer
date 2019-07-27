#include "pch.h"
#include <Windows.h>
#include "IMediaInfo.h"
#include "BuiltInCodec\FfmpegMediaInfo.h"
#include "BuiltInCodec\FfmpegFileIO.h"
#include "BuiltInCodec\StringUtils.h"
#include "InternalByteBuffer.h"
#include "Interop.h"
#include <shcore.h>
#include "AsyncHelper.h"

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}


using namespace Platform;
using namespace Light;
using namespace Light::BuiltInCodec;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

enum FileType {
	OTHER_FILE = 0,
	ALAC_FILE = 10,
	AAC_FILE = 20,
	MP3_FILE = 30,
	WMA_FILE = 40,
	APE_FILE = 50,
	FLAC_FILE = 60,
	WAV_FILE = 70,
	MP2_FILE = 80
};

inline HRESULT InternalGetMediaInfo(
	AVIOContext* io, 
	IMediaInfo^* out_mediaInfo) {
	int ret = 0;
	if (!io)
		return E_FAIL;
	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	if (ret = avformat_open_input(&pFormatContext, "", NULL, NULL) || 
		((ret = avformat_find_stream_info(pFormatContext, NULL)) < 0)) {
		avformat_close_input(&pFormatContext);
		return E_FAIL;
	}

	auto metadata = pFormatContext->metadata;

	auto _info = ref new FfmpegMediaInfo();
	AVStream *pAudioStream = nullptr;
	for (int i = 0; i < (int)pFormatContext->nb_streams; i++) {
		if (pFormatContext->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO) {
			pAudioStream = pFormatContext->streams[i];
			break;
		}
	}
	if (metadata == nullptr && 
		pAudioStream != nullptr) {
		metadata = pAudioStream->metadata;
	}
	if (!pAudioStream) {
		avformat_close_input(&pFormatContext);
		return E_FAIL;
	}

	auto duration = 
		(__int64)((pAudioStream->duration * av_q2d(pAudioStream->time_base) * 1000L) * 10000L);
	_info->Initialize(duration, metadata);

	*out_mediaInfo = _info;

	avformat_close_input(&pFormatContext);
	return S_OK;
}

inline HRESULT InternalGetAudioFormat(
	AVIOContext* io,
	int* AudioFormat) {
	int ret = 0;
	if (!io)
		return E_FAIL;
	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	if (ret = avformat_open_input(&pFormatContext, "", NULL, NULL)) {
		avformat_close_input(&pFormatContext);
		return E_FAIL;
	}
	auto nAudioStream = -1;
	for (int i = 0; i < (int)pFormatContext->nb_streams; i++) {
		if (pFormatContext->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO) {
			nAudioStream = i;
			break;
		}
	}
	if (nAudioStream == -1) {
		avformat_close_input(&pFormatContext);
		return E_FAIL;
	}
	auto pCodeContext = pFormatContext->streams[nAudioStream]->codec;

	av_seek_frame(pFormatContext, nAudioStream, 0, 0);
	auto pCodec = avcodec_find_decoder(pCodeContext->codec_id);
	if (!pCodec) {
		avformat_close_input(&pFormatContext);
		return E_FAIL;
	}
	int val = OTHER_FILE;
	const char *codecName = pCodec->name;
	if (!strcmp(codecName, "aac"))
		val = AAC_FILE;
	else if (!strcmp(codecName, "alac"))
		val = ALAC_FILE;
	else if (!strcmp(codecName, "mp3"))
		val = MP3_FILE;
	else if (!strcmp(codecName, "ape"))
		val = APE_FILE;
	else if (!strcmp(codecName,"flac"))
		val = FLAC_FILE;
	else if (!strcmp(codecName, "wma"))
		val = WMA_FILE;
	else if (!strncmp(codecName, "pcm", 3))
		val = WAV_FILE;
	*AudioFormat = val;
	avformat_close_input(&pFormatContext);
	return S_OK;
}

HRESULT WINAPI GetMediaInfoFromStream(
	IRandomAccessStream^ stream,
	IMediaInfo^* out_mediaInfo) {
	Microsoft::WRL::ComPtr<IStream> fileStreamData;
	HRESULT hr = CreateStreamOverRandomAccessStream(
		reinterpret_cast<IUnknown*>(stream), IID_PPV_ARGS(&fileStreamData));
	if (!SUCCEEDED(hr))
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*) av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0,
		fileStreamData.Get(),
		IStreamRead, 0,
		IStreamSeek);
	auto ret = InternalGetMediaInfo(io, out_mediaInfo);
	av_freep(&io->buffer);
	av_freep(&io);
	return ret;
}

HRESULT WINAPI GetMediaInfoFromFilePath(
	LPCWSTR Path,
	IMediaInfo^* out_mediaInfo) {
	auto file = _wfopen(Path, L"rb");
	if (!file)
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*) av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0, file,
		&crt_read_packet, 
		&crt_write_packet, 
		&crt_seek);
	auto ret = InternalGetMediaInfo(io, out_mediaInfo);
	fclose(file);
	av_freep(&io->buffer);
	av_freep(&io);
	return ret;
}

HRESULT WINAPI GetAudioFormatFromStream(
	IRandomAccessStream^ stream,
	int* AudioFormat) {
	IStream* fileStreamData;
	HRESULT hr = CreateStreamOverRandomAccessStream(
		reinterpret_cast<IUnknown*>(stream), IID_PPV_ARGS(&fileStreamData));
	if (!SUCCEEDED(hr))
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*)av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0,
		fileStreamData,
		IStreamRead, 0,
		IStreamSeek);
	auto ret = InternalGetAudioFormat(io, AudioFormat);
	av_freep(&io->buffer);
	av_freep(&io);
	fileStreamData->Release();
	return ret;
}

HRESULT WINAPI GetAudioFormatFromFilePath(
	LPCWSTR Path,
	int* AudioFormat) {
	auto file = _wfopen(Path, L"rb");
	if (!file)
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*)av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0, file,
		&crt_read_packet,
		&crt_write_packet,
		&crt_seek);
	auto ret = InternalGetAudioFormat(io, AudioFormat);
	fclose(file);
	av_freep(&io->buffer);
	av_freep(&io);
	return ret;
}

HRESULT WINAPI InitializeFfmpeg() {
	av_register_all();
	return S_OK;
}

void Interop::InitializeFfmpeg() {
	av_register_all();
}

int Interop::GetMediaInfoFromStream(IRandomAccessStream^ stream, IMediaInfo^* out_mediaInfo) {
	IStream* fileStreamData;
	HRESULT hr = CreateStreamOverRandomAccessStream(
		reinterpret_cast<IUnknown*>(stream), IID_PPV_ARGS(&fileStreamData));
	if (!SUCCEEDED(hr))
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*)av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0,
		fileStreamData,
		IStreamRead, 0,
		IStreamSeek);
	auto ret = InternalGetMediaInfo(io, out_mediaInfo);
	av_freep(&io->buffer);
	av_freep(&io);
	fileStreamData->Release();
	return ret;
}

int Interop::GetMediaInfoFromFilePath(String^ Path, IMediaInfo^* out_mediaInfo) {
	auto file = _wfopen(Path->Data(), L"rb");
	if (!file)
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*)av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0, file,
		&crt_read_packet,
		&crt_write_packet,
		&crt_seek);
	auto ret = InternalGetMediaInfo(io, out_mediaInfo);
	fclose(file);
	av_freep(&io->buffer);
	av_freep(&io);
	return ret;
}

inline HRESULT InternalGetAlbumCover(
	AVIOContext* io,
	IRandomAccessStream^* out_stream) {
	int ret = 0;
	if (!io)
		return E_FAIL;
	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	if (ret = avformat_open_input(&pFormatContext, "", NULL, NULL) ||
		((ret = avformat_find_stream_info(pFormatContext, NULL)) < 0)) {
		avformat_close_input(&pFormatContext);
		return E_FAIL;
	}
	for (unsigned int i = 0; i < pFormatContext->nb_streams; i++) {
		if (pFormatContext->streams[i]->disposition & AV_DISPOSITION_ATTACHED_PIC) {
			AVPacket pkt = pFormatContext->streams[i]->attached_pic;
			auto stream = ref new InMemoryRandomAccessStream();
			auto writer = ref new DataWriter(stream);
			writer->WriteBytes(Platform::ArrayReference<BYTE>(pkt.data, pkt.size));
			AWait(writer->StoreAsync());
			AWait(writer->FlushAsync());
			writer->DetachStream();
			stream->Seek(0);
			*out_stream = stream;
			delete writer;
			break;
		}
	}
	avformat_close_input(&pFormatContext);
	return S_OK;
}

HRESULT WINAPI GetAlbumCoverFromStream(
	Windows::Storage::Streams::IRandomAccessStream^ stream,
	Windows::Storage::Streams::IRandomAccessStream^* out_stream) {
	IStream* fileStreamData;
	HRESULT hr = CreateStreamOverRandomAccessStream(
		reinterpret_cast<IUnknown*>(stream), IID_PPV_ARGS(&fileStreamData));
	if (!SUCCEEDED(hr))
		return E_FAIL;
	auto io = avio_alloc_context(
		(unsigned char*)av_malloc(FS_BUFFER_SIZE_SCAN),
		FS_BUFFER_SIZE_SCAN, 0,
		fileStreamData,
		IStreamRead, 0,
		IStreamSeek);

	auto ret = InternalGetAlbumCover(io, out_stream);

	av_freep(&io->buffer);
	av_freep(&io);
	fileStreamData->Release();
	return ret;
}