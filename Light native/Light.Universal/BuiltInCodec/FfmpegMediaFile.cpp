#include "pch.h"
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswresample/swresample.h>
}
#include <shcore.h>
#include <mutex>
#include <queue>
#include "IMediaInfo.h"
#include "AudioIndexCue.h"
#include "IMediaFile.h"
#include "FfmpegMediaInfo.h"
#include "PcmSampleInfo.h"
#include "FfmpegAudioReader.h"
#include "FfmpegMediaFile.h"
#include "FfmpegFileIO.h"
#include "StringUtils.h"
#include "InternalByteBuffer.h"

using namespace Light;
using namespace Light::BuiltInCodec;
using namespace Windows::Storage::Streams;
using namespace Windows::Media;
using namespace Windows::Media::Core;
using namespace Windows::Media::MediaProperties;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace std;


FfmpegMediaFile::FfmpegMediaFile(IRandomAccessStream^ stream)
{
	reader = new FfmpegAudioReader(stream);
}

FfmpegMediaFile::FfmpegMediaFile(Windows::Storage::IStorageFile^ file)
{
	reader = new FfmpegAudioReader(file);
}

FfmpegMediaFile::~FfmpegMediaFile()
{
	delete reader;
}

IBuffer^ FfmpegMediaFile::FrontCover::get()
{
	return reader->ReadFrontCover();
}

IBuffer^ FfmpegMediaFile::BackCover::get()
{
	return nullptr;
}

IBuffer^ FfmpegMediaFile::DiscScan::get()
{
	return nullptr;
}

IBuffer^ FfmpegMediaFile::ArtistImage::get()
{
	return nullptr;
}

IBuffer^ FfmpegMediaFile::Icon::get()
{
	return nullptr;
}

String^ FfmpegMediaFile::CueSheet::get()
{
	return reader->ReadCueSheet();
}

MediaStreamSource^ FfmpegMediaFile::LoadTrack()
{
	return LoadTrack(nullptr);
}

MediaStreamSource^ FfmpegMediaFile::LoadTrack(Light::AudioIndexCue^ range)
{
	reader->SetTrackTimeRange(range);
	auto _info = reader->ReadMetadata();
	//set resampler here.
	//reader->SetResampleTarget(ref new PcmSampleInfo(48000, 2, 24));
	this->mss = ref new MediaStreamSource(reader->GetAudioDescriptor());
	mss->CanSeek = true;
	mss->Duration = reader->GetActualDuration();
	mss->MusicProperties->Album = _info->Album;
	mss->MusicProperties->Artist = _info->Artist;
	mss->MusicProperties->Title = _info->Title;
	mss->MusicProperties->Year = _wtoi(_info->Date->Data());
	mss->MusicProperties->TrackNumber = _wtoi(_info->TrackNumber->Data());
	reader->CloseDelayedStream();
	mss->Starting +=
		ref new TypedEventHandler<MediaStreamSource ^, MediaStreamSourceStartingEventArgs ^>(this, &FfmpegMediaFile::OnStarting);
	mss->Closed +=
		ref new TypedEventHandler<MediaStreamSource ^, MediaStreamSourceClosedEventArgs ^>(this, &FfmpegMediaFile::OnClosed);
	mss->SampleRequested +=
		ref new TypedEventHandler<MediaStreamSource ^, MediaStreamSourceSampleRequestedEventArgs ^>(this, &FfmpegMediaFile::OnSampleRequested);
	return mss;
}

IMediaInfo^ FfmpegMediaFile::GetTrackInfo()
{
	return reader->ReadMetadata();
}

void Light::BuiltInCodec::FfmpegMediaFile::OnStarting(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^args)
{
	auto sp = args->Request->StartPosition;
	if (sp&&sp->Value.Duration <= sender->Duration.Duration)
	{
		reader->AccurateSeek(sp->Value.Duration);
	}
	auto spt = Windows::Foundation::TimeSpan();
	spt.Duration = reader->DecodedTicks;
	args->Request->SetActualStartPosition(spt);
}

void Light::BuiltInCodec::FfmpegMediaFile::OnClosed(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^args)
{
	reader->CloseDelayedStream();
}

void Light::BuiltInCodec::FfmpegMediaFile::OnSampleRequested(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^args)
{
	auto writer = ref new DataWriter();
	uint64 totalDecoded;
	// 200 milliseconds
	for (totalDecoded = 0; totalDecoded < 2000000LL;) {
		int decoded;
		auto buffer = reader->ReadAndDecodeFrame(decoded);
		if (buffer == nullptr)
			break;
		totalDecoded += decoded;
		writer->WriteBuffer(buffer);
	}
	if (totalDecoded == 0) {
		return;
	}
	Windows::Foundation::TimeSpan sp = Windows::Foundation::TimeSpan();
	sp.Duration = reader->DecodedTicks;
	auto sample = MediaStreamSample::CreateFromBuffer(writer->DetachBuffer(), sp);
	delete writer;
	auto req = args->Request;
	Windows::Foundation::TimeSpan duration = Windows::Foundation::TimeSpan();
	duration.Duration = totalDecoded;
	sample->Duration = duration;
	sample->KeyFrame = true;
	req->Sample = sample;
}
