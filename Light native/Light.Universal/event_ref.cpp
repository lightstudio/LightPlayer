#include "pch.h"
#ifdef ENABLE_LEGACY
#include "event_ref.h"

void event_ref::register_mss(MediaStreamSource^ i_mss, long long Duration, int bitspersample)
{
	this->mss = i_mss;
	this->duration = Duration;
	this->bitspersample = bitspersample;
	startingRequestedToken = mss->Starting += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^>(this, &event_ref::OnStarting);
	sampleRequestedToken = mss->SampleRequested += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^>(this, &event_ref::OnSampleRequested);
	closedRequestedToken = mss->Closed += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^>(this, &event_ref::OnClosed);
}

void event_ref::register_av(AVFormatContext* av_format, AVCodecContext* context, int nAudioStream)
{
	this->format = av_format;
	this->codec = context;
	this->nAudioStream = nAudioStream;
	this->sfmt = codec->sample_fmt;
}


void event_ref::set_selfref(_m_evref* self)
{
	this->self_ref = self;
}	

void event_ref::OnStarting(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^args)
{
	auto sp = args->Request->StartPosition;
	auto t = sender->Duration.Duration / 10000;
	if (sp&&sp->Value.Duration <= sender->Duration.Duration)
	{
		auto val = sp->Value;
		auto dur = val.Duration;

		int64_t timestamp = int64_t((int) (dur / 10000) / av_q2d(format->streams[nAudioStream]->time_base) / 1000);
		timestamp = max(0, timestamp);
		timestamp = min(timestamp, format->streams[nAudioStream]->duration);
		int ret = av_seek_frame(format, nAudioStream, timestamp, 0);
		if (ret < 0)
		{
			//throw Exception::CreateException(0, "Error setting time");
			Windows::Foundation::TimeSpan spt = Windows::Foundation::TimeSpan();
			spt.Duration = DecodedTimeInMs * 10000L;
			args->Request->SetActualStartPosition(spt);
			return;
		}


		DecodedTimeInMs = dur / 10000;
		args->Request->SetActualStartPosition(val);
		return;
	}

	Windows::Foundation::TimeSpan spt = Windows::Foundation::TimeSpan();
	spt.Duration = DecodedTimeInMs * 10000L;
	args->Request->SetActualStartPosition(spt);
}

void event_ref::OnSampleRequested(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^args)
{
read:
	auto ret = av_read_frame(format, &packet);
	double time = 0;
	IBuffer^ iBuf = nullptr;
	if (ret >= 0)
	{
		if (packet.stream_index != nAudioStream)
		{
			av_free_packet(&packet);
			goto read;
		}
		int stat = 0;
		AVFrame *frame = av_frame_alloc();//avcodec_alloc_frame();
		int ret = avcodec_decode_audio4(codec, frame, &stat, &packet);
		auto bytes = frame->nb_samples * bitspersample / 4;
		if (ret < 0 || stat == 0)
		{
			av_free_packet(&packet);
			goto read;
		}
		else if (stat)
		{
			int plane_size = 0;
			int data_size = av_samples_get_buffer_size(&plane_size, codec->channels,
				frame->nb_samples,
				codec->sample_fmt, 1);



			auto buf = new byte[bytes];
			uint16_t *out = (uint16_t *) buf;
			int write_ps = 0;

			//Platform::Array<uint8_t>^ a;
			uint32_t* tmpBytes;
			int tmp_ps;
			switch (sfmt)
			{
			case AV_SAMPLE_FMT_FLTP:
#undef max
				for (int i = 0; i < plane_size / sizeof(float); i++)
				{
					for (int c = 0; c < codec->channels; c++)
					{
						float* extended_data = (float*) frame->extended_data[c];
						float sample = extended_data[i];
						if (sample < -1.0f) sample = -1.0f;
						else if (sample > 1.0f) sample = 1.0f;
						out[i * codec->channels + c] = (int16_t) round(sample * 32767.0f);
					}
				}
				bytes = (plane_size / sizeof(float))  * sizeof(uint16_t) * codec->channels;
				break;
			case AV_SAMPLE_FMT_FLT:
				for (int nb = 0; nb < plane_size / sizeof(float); nb++)
				{
					//float* extended_data = (float*)frame->extended_data[0];
					//float sample = extended_data[nb];
					//if (sample < -1.0f) sample = -1.0f;
					//else if (sample > 1.0f) sample = 1.0f;
					//out[nb] = (int16_t)round(sample*32767.0f);
					out[nb] = static_cast<short> (((float *) frame->extended_data[0])[nb] * std::numeric_limits<short>::max());
				}
				break;

			case AV_SAMPLE_FMT_U8P:
				for (int nb = 0; nb < plane_size / sizeof(uint8_t); nb++)
				{
					for (int ch = 0; ch < codec->channels; ch++)
					{
						out[write_ps] = (((uint8_t *) frame->extended_data[0])[nb] - 127) * std::numeric_limits<short>::max() / 127;
						write_ps++;
					}
				}
				break;
			case AV_SAMPLE_FMT_U8:
				for (int nb = 0; nb < plane_size / sizeof(uint8_t); nb++)
				{
					out[nb] = static_cast<short> ((((uint8_t *) frame->extended_data[0])[nb] - 127) * std::numeric_limits<short>::max() / 127);
				}
				break;
			case AV_SAMPLE_FMT_S16:
				memcpy(buf, frame->extended_data[0], bytes);
				break;
			case AV_SAMPLE_FMT_S16P:
				for (int nb = 0; nb < plane_size / sizeof(uint16_t); nb++)
				{
					for (int ch = 0; ch < codec->channels; ch++)
					{
						out[write_ps] = ((uint16_t*) frame->extended_data[ch])[nb];
						write_ps++;
					}
				}
				break;
			case AV_SAMPLE_FMT_S32P:
				if (bitspersample == 24)
				{
					tmpBytes = new uint32_t[bytes / 3];
					tmp_ps = 0;
					for (int nb = 0; nb < plane_size / sizeof(uint32_t); nb++)
					{
						for (int ch = 0; ch < codec->channels; ch++)
						{
							tmpBytes[tmp_ps] = ((uint32_t*) frame->extended_data[ch])[nb];
							tmp_ps++;
						}
					}
					for (int i = 0, j = 0; j < bytes; i++)
					{
						if ((i % 4) == 0)
							continue;
						buf[j] = ((uint8_t*) tmpBytes)[i];
						j++;
					}
					/*a = ref new Platform::Array<uint8_t>(bytes);
					for (int i = 0; i < bytes; i++)
					{
						a[i] = ((uint8_t*)tmpBytes)[i];
					}*/
					delete tmpBytes;
				}
				else
				{
					//32bit, NOT tested.
					tmpBytes = new uint32_t[bytes / 3];
					tmp_ps = 0;
					for (int nb = 0; nb < plane_size / sizeof(uint32_t); nb++)
					{
						for (int ch = 0; ch < codec->channels; ch++)
						{
							tmpBytes[tmp_ps] = ((uint32_t*) frame->extended_data[ch])[nb];
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
				return;
			case AV_SAMPLE_FMT_DBLP:
				//remain for implementation
				return;
			default:
				//not supported
				return;
			}
#define max(a,b)    (((a) > (b)) ? (a) : (b))
			av_frame_free(&frame);//avcodec_free_frame(&frame);
			Microsoft::WRL::ComPtr<InternalByteBuffer> buffer;
			Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&buffer, buf, bytes);
			av_free_packet(&packet);
			time = bytes * 1000.0f / codec->sample_rate / (codec->channels * bitspersample / 8);
			iBuf = reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(buffer.Get());
		}
		else
			goto read;
	}
	else
	{
		char errstr[100];
		av_make_error_string(errstr, 100, ret);
		return;
	}



	DecodedTimeInMs += time;
	Windows::Foundation::TimeSpan sp = Windows::Foundation::TimeSpan();
	sp.Duration = DecodedTimeInMs * 10000;
	auto sample = MediaStreamSample::CreateFromBuffer(iBuf, sp);

	auto req = args->Request;
	Windows::Foundation::TimeSpan duration = Windows::Foundation::TimeSpan();
	duration.Duration = time * 10000;
	sample->Duration = duration;
	sample->KeyFrame = true;
	req->Sample = sample;
}
void event_ref::OnClosed(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^args)
{
	mss->Starting -= startingRequestedToken;
	mss->SampleRequested -= sampleRequestedToken;
	mss->Closed -= closedRequestedToken;
	if (format)
	{
		avformat_close_input(&format);
		format = NULL;
	}
	//stream->Dispose();
	if (stream)
	{
		delete stream;
		stream = nullptr;
	}
	if (self_ref)
	{
		//unref
		delete self_ref;
		//self_ref = NULL;
	}
}
#endif