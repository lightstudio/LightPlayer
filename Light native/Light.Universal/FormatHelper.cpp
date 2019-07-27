#include "pch.h"
#ifdef ENABLE_LEGACY
#include <Windows.h>
#include "event_ref.h"
#include "AsyncHelper.h"

extern "C"
{
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}

enum FileType
{
	INVALID_FILE = -10,
	OTHER_FILE = 0,
	ALAC_FILE = 10,
	AAC_FILE = 20,
	MP3_FILE = 30,
	WMA_FILE = 40,
	APE_FILE = 50,
	FLAC_FILE = 60,
	WAV_FILE = 70,
	//M4A_FILE = 80,
	MP2_FILE = 90
};
using namespace Windows::Storage::Streams;
using namespace Platform;

int read_packet(void *opaque, uint8_t *buf, int buf_size)
{
	Array<BYTE>^ buffer = ref new Array<BYTE>(buf_size);
	auto ref = ((_m_evref*) opaque)->Content;
	auto stream = ref->stream;
	auto reader = ref new DataReader(stream);
	auto bytes = AWait(reader->LoadAsync(buf_size));
	auto pBytes = ref new Array<unsigned char>(bytes);
	reader->ReadBytes(pBytes);
	reader->DetachStream();
	memcpy(buf, pBytes->Data, bytes);
	return bytes;
}

int write_packet(void *opaque, uint8_t *buf, int buf_size)
{
	//will not use this function
	return 0;
}

int64_t seek(void *opaque, int64_t offset, int whence)
{
	auto ref = ((_m_evref*) opaque)->Content;
	auto stream = ref->stream;
	if (whence == 65536)
		return stream->Size;
	else
	{
		switch (whence)
		{
		case 0:
			stream->Seek(offset);
			break;
		case 1:
			stream->Seek(stream->Position + offset);
			break;
		case 2:
			stream->Seek(stream->Size - offset);
			break;
		}
		return stream->Position;
	}
}

extern "C" int __declspec(dllexport) WINAPI GetAudioFormat(Windows::Storage::IStorageFile^ storageFile)
{
	auto stream = AWait(storageFile->OpenReadAsync());
	event_ref^ evref = ref new event_ref();
	_m_evref* mEvref = new _m_evref();
	mEvref->Content = evref;
	evref->set_selfref(mEvref);
	evref->stream = stream;
	auto io = avio_alloc_context((unsigned char*)av_malloc(4096), 4096, 0, mEvref, &read_packet, &write_packet, &seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	auto ret = 0;
	if (ret = avformat_open_input(&pFormatContext, ""/*ws2s(wstring(storageFile->Name->Data())).data()*/, NULL, NULL))
	{
		//av_close_input_file(pFormatContext);
		delete mEvref;
		delete stream;
		return INVALID_FILE;
	}
	if ((ret = avformat_find_stream_info(pFormatContext, NULL)) < 0)
	{
		delete mEvref;
		delete stream;
		return INVALID_FILE;
	}
	auto nAudioStream = -1;
	for (int i = 0; i < (int)pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO)
		{
			nAudioStream = i;
			break;
		}
	}
	if (nAudioStream == -1)
	{
		delete mEvref;
		delete stream;
		return INVALID_FILE;
	}
	auto pCodeContext = pFormatContext->streams[nAudioStream]->codec;

	av_seek_frame(pFormatContext, nAudioStream, 0, 0);
	auto pCodec = avcodec_find_decoder(pCodeContext->codec_id);
	int val = OTHER_FILE;
	if (!pCodec)
	{
		avformat_close_input(&pFormatContext);

		delete mEvref;
		delete stream;
		return INVALID_FILE;
	}
	else{
		if (pCodec->name == "aac")
		{
			val = AAC_FILE;
		}
		else if (pCodec->name == "alac")
		{
			val = ALAC_FILE;
		}
		else if (pCodec->name == "mp3")
		{
			val = MP3_FILE;
		}
		else if (pCodec->name == "ape")
		{
			val = APE_FILE;
		}
		else if (pCodec->name == "flac")
		{
			val = FLAC_FILE;
		}
		//else if (pCodec->name == "m4a")
		//	val = M4A_FILE;
		else if (pCodec->name == "pcm")
		{
			val = WAV_FILE;
		}
		else if (pCodec->name == "wma")
		{
			val = WMA_FILE;
		}
		else
		{
			val = OTHER_FILE;
		}
	}

	avformat_close_input(&pFormatContext);

	delete mEvref;
	delete stream;
	return val;
}

extern "C" int __declspec(dllexport) WINAPI IsALAC(Windows::Storage::IStorageFile^ storageFile)
{
	return GetAudioFormat(storageFile);
}

#endif