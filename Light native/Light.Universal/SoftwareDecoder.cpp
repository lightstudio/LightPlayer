#include "pch.h"
#ifdef ENABLE_LEGACY
extern "C"
{
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}
#include "event_ref.h"
#include "SoftwareDecoder.h"
#include "Tag.h"
#include "AppValidate.h"
using namespace Light;
using namespace Platform;

void SoftwareDecoder::InitializeAll()
{
	//ValidateLicense
	av_register_all();
}

inline std::wstring ToLower(const wchar_t* ch)
{
	std::wstring ret = std::wstring(ch);
	for (int i = 0; i<ret.length(); i++)
	{
		if (ret[i] >= L'A'&&ret[i] <= L'Z')
		{
			ret[i] -= L'A' - L'a';
		}
	}
	return ret;
}

inline Platform::String^ utf8toPlatformString(char* str)
{
	auto st = string(str);
	auto wstr = s2ws(st, true);
	auto wstr_d = wstr.data();
	return ref new Platform::String(wstr_d);
}

MediaStreamSource^ SoftwareDecoder::GetMediaStreamSourceByFile(IStorageFile^ storageFile)
{
#if _M_ARM
	//use managed ogg decoder instead.
	if (ToLower(storageFile->FileType->Data()) == L".ogg")
	{
		Light::dotNetTools::ManagedVorbisDecoder^ vorbis = ref new Light::dotNetTools::ManagedVorbisDecoder();
		vorbis->InitFile(AWait(storageFile->OpenReadAsync()));
		vorbis->InitDecoder();
		return (ref new Light::dotNetTools::ManagedMSSHandler(vorbis))->MSS;
	}
#endif
	event_ref^ evref = ref new event_ref();
	_m_evref* mEvref = new _m_evref();
	mEvref->Content = evref;
	evref->set_selfref(mEvref);
	evref->stream = AWait(storageFile->OpenReadAsync());
	auto io = avio_alloc_context((unsigned char*) av_malloc(4096), 4096, 0, mEvref, &SoftwareDecoder::read_packet, &SoftwareDecoder::write_packet, &SoftwareDecoder::seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	auto ret = 0;
	if (ret = avformat_open_input(&pFormatContext, ""/*ws2s(wstring(storageFile->Name->Data())).data()*/, NULL, NULL))
		throw Exception::CreateException(E_FAIL, L"Ffmpeg Initialize failed");
	if ((ret = avformat_find_stream_info(pFormatContext, NULL)) < 0)
		throw Exception::CreateException(E_FAIL, L"Ffmpeg stream info not found");
	auto nAudioStream = -1;
	for (int i = 0; i < (int) pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO)
		{
			nAudioStream = i;
			break;
		}
	}
	if (nAudioStream == -1)
		throw Exception::CreateException(E_FAIL, L"No Audio track found!");
	auto pCodeContext = pFormatContext->streams[nAudioStream]->codec;

	av_seek_frame(pFormatContext, nAudioStream, 0, 0);


	int r = max(pCodeContext->bits_per_coded_sample, pCodeContext->bits_per_raw_sample);
	if (r == 0)
		r = 16;
	auto props = AudioEncodingProperties::CreatePcm((unsigned int) pCodeContext->sample_rate, (unsigned int) pCodeContext->channels, (unsigned int) r);

	auto audioDescriptor = ref new AudioStreamDescriptor(props);
	auto mss = ref new MediaStreamSource(audioDescriptor);
	mss->CanSeek = true;
	
	//auto metadata = pFormatContext->metadata;
	//auto eTitle = utf8toPlatformString(av_dict_get(metadata, "title", NULL, 0)->value);
	MusicTag^ tag = ref new MusicTag(storageFile);
	//call tag lib to get tags
	mss->MusicProperties->Album = tag->Album;
	mss->MusicProperties->Artist = tag->Artist;
	mss->MusicProperties->Title = tag->Title;
	mss->MusicProperties->Year = tag->Year;
	mss->MusicProperties->TrackNumber = tag->TrackNumber;
	auto duration = Windows::Foundation::TimeSpan();
	duration.Duration = (pFormatContext->streams[nAudioStream]->duration * av_q2d(pFormatContext->streams[nAudioStream]->time_base) * 1000L) * 10000L;
	mss->Duration = duration;

	auto pCodec = avcodec_find_decoder(pCodeContext->codec_id);
	if (!pCodec)
		throw Exception::CreateException(E_FAIL, L"No Available Codec Found!");
	if (ret = avcodec_open2(pCodeContext, pCodec, NULL) < 0)
		throw Exception::CreateException(E_FAIL, L"Codec Not supported");
	evref->register_mss(mss, duration.Duration, r);
	evref->register_av(pFormatContext, pCodeContext, nAudioStream);
	return mss;
}

MediaStreamSource^ SoftwareDecoder::GetMediaStreamSourceByStream(IRandomAccessStream^ stream, Platform::String^ extension)
{
	event_ref^ evref = ref new event_ref();
	_m_evref* mEvref = new _m_evref();
	mEvref->Content = evref;
	evref->set_selfref(mEvref);
	evref->stream = stream;
	auto io = avio_alloc_context((unsigned char*) av_malloc(4096), 4096, 0, mEvref, &SoftwareDecoder::read_packet, &SoftwareDecoder::write_packet, &SoftwareDecoder::seek);

	auto pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	auto ret = 0;
	if (ret = avformat_open_input(&pFormatContext, "", NULL, NULL))
		throw Exception::CreateException(E_FAIL, L"Ffmpeg Initialize failed");
	if ((ret = avformat_find_stream_info(pFormatContext, NULL)) < 0)
		throw Exception::CreateException(E_FAIL, L"Ffmpeg stream info not found");
	auto nAudioStream = -1;
	for (int i = 0; i < (int) pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO)
		{
			nAudioStream = i;
			break;
		}
	}
	if (nAudioStream == -1)
		throw Exception::CreateException(E_FAIL, L"No Audio track found!");
	auto pCodeContext = pFormatContext->streams[nAudioStream]->codec;

	av_seek_frame(pFormatContext, nAudioStream, 0, 0);


	int r = max(pCodeContext->bits_per_coded_sample, pCodeContext->bits_per_raw_sample);
	if (r == 0)
		r = 16;
	auto props = AudioEncodingProperties::CreatePcm((unsigned int) pCodeContext->sample_rate, (unsigned int) pCodeContext->channels, (unsigned int) r);

	auto audioDescriptor = ref new AudioStreamDescriptor(props);
	auto mss = ref new MediaStreamSource(audioDescriptor);
	mss->CanSeek = true;

	//call tag lib to get tags
	MusicTag^ tag = ref new MusicTag(stream, extension);
	mss->MusicProperties->Album = tag->Album;
	mss->MusicProperties->Artist = tag->Artist;
	mss->MusicProperties->Title = tag->Title;
	mss->MusicProperties->Year = tag->Year;
	mss->MusicProperties->TrackNumber = tag->TrackNumber;
	auto duration = Windows::Foundation::TimeSpan();
	duration.Duration = (pFormatContext->streams[nAudioStream]->duration * av_q2d(pFormatContext->streams[nAudioStream]->time_base) * 1000L) * 10000L;
	mss->Duration = duration;

	auto pCodec = avcodec_find_decoder(pCodeContext->codec_id);
	if (!pCodec)
		throw Exception::CreateException(E_FAIL, L"No Available Codec Found!");
	if (ret = avcodec_open2(pCodeContext, pCodec, NULL) < 0)
		throw Exception::CreateException(E_FAIL, L"Codec Not supported");
	evref->register_mss(mss, duration.Duration, r);
	evref->register_av(pFormatContext, pCodeContext, nAudioStream);
	return mss;
}

int SoftwareDecoder::read_packet(void *opaque, uint8_t *buf, int buf_size)
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

int SoftwareDecoder::write_packet(void *opaque, uint8_t *buf, int buf_size)
{
	//will not use this function
	return 0;
}

int64_t SoftwareDecoder::seek(void *opaque, int64_t offset, int whence)
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
#endif