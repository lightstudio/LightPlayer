#pragma once
#ifdef ENABLE_LEGACY
#include "InternalByteBuffer.h"

extern "C"
{
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}
using namespace Windows::Media::Core;
using namespace Windows::Storage::Streams;

struct _m_evref;


ref class event_ref
{
internal:
	event_ref()
		:DecodedTimeInMs(0)
	{ }
	void register_mss(MediaStreamSource^ i_mss, long long Duration, int bitspersample);
	void register_av(AVFormatContext* av_format, AVCodecContext* context, int nAudioStream);
	void set_selfref(_m_evref* self);
	property IRandomAccessStream^ stream;
private:
	long long duration;

	Windows::Foundation::EventRegistrationToken sampleRequestedToken;
	Windows::Foundation::EventRegistrationToken startingRequestedToken;
	Windows::Foundation::EventRegistrationToken closedRequestedToken;


	void OnStarting(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^args);
	void OnSampleRequested(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^args);
	void OnClosed(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^args);
	double DecodedTimeInMs;
	AVFormatContext* format;
	AVCodecContext* codec;
	AVPacket packet;
	MediaStreamSource^ mss;
	int nAudioStream;
	_m_evref* self_ref;
	int bitspersample;
	AVSampleFormat sfmt;
};

struct _m_evref
{
	event_ref^ Content;
};
#endif