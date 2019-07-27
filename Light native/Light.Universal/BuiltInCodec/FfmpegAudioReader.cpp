#include "pch.h"
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswresample/swresample.h>
#include <libavutil/opt.h>
}
#include <shcore.h>
#include <mutex>
#include <queue>
#include <wrl.h>
#include <robuffer.h>
#include "StringUtils.h"
#include "AsyncHelper.h"
#include "Interfaces\IMediaInfo.h"
#include "FfmpegMediaInfo.h"
#include "AudioIndexCue.h"
#include "FfmpegFileIO.h"
#include "PcmSampleInfo.h"
#include "FfmpegAudioReader.h"
#include "InternalByteBuffer.h"
#include "NativeSettingsManager.h"
#include "SampleRateHelper.h"


using namespace Light::BuiltInCodec;
using namespace Platform;
using namespace Windows::Media::MediaProperties;
using namespace Windows::Media::Core;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;


FfmpegAudioReader::FfmpegAudioReader(IRandomAccessStream ^ stream)
{
	OpenStream(stream);
}

FfmpegAudioReader::~FfmpegAudioReader()
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndClearUnreadDelayedCodecBuffers();
	if (delayedStream && delayedStreamOpened)
	{
		ReleaseFfmpeg();
		fileStreamData->Release();
		delayedStreamOpened = false;
	}
	if (!delayedStream)
	{
		ReleaseFfmpeg();
		fileStreamData->Release();
	}
	if (streamData != nullptr)
	{
		delete streamData;
		streamData = nullptr;
	}
	if (swr_ctx)
	{
		swr_free(&swr_ctx);
	}
}

void FfmpegAudioReader::SetDefaultResampler()
{
	if (swr_ctx)
	{
		swr_free(&swr_ctx);
		_sampleInfo = nullptr;
	}

	int sampleRate = Light::NativeSettingsManager::Instance->PreferredSampleRate;

	if (sampleRate == 0) {
		sampleRate = systemSampleRate;
	}

	if (sampleRate == 0) {
		sampleRate = 192000;
	}

	_sampleInfo = ref new PcmSampleInfo(sampleRate, 2, 24);

	swr_ctx = swr_alloc_set_opts(NULL,
		AV_CH_LAYOUT_STEREO, AV_SAMPLE_FMT_S32, sampleRate,
		codec->channel_layout, codec->sample_fmt, codec->sample_rate,
		0, NULL);

	auto result = swr_init(swr_ctx);
	if (result < 0)
	{
		char errstr[100];
		av_strerror(result, errstr, 100);
		throw ref new Platform::Exception(E_FAIL);
	}
}

void FfmpegAudioReader::SetResampleTarget(PcmSampleInfo ^ sample)
{
	if (swr_ctx)
	{
		swr_free(&swr_ctx);
		_sampleInfo = nullptr;
	}
	_sampleInfo = sample;
	if (delayedStream && !delayedStreamOpened)
		return;
	if (sample)
	{
		swr_ctx = swr_alloc();
		auto src_ch_layout =
			(codec->channel_layout &&
				codec->channels ==
				av_get_channel_layout_nb_channels(codec->channel_layout)) ?
			codec->channel_layout :
			av_get_default_channel_layout(codec->channels);

		//channels may be extended later.
		if (_sampleInfo->Channels > 2)
			_sampleInfo->Channels = 2;
		//av_opt_set(swr_ctx, "resampler", "soxr", 0);
		av_opt_set_int(swr_ctx, "in_channel_layout", src_ch_layout, 0);
		if (_sampleInfo->Channels == 2)
			av_opt_set_int(swr_ctx, "out_channel_layout", AV_CH_LAYOUT_STEREO, 0);
		else if (_sampleInfo->Channels == 1)
			av_opt_set_int(swr_ctx, "out_channel_layout", AV_CH_LAYOUT_MONO, 0);
		else
			av_opt_set_int(swr_ctx, "out_channel_layout", src_ch_layout, 0);
		av_opt_set_int(swr_ctx, "in_sample_rate", codec->sample_rate, 0);
		av_opt_set_int(swr_ctx, "out_sample_rate", _sampleInfo->SampleRate == 0 ? codec->sample_rate : _sampleInfo->SampleRate, 0);

		av_opt_set_int(swr_ctx, "in_sample_fmt", codec->sample_fmt, 0);
		switch (sample->BitsPerSample)
		{
		case 16:
			av_opt_set_int(swr_ctx, "out_sample_fmt", AV_SAMPLE_FMT_S16, 0);
			break;
		case 24:
		case 32:
			av_opt_set_int(swr_ctx, "out_sample_fmt", AV_SAMPLE_FMT_S32, 0);
			break;
		}
		auto result = swr_init(swr_ctx);
		if (result < 0)
		{
			char errstr[100];
			av_strerror(result, errstr, 100);
			throw ref new Platform::Exception(E_FAIL);
		}
	}
}

void FfmpegAudioReader::SetTrackTimeRange(Light::AudioIndexCue^ cue)
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();
	if (cue == nullptr)
	{
		audioDurationTicks = -1;
		startOffsetTicks = 0;
	}
	else
	{
		audioDurationTicks = cue->Duration.Duration;
		startOffsetTicks = cue->StartTime.Duration;
		if (audioDurationTicks == 0)
		{
			audioDurationTicks = AudiofileDuration - startOffsetTicks;
		}
		//this->AccurateSeek(startOffsetTicks);
		DecodedTicks = 0;
	}
}

FfmpegMediaInfo ^ FfmpegAudioReader::ReadMetadata()
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();

	auto _info = ref new FfmpegMediaInfo();
	auto metadata = pFormatContext->metadata;
	if (metadata == nullptr) {
		metadata = pFormatContext->streams[nAudioStream]->metadata;
	}
	_info->Initialize(GetActualDuration().Duration, metadata);
	return _info;
}

IBuffer ^ FfmpegAudioReader::ReadFrontCover()
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();
	for (unsigned int i = 0; i < pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->disposition & AV_DISPOSITION_ATTACHED_PIC)
		{
			AVPacket pkt = pFormatContext->streams[i]->attached_pic;
			auto writer = ref new DataWriter();
			writer->WriteBytes(Platform::ArrayReference<BYTE>(pkt.data, pkt.size));
			auto buffer = writer->DetachBuffer();
			delete writer;
			return buffer;
		}
	}
	return nullptr;
}

IBuffer ^ FfmpegAudioReader::ReadAndDecodeFrame(int& bufferTicks)
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	if (!pFormatContext)
		return nullptr;
	if (audioDurationTicks != -1 && DecodedTicks >= audioDurationTicks)
	{
		bufferTicks = 0;
		return nullptr;
	}
	IBuffer^ buffer = nullptr;
	if (!buffer_queue.empty())
	{
		buffer = buffer_queue.front();
		buffer_queue.pop();
	}
	else
		ReadAndDecodeInternal(buffer);
	if (buffer == nullptr)
	{
		bufferTicks = 0;
		return nullptr;
	}
	if (_sampleInfo)
	{
		bufferTicks = buffer->Length * 10000000LL / _sampleInfo->SampleRate / (_sampleInfo->Channels * _sampleInfo->BitsPerSample / 8);
	}
	else
	{
		bufferTicks = buffer->Length * 10000000LL / codec->sample_rate / (codec->channels * bitspersample / 8);
	}
	DecodedTicks += bufferTicks;
	return buffer;
}

void FfmpegAudioReader::ReadAndDecodeInternal(IBuffer^& buffer)
{
	int decoderError = 0;
	if (nullptr != frame &&
		delayedCodec &&
		ContinueDecodeUnreadDelayedCodecBuffers(decoderError))
	{
		if (_sampleInfo != nullptr)
			buffer = ResampleBuffer();
		else
			buffer = RearrangeBufferLayout();
	}
	else
	{
		int ret = 0, stat = 0;
		for (;;)
		{
			if (!_frame_read)
				do
				{
					ret = av_read_frame(pFormatContext, &packet);
					if (ret < 0)
					{
						buffer = nullptr;
						return;
					}
				} while (packet.stream_index != nAudioStream);
			else _frame_read = false;
			frame = av_frame_alloc();
			ret = avcodec_decode_audio4(codec, frame, &stat, &packet);
			if (ret < 0 || !stat)
			{
				av_packet_unref(&packet);
				av_frame_free(&frame);
			}
			else
				break;
		}
		if (_sampleInfo != nullptr)
			buffer = ResampleBuffer();
		else
			buffer = RearrangeBufferLayout();
	}
}


AudioStreamDescriptor ^ FfmpegAudioReader::GetAudioDescriptor()
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();
	if (_sampleInfo != nullptr)
	{
		auto props = AudioEncodingProperties::CreatePcm(
			(unsigned int)_sampleInfo->SampleRate,
			(unsigned int)_sampleInfo->Channels,
			(unsigned int)_sampleInfo->BitsPerSample);
		return ref new AudioStreamDescriptor(props);
	}
	else
	{
		auto props = AudioEncodingProperties::CreatePcm(
			(unsigned int)codec->sample_rate,
			(unsigned int)codec->channels,
			(unsigned int)bitspersample);
		return ref new AudioStreamDescriptor(props);
	}

}

int64_t FfmpegAudioReader::Seek(int64_t ticks)
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();
	CheckAndClearUnreadDelayedCodecBuffers();
	av_packet_unref(&packet);
	av_frame_free(&frame);
	auto tb = av_q2d(pFormatContext->streams[nAudioStream]->time_base);
	auto timestamp = int64_t((long long)((ticks + startOffsetTicks) / 10000000) / tb);
	timestamp = std::max(0LL, timestamp);
	timestamp = std::min(timestamp, pFormatContext->streams[nAudioStream]->duration);
	int ret = av_seek_frame(pFormatContext, nAudioStream, timestamp, AVSEEK_FLAG_BACKWARD);
	if (timestamp == 0)
		DecodedTicks = 0;
	else
	{
		do
		{
			ret = av_read_frame(pFormatContext, &packet);
			if (ret < 0)
				return ticks;
		} while (packet.stream_index != nAudioStream);
		_frame_read = true;
		if (AV_NOPTS_VALUE == packet.dts)
			DecodedTicks = ticks;
		else
			DecodedTicks = (int64_t)(packet.dts * av_q2d(pFormatContext->streams[nAudioStream]->time_base) * 10000000LL) - startOffsetTicks;
	}
	return DecodedTicks;
}

int64_t FfmpegAudioReader::AccurateSeek(int64_t ticks)
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();
	CheckAndClearUnreadDelayedCodecBuffers();
	av_packet_unref(&packet);
	av_frame_free(&frame);
	auto tb = av_q2d(pFormatContext->streams[nAudioStream]->time_base);
	auto timestamp = int64_t((long long)((ticks + startOffsetTicks) / 10000000) / tb);
	timestamp = std::max(0LL, timestamp);
	timestamp = std::min(timestamp, pFormatContext->streams[nAudioStream]->duration);
	int ret = av_seek_frame(pFormatContext, nAudioStream, timestamp, AVSEEK_FLAG_BACKWARD);
	if (timestamp == 0)
		DecodedTicks = 0;
	else
	{
		do
		{
			ret = av_read_frame(pFormatContext, &packet);
			if (ret < 0)
				return ticks;
		} while (packet.stream_index != nAudioStream);
		_frame_read = true;
		if (AV_NOPTS_VALUE == packet.dts)
			DecodedTicks = ticks;
		else
		{
			auto actualTicks = (int64_t)(packet.dts * av_q2d(pFormatContext->streams[nAudioStream]->time_base) * 10000000LL) - startOffsetTicks;
			auto left = ticks - actualTicks;
			IBuffer^ buffer = nullptr;
			for (;;)
			{
				ReadAndDecodeInternal(buffer);
				if (buffer == nullptr)
					DecodedTicks = actualTicks;
				else
				{
					int sampleRate, channels, bitdepth;
					if (_sampleInfo != nullptr) {
						sampleRate = _sampleInfo->SampleRate;
						channels = _sampleInfo->Channels;
						bitdepth = _sampleInfo->BitsPerSample;
					}
					else {
						sampleRate = codec->sample_rate;
						channels = codec->channels;
						bitdepth = bitspersample;
					}

					auto bufferTicks = buffer->Length * 10000000LL / sampleRate / (channels * bitdepth / 8);
					actualTicks += bufferTicks;
					if (actualTicks > ticks)
					{
						auto units = buffer->Length / (channels * bitdepth / 8);
						auto validUnits = units * (actualTicks - ticks) / bufferTicks;
						auto validByteLength = (uint32_t)(validUnits * (channels * bitdepth / 8));
						ComPtr<IBufferByteAccess> bufferByteAccess;
						reinterpret_cast<IInspectable*>(buffer)->QueryInterface(IID_PPV_ARGS(&bufferByteAccess));
						byte *bytebuf, *targetBytes = new byte[validByteLength];
						auto hr = bufferByteAccess->Buffer(&bytebuf);
						if (SUCCEEDED(hr))
						{
							memcpy(targetBytes, bytebuf + buffer->Length - validByteLength, (size_t)validByteLength);
							Microsoft::WRL::ComPtr<InternalByteBuffer> combuffer;
							Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&combuffer, targetBytes, validByteLength);
							buffer_queue.push(reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(combuffer.Get()));
							DecodedTicks = ticks;
						}
						else
						{
							buffer_queue.push(buffer);
							DecodedTicks = actualTicks - bufferTicks;
						}
						break;
					}
				}
			}
		}
	}
	return DecodedTicks;
}

bool FfmpegAudioReader::CloseDelayedStream()
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	if (!(delayedStream && delayedStreamOpened))
		return false;
	else
	{
		ReleaseFfmpeg();
		fileStreamData->Release();
		delayedStreamOpened = false;
		return true;
	}
}

Platform::String ^ FfmpegAudioReader::ReadCueSheet()
{
	std::lock_guard<std::mutex> lock(decode_mutex);
	CheckAndInitializeDelayedStream();
	return GetAVDictValueAsString(pFormatContext->metadata, "cuesheet");
}

TimeSpan FfmpegAudioReader::GetActualDuration()
{
	if (audioDurationTicks == -1)
	{
		return TimeSpan{ AudiofileDuration };
	}
	else
	{
		return TimeSpan{ audioDurationTicks };
	}
}

void FfmpegAudioReader::OpenStream(IRandomAccessStream ^ stream)
{
	if (streamData != nullptr)
	{
		delete streamData;
	}
	streamData = stream;
	auto hr = CreateStreamOverRandomAccessStream(reinterpret_cast<IUnknown*>(stream), IID_PPV_ARGS(&fileStreamData));
	if (FAILED(hr))
		throw ref new Platform::COMException(hr);
	auto io = avio_alloc_context(
		(unsigned char*)av_malloc(FS_BUFFER_SIZE),
		FS_BUFFER_SIZE, 0,
		fileStreamData,
		IStreamRead, 0,
		IStreamSeek);
	int ret = 0;
	if (!io)
	{
		throw ref new Exception(E_FAIL);
	}
	pFormatContext = avformat_alloc_context();
	pFormatContext->pb = io;

	if (ret = avformat_open_input(&pFormatContext, "", NULL, NULL))
	{
		avformat_close_input(&pFormatContext);
		throw ref new Exception(E_FAIL);
	}
	if ((ret = avformat_find_stream_info(pFormatContext, NULL)) < 0)
	{
		avformat_close_input(&pFormatContext);
		throw ref new Exception(E_FAIL);
	}
	nAudioStream = -1;
	for (int i = 0; i < (int)pFormatContext->nb_streams; i++)
	{
		if (pFormatContext->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO)
		{
			nAudioStream = i;
			break;
		}
	}
	if (nAudioStream == -1)
		throw ref new Exception(E_FAIL);

	auto duration = Windows::Foundation::TimeSpan();
	AudiofileDuration = duration.Duration = (int64_t)(pFormatContext->streams[nAudioStream]->duration * av_q2d(pFormatContext->streams[nAudioStream]->time_base) * 10000000LL);

	codec = pFormatContext->streams[nAudioStream]->codec;

	av_seek_frame(pFormatContext, nAudioStream, 0, 0);
	bitspersample = std::max(codec->bits_per_coded_sample, codec->bits_per_raw_sample);
	if (bitspersample == 0)
		bitspersample = 16;

	auto pCodec = avcodec_find_decoder(codec->codec_id);
	if (!pCodec)
		throw Exception::CreateException(E_FAIL, L"No Available Codec Found!");

	delayedCodec = (pCodec->capabilities&CODEC_CAP_DELAY) != 0;

	if ((ret = avcodec_open2(codec, pCodec, NULL)) < 0)
		throw Exception::CreateException(E_FAIL, L"Codec Not supported");
	sfmt = codec->sample_fmt;

	// Resample to user-preferred PCM format for output.
	if (Light::NativeSettingsManager::Instance->AlwaysResample ||
		codec->codec_id == AV_CODEC_ID_DSD_LSBF ||
		codec->codec_id == AV_CODEC_ID_DSD_MSBF ||
		codec->codec_id == AV_CODEC_ID_DSD_LSBF_PLANAR ||
		codec->codec_id == AV_CODEC_ID_DSD_MSBF_PLANAR) {
		SetDefaultResampler();
	}

	//ugly workaround for tak decoding issue.
	typedef struct TAKDemuxContext
	{
		int     mlast_frame;
		int64_t data_end;
	} TAKDemuxContext;
	if (!strcmp(pCodec->name, "tak"))
	{
		auto tak = (TAKDemuxContext*)pFormatContext->priv_data;
		tak->data_end = 0x7FFFFFFFFFFFFFFF;
	}
}

void FfmpegAudioReader::OpenFileForDelayedStream()
{
	OpenStream(AWait(storageFile->OpenReadAsync()));
}

void FfmpegAudioReader::CheckAndInitializeDelayedStream()
{
	if (delayedStream && !delayedStreamOpened)
	{
		OpenFileForDelayedStream();
		if (_sampleInfo && !swr_ctx)
			SetResampleTarget(_sampleInfo);
		delayedStreamOpened = true;
	}
}

void FfmpegAudioReader::CheckAndClearUnreadDelayedCodecBuffers()
{
	if (frame&&delayedCodec)
	{
		while (true)
		{
			int stat = 0;
			packet.data = NULL;
			packet.size = 0;
			int ret = avcodec_decode_audio4(codec, frame, &stat, &packet);
			if (stat == 0)
				break;
		}
	}
}

bool FfmpegAudioReader::ContinueDecodeUnreadDelayedCodecBuffers(int& decoderError)
{
	int stat = 0;
	packet.data = NULL;
	packet.size = 0;
	decoderError = avcodec_decode_audio4(codec, frame, &stat, &packet);
	if (!stat)
	{
		av_frame_free(&frame);
		return false;
	}
	else if (decoderError < 0)
	{
		av_packet_unref(&packet);
		av_frame_free(&frame);
		return false;
	}
	return true;
}

void FfmpegAudioReader::ReleaseFfmpeg()
{
	av_packet_unref(&packet);
	av_frame_free(&frame);
	if (codec)
	{
		avcodec_close(codec);
	}
	if (pFormatContext)
	{
		if (pFormatContext->pb)
		{
			av_freep(&pFormatContext->pb->buffer);
			av_free(pFormatContext->pb);
		}
		avformat_close_input(&pFormatContext);
	}
}

IBuffer ^ FfmpegAudioReader::ResampleBuffer()
{
	auto dst_nb_samples = av_rescale_rnd(swr_get_delay(swr_ctx, codec->sample_rate) +
		frame->nb_samples, _sampleInfo->SampleRate, codec->sample_rate, AV_ROUND_UP);
	int bytes = (int)(dst_nb_samples*_sampleInfo->Channels*(_sampleInfo->BitsPerSample == 24 ? 32 : _sampleInfo->BitsPerSample) / 8);
	byte* buf = new byte[bytes];

	auto ret = swr_convert(swr_ctx, &buf, (int)dst_nb_samples, (const uint8_t**)frame->extended_data, frame->nb_samples);
	if (ret < 0)
		return nullptr;
	bytes = ret*(_sampleInfo->BitsPerSample == 24 ? 32 : _sampleInfo->BitsPerSample)*(_sampleInfo->Channels == 0 ? codec->channels : _sampleInfo->Channels) / 8;
	if (!delayedCodec)
		av_frame_free(&frame);
	if (_sampleInfo->BitsPerSample == 24)
	{
		auto tmpBytes = buf;
		auto newbytes = bytes * 3 / 4;
		buf = new byte[newbytes];
		for (int i = 0, j = 0; j < newbytes; i++)
		{
			if ((i % 4) == 0)
				continue;
			buf[j] = tmpBytes[i];
			j++;
		}
		delete tmpBytes;
		bytes = newbytes;
	}
	Microsoft::WRL::ComPtr<InternalByteBuffer> buffer;
	Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&buffer, buf, bytes);
	av_packet_unref(&packet);
	return reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(buffer.Get());
}


IBuffer ^ FfmpegAudioReader::RearrangeBufferLayout()
{
	auto bytes = frame->nb_samples * bitspersample * codec->channels / 8;
	int plane_size = 0;
	int data_size = av_samples_get_buffer_size(&plane_size, codec->channels,
		frame->nb_samples,
		codec->sample_fmt, 1);
	auto buf = new byte[bytes];
	uint16_t *out = (uint16_t *)buf;
	int write_ps = 0;

	uint32_t* tmpBytes;
	int tmp_ps;
	switch (sfmt)
	{
	case AV_SAMPLE_FMT_FLTP:
		for (unsigned int i = 0; i < plane_size / sizeof(float); i++)
		{
			for (int c = 0; c < codec->channels; c++)
			{
				float* extended_data = (float*)frame->extended_data[c];
				float sample = extended_data[i];
				if (sample < -1.0f) sample = -1.0f;
				else if (sample > 1.0f) sample = 1.0f;
				out[i * codec->channels + c] = (int16_t)round(sample * 32767.0f);
			}
		}
		bytes = (plane_size / sizeof(float)) * sizeof(uint16_t) * codec->channels;
		break;
	case AV_SAMPLE_FMT_FLT:
		for (unsigned int nb = 0; nb < plane_size / sizeof(float); nb++)
		{
			out[nb] = static_cast<short> (((float *)frame->extended_data[0])[nb] * std::numeric_limits<short>::max());
		}
		break;

	case AV_SAMPLE_FMT_U8P:
		for (unsigned int nb = 0; nb < plane_size / sizeof(uint8_t); nb++)
		{
			for (int ch = 0; ch < codec->channels; ch++)
			{
				out[write_ps] = (((uint8_t *)frame->extended_data[0])[nb] - 127) * std::numeric_limits<short>::max() / 127;
				write_ps++;
			}
		}
		break;
	case AV_SAMPLE_FMT_U8:
		for (unsigned int nb = 0; nb < plane_size / sizeof(uint8_t); nb++)
		{
			out[nb] = static_cast<short> ((((uint8_t *)frame->extended_data[0])[nb] - 127) * std::numeric_limits<short>::max() / 127);
		}
		break;
	case AV_SAMPLE_FMT_S16:
		memcpy(buf, frame->extended_data[0], bytes);
		break;
	case AV_SAMPLE_FMT_S16P:
		for (unsigned int nb = 0; nb < plane_size / sizeof(uint16_t); nb++)
		{
			for (int ch = 0; ch < codec->channels; ch++)
			{
				out[write_ps] = ((uint16_t*)frame->extended_data[ch])[nb];
				write_ps++;
			}
		}
		break;
	case AV_SAMPLE_FMT_S32P:
		if (bitspersample == 24)
		{
			tmpBytes = new uint32_t[bytes / 3];
			tmp_ps = 0;
			for (unsigned int nb = 0; nb < plane_size / sizeof(uint32_t); nb++)
			{
				for (int ch = 0; ch < codec->channels; ch++)
				{
					tmpBytes[tmp_ps] = ((uint32_t*)frame->extended_data[ch])[nb];
					tmp_ps++;
				}
			}
			for (int i = 0, j = 0; j < bytes; i++)
			{
				if ((i % 4) == 0)
					continue;
				buf[j] = ((uint8_t*)tmpBytes)[i];
				j++;
			}
			delete tmpBytes;
		}
		else
		{
			//32bit, NOT tested.
			tmpBytes = new uint32_t[bytes / 3];
			tmp_ps = 0;
			for (unsigned int nb = 0; nb < plane_size / sizeof(uint32_t); nb++)
			{
				for (int ch = 0; ch < codec->channels; ch++)
				{
					tmpBytes[tmp_ps] = ((uint32_t*)frame->extended_data[ch])[nb];
					tmp_ps++;
				}
			}
			memcpy(buf, tmpBytes, bytes);
			delete tmpBytes;
		}
		break;
	case AV_SAMPLE_FMT_S32:
		if (bitspersample == 24)
		{
			for (int i = 0, j = 0; j < bytes; i++)
			{
				if ((i % 4) == 0)
					continue;
				buf[j] = frame->extended_data[0][i];
				j++;
			}
		}
		else
			//32bit NOT tested. (should work)
			memcpy(buf, frame->extended_data[0], bytes);
		break;
	case AV_SAMPLE_FMT_DBL:
		//remain for implementation
		return nullptr;
	case AV_SAMPLE_FMT_DBLP:
		//remain for implementation
		return nullptr;
	default:
		//not supported
		return nullptr;
	}
	if (!delayedCodec)
		av_frame_free(&frame); //avcodec_free_frame(&frame);
	Microsoft::WRL::ComPtr<InternalByteBuffer> buffer;
	Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&buffer, buf, bytes);
	av_packet_unref(&packet);
	return reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(buffer.Get());
}

Platform::String ^ FfmpegAudioReader::GetAVDictValueAsString(AVDictionary * dictionary, const char * key)
{
	auto entry = av_dict_get(dictionary, key, NULL, 0);
	if (!entry)
		return L"";
	return utf8ToString(entry->value);
}

void FfmpegAudioReader::GetAVDictSplittedValueAsStrings(AVDictionary * dict, const char * key, String ^& value1, String ^& value2)
{
	value1 = value2 = nullptr;

	auto entry = av_dict_get(dict, key, NULL, 0);
	if (!entry) return;

	auto len = strlen(entry->value);
	if (int pos = indexOf(entry->value, '/') > -1)
	{
		auto buffer = std::make_unique<char[]>(len);

		substring(buffer.get(), entry->value, pos);
		value1 = utf8ToString(buffer.get());

		substring(buffer.get(), entry->value, len, pos + 1);
		value2 = utf8ToString(buffer.get());
	}
	else value1 = utf8ToString(entry->value);
}
