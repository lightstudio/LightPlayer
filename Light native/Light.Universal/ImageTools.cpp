#include "pch.h"
#ifdef ENABLE_LEGACY
#include <Windows.h>
#include "event_ref.h"
#include "AsyncHelper.h"
#include "BuiltInCodec\FfmpegFileIO.h"
#include "InternalByteBuffer.h"

extern "C"
{
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}

using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Platform;

extern int read_packet(void *opaque, uint8_t *buf, int buf_size);
extern int write_packet(void *opaque, uint8_t *buf, int buf_size);
extern int64_t seek(void *opaque, int64_t offset, int whence);

extern "C" void __declspec(dllexport) WINAPI Free(void * buffer)
{
	if (buffer)
		free(buffer);
}

extern "C" void __declspec(dllexport) WINAPI GetAlbumCoverFromStream(IRandomAccessStream^ stream, IBuffer^* out_buffer)
{
	event_ref^ evref = ref new event_ref();
	_m_evref* mEvref = new _m_evref();
	mEvref->Content = evref;
	evref->set_selfref(mEvref);
	evref->stream = stream;
	auto io = avio_alloc_context((unsigned char*) av_malloc(4096), 4096, 0, mEvref, &read_packet, &write_packet, &seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;
	if (avformat_open_input(&pFormatContext, "", NULL, NULL) != 0)
	{
		av_free(io);
		av_free(pFormatContext);
		delete mEvref;
		delete stream;
		*out_buffer = nullptr;
		return;
	}

	// read the format headers
	if (pFormatContext->iformat->read_header(pFormatContext) < 0)
	{
		av_free(io);
		av_free(pFormatContext);
		delete mEvref;
		delete stream;
		*out_buffer = nullptr;
		return;
	}
	//The first image will be returned.
	for (int i = 0; i < pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->disposition & AV_DISPOSITION_ATTACHED_PIC)
		{
			AVPacket pkt = pFormatContext->streams[i]->attached_pic;

			byte* buf = new byte[pkt.size];
			memcpy(buf, pkt.data, pkt.size);
			Microsoft::WRL::ComPtr<InternalByteBuffer> buffer;
			Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&buffer, buf, pkt.size);

			av_free_packet(&pkt);
			*out_buffer = reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(buffer.Get());
			goto end;
		}
	}
	//or return nullptr
	*out_buffer = nullptr;
end:
	av_free(io);
	av_free(pFormatContext);
	//avformat_free_context(pFormatContext);
	//avformat_close_input(&pFormatContext);
	delete mEvref;
	delete stream;
	//_CrtDumpMemoryLeaks();
}

extern "C" void __declspec(dllexport) WINAPI GetAlbumCoverFromStream2(IRandomAccessStream^ stream, byte** out_buffer, int* out_length)
{
	event_ref^ evref = ref new event_ref();
	_m_evref* mEvref = new _m_evref();
	mEvref->Content = evref;
	evref->set_selfref(mEvref);
	evref->stream = stream;
	auto io = avio_alloc_context((unsigned char*)av_malloc(4096), 4096, 0, mEvref, &read_packet, &write_packet, &seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;
	if (avformat_open_input(&pFormatContext, "", NULL, NULL) != 0)
	{
		av_free(io);
		av_free(pFormatContext);
		delete mEvref;
		delete stream;
		*out_length = 0;
		*out_buffer = nullptr;
		return;
	}

	// read the format headers
	if (pFormatContext->iformat->read_header(pFormatContext) < 0)
	{
		av_free(io);
		av_free(pFormatContext);
		delete mEvref;
		delete stream;
		*out_length = 0;
		*out_buffer = nullptr;
		return;
	}
	//The first image will be returned.
	for (int i = 0; i < pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->disposition & AV_DISPOSITION_ATTACHED_PIC)
		{
			AVPacket pkt = pFormatContext->streams[i]->attached_pic;

			byte* buf = new byte[pkt.size];
			memcpy(buf, pkt.data, pkt.size);

			*out_buffer = buf;
			*out_length = pkt.size;
			av_free_packet(&pkt);
			goto end;
		}
	}
	//or return nullptr
	*out_length = 0;
	*out_buffer = nullptr;
end:
	av_free(io);
	av_free(pFormatContext);
	//avformat_free_context(pFormatContext);
	//avformat_close_input(&pFormatContext);
	delete mEvref;
	delete stream;
	//_CrtDumpMemoryLeaks();
}


extern "C" void __declspec(dllexport) WINAPI GetAlbumCoverFromFile(IStorageFile^ file, IBuffer^* buffer)
{
	GetAlbumCoverFromStream(AWait(file->OpenReadAsync()), buffer);
}

extern "C" void __declspec(dllexport) WINAPI GetAlbumCoverFromFile2(IStorageFile^ file, byte** out_buffer, int* out_length)
{
	GetAlbumCoverFromStream2(AWait(file->OpenReadAsync()), out_buffer, out_length);
}


extern "C" void __declspec(dllexport) WINAPI GetAlbumCoverFromPath(LPSTR path, IBuffer^* out_buffer)
{
	auto file = fopen(path, "rb");
	auto io = avio_alloc_context((unsigned char*)av_malloc(4096), 4096, 0, file, &crt_read_packet, &crt_write_packet, &crt_seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;
	if (avformat_open_input(&pFormatContext, "", NULL, NULL) != 0)
	{
		av_free(io);
		av_free(pFormatContext);
		*out_buffer = nullptr;
		fclose(file);
		return;
	}
	// read the format headers
	if (pFormatContext->iformat->read_header(pFormatContext) < 0)
	{
		av_free(pFormatContext);
		*out_buffer = nullptr;
		fclose(file);
		return;
	}
	//The first image will be returned.
	for (int i = 0; i < pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->disposition & AV_DISPOSITION_ATTACHED_PIC)
		{
			AVPacket pkt = pFormatContext->streams[i]->attached_pic;

			byte* buf = new byte[pkt.size];
			memcpy(buf, pkt.data, pkt.size);
			Microsoft::WRL::ComPtr<InternalByteBuffer> buffer;
			Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&buffer, buf, pkt.size);

			av_free_packet(&pkt);
			*out_buffer = reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(buffer.Get());
			av_free(pFormatContext);
			fclose(file);
			return;
		}
	}
	//or return nullptr
	*out_buffer = nullptr;
	av_free(pFormatContext);
	fclose(file);
}

extern "C" void __declspec(dllexport) WINAPI GetAlbumCoverFromPath2(LPSTR path, byte** out_buffer, int* out_length)
{
	auto file = fopen(path, "rb");
	auto io = avio_alloc_context((unsigned char*)av_malloc(4096), 4096, 0, file, &crt_read_packet, &crt_write_packet, &crt_seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;
	if (avformat_open_input(&pFormatContext, "", NULL, NULL) != 0)
	{
		av_free(io);
		av_free(pFormatContext);
		*out_length = 0;
		*out_buffer = nullptr;
		fclose(file);
		return;
	}
	// read the format headers
	if (pFormatContext->iformat->read_header(pFormatContext) < 0)
	{
		av_free(pFormatContext);
		*out_length = 0;
		*out_buffer = nullptr;
		fclose(file);
		return;
	}
	//The first image will be returned.
	for (int i = 0; i < pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->disposition & AV_DISPOSITION_ATTACHED_PIC)
		{
			AVPacket pkt = pFormatContext->streams[i]->attached_pic;

			byte* buf = new byte[pkt.size];
			memcpy(buf, pkt.data, pkt.size);

			*out_buffer = buf;
			*out_length = pkt.size;
			av_free_packet(&pkt);
			av_free(pFormatContext);
			fclose(file);
			return;
		}
	}
	//or return nullptr
	*out_length = 0;
	*out_buffer = nullptr;
	av_free(pFormatContext);
	fclose(file);
}
#endif