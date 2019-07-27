#pragma once
#ifdef ENABLE_LEGACY
#include "Extension\config.h"
#include "Extension\lightapi_all.h"
#include "Extension\ByteBuffer.h"
using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation::Collections;
using namespace Windows::Media::MediaProperties;
using namespace Windows::Media::Core;
using namespace Windows::Storage;
ref class evref_ext; 
class sref
{
public:
	evref_ext^ ref;
};

ref class evref_ext sealed
{
internal:
	evref_ext(ITrack* track, memory_operation_base* mem)
	{
		this->self_ref = new sref();
		this->self_ref->ref = this;
		this->decoder = track->decoder;
		decoder->SeekToTrack(track);
		MediaStreamSource^ mss = nullptr;
		DecodedTimeInMs = 0;
		auto audio_props = AudioEncodingProperties::CreatePcm((unsigned int)track->Samplerate, (unsigned int)track->Channels, (unsigned int)track->BitsPerSample);
		AudioStreamDescriptor^ audioDescriptor = ref new AudioStreamDescriptor(audio_props);
		mss = ref new Windows::Media::Core::MediaStreamSource(audioDescriptor);
		mss->CanSeek = track->CanSeek;
		auto duration = Windows::Foundation::TimeSpan();
		auto t = track->Duration * 10000L;
		duration.Duration = track->Duration * 10000L;
		mss->Duration = duration;
		startingRequestedToken = mss->Starting += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^>(this, &evref_ext::OnStarting);
		sampleRequestedToken = mss->SampleRequested += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^>(this, &evref_ext::OnSampleRequested);
		pausedRequestedToken = mss->Paused += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Platform::Object ^>(this, &evref_ext::OnPaused);
		switchstreamsRequestedToken = mss->SwitchStreamsRequested += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceSwitchStreamsRequestedEventArgs ^>(this, &evref_ext::OnSwitchStreamsRequested);
		closedRequestedToken = mss->Closed += ref new Windows::Foundation::TypedEventHandler<Windows::Media::Core::MediaStreamSource ^, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^>(this, &evref_ext::OnClosed);
		this->MSS = mss;
		this->BufferTimeInMs = DEFAULT_BUFFER_TIME_MS;
		this->track = track;
		this->mem = mem;
	}
	/*
	* sets the buffer time when decoding.
	* default value is defined in config.h
	*/
	void SetBufferTime(int ms)
	{
		this->BufferTimeInMs = ms;
	}
	property MediaStreamSource^ MSS;

private:
	void OnStarting(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^args)
	{
		auto sp = args->Request->StartPosition;
		auto t = sender->Duration.Duration / 10000;
		if (sp&&sp->Value.Duration <= sender->Duration.Duration)
		{
			auto val = sp->Value;
			auto dur = val.Duration;
			decoder->SeekToRelativeTime((int)(dur / 10000));
			DecodedTimeInMs = dur / 10000;
			args->Request->SetActualStartPosition(val);
			return;
		}

		Windows::Foundation::TimeSpan spt = Windows::Foundation::TimeSpan();
		spt.Duration = DecodedTimeInMs * 10000L;
		args->Request->SetActualStartPosition(spt);
	}
	void OnSampleRequested(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^args)
	{
		auto req = args->Request;
		int err = 0;
		Windows::Foundation::TimeSpan sp = Windows::Foundation::TimeSpan();
		unsigned char* buffer = NULL;
		int retrieved = 0;
		err += decoder->Read(&buffer, &retrieved, BufferTimeInMs);
		if (err != 0 || retrieved == 0)
		{
			//stream end
			return;
		}

		long long retrievedtimeinms = retrieved * 1000 / track->BlockAlign  / track->Samplerate;
		sp.Duration = DecodedTimeInMs * 10000L;
		DecodedTimeInMs += retrievedtimeinms;

		Microsoft::WRL::ComPtr<ByteBuffer> cpBuffer;
		Microsoft::WRL::Details::MakeAndInitialize<ByteBuffer>(&cpBuffer, buffer, retrieved, mem);
		auto ibuffer = reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(cpBuffer.Get());

		auto sample = MediaStreamSample::CreateFromBuffer(ibuffer, sp);
		Windows::Foundation::TimeSpan duration = Windows::Foundation::TimeSpan();
		duration.Duration = retrievedtimeinms * 10000;
		sample->Duration = duration;
		sample->KeyFrame = true;
		req->Sample = sample;
	}
	void OnPaused(Windows::Media::Core::MediaStreamSource ^sender, Platform::Object ^args)
	{
	}
	void OnSwitchStreamsRequested(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceSwitchStreamsRequestedEventArgs ^args)
	{
	}
	void OnClosed(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^args)
	{
		sender->Starting -= startingRequestedToken;
		sender->SampleRequested -= sampleRequestedToken;
		sender->Closed -= closedRequestedToken;
		sender->Paused -= pausedRequestedToken;
		sender->SwitchStreamsRequested -= switchstreamsRequestedToken;
		self_ref->ref = nullptr;
		delete self_ref;
	}
	Windows::Foundation::EventRegistrationToken sampleRequestedToken;
	Windows::Foundation::EventRegistrationToken startingRequestedToken;
	Windows::Foundation::EventRegistrationToken closedRequestedToken;
	Windows::Foundation::EventRegistrationToken pausedRequestedToken;
	Windows::Foundation::EventRegistrationToken switchstreamsRequestedToken;
	int BufferTimeInMs;
	long long DecodedTimeInMs;
	IExtendedDecoder* decoder;
	ITrack* track;
	memory_operation_base* mem;
	sref* self_ref;
};
#endif