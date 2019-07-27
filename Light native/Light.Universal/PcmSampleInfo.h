#pragma once


namespace Light {
	namespace BuiltInCodec {
		public ref class PcmSampleInfo sealed {
		public:
			//We do not need a sample format property since MSS API 
			//only accepts signed integer non-planar PCM format.
			PcmSampleInfo(
				unsigned int sampleRate, 
				unsigned int channels, 
				unsigned int bitsPerSample): 
				SampleRate(sampleRate),
				Channels(channels),
				BitsPerSample(bitsPerSample){}
		internal:
			unsigned int SampleRate;
			unsigned int Channels;
			unsigned int BitsPerSample;
		};
	}
}