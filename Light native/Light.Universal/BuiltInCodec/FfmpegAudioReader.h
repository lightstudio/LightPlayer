#pragma once

class FfmpegAudioReader
{
public:
	//Create ffmpeg reader from stream
	FfmpegAudioReader(Windows::Storage::Streams::IRandomAccessStream^ stream);
	//Create ffmpeg reader from file (delay open)
	FfmpegAudioReader(Windows::Storage::IStorageFile^ file) :storageFile(file), delayedStream(true) {}

	~FfmpegAudioReader();
	
	void SetResampleTarget(Light::BuiltInCodec::PcmSampleInfo^ sample);
	void SetTrackTimeRange(Light::AudioIndexCue^ cue);
	Light::BuiltInCodec::FfmpegMediaInfo^ ReadMetadata();
	Windows::Storage::Streams::IBuffer^ ReadFrontCover();
	Windows::Storage::Streams::IBuffer^ ReadAndDecodeFrame(int& decodedTicks);
	Windows::Media::Core::AudioStreamDescriptor^ GetAudioDescriptor();
	int64_t Seek(int64_t timestamp);
	int64_t AccurateSeek(int64_t timestamp);
	bool CloseDelayedStream();
	Platform::String^ ReadCueSheet();
	Windows::Foundation::TimeSpan GetActualDuration();

	int64_t AudiofileDuration;
	int64_t DecodedTicks = 0;

private:
	void OpenStream(Windows::Storage::Streams::IRandomAccessStream^ stream);
	void OpenFileForDelayedStream();
	void SetDefaultResampler();
	void CheckAndInitializeDelayedStream();
	void CheckAndClearUnreadDelayedCodecBuffers();
	bool ContinueDecodeUnreadDelayedCodecBuffers(int& decoderError);
	void ReleaseFfmpeg();
	void ReadAndDecodeInternal(Windows::Storage::Streams::IBuffer^& buffer);
	Windows::Storage::Streams::IBuffer^ RearrangeBufferLayout();
	Windows::Storage::Streams::IBuffer^ ResampleBuffer();
	Platform::String^ GetAVDictValueAsString(AVDictionary* dictionary, const char* key);
	void GetAVDictSplittedValueAsStrings(AVDictionary* dict, const char* key, Platform::String^& value1, Platform::String^& value2);

	Light::BuiltInCodec::PcmSampleInfo^ _sampleInfo;

	Light::BuiltInCodec::FfmpegMediaInfo^ _info;
	AVFormatContext* pFormatContext;

	int64_t startOffsetTicks = 0;
	int64_t audioDurationTicks = -1;
	bool delayedStream = false;
	bool delayedStreamOpened = false;
	IStream* fileStreamData = nullptr;
	Windows::Storage::Streams::IRandomAccessStream^ streamData = nullptr;
	Windows::Storage::IStorageFile^ storageFile = nullptr;
	std::mutex decode_mutex;
	std::queue<Windows::Storage::Streams::IBuffer^> buffer_queue;

	SwrContext * swr_ctx = NULL;
	AVCodecContext* codec;
	AVPacket packet;
	AVFrame *frame;
	int nAudioStream;
	int bitspersample;
	AVSampleFormat sfmt;
	bool delayedCodec;
	
	bool _frame_read = false;
};