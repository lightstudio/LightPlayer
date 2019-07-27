//*****************************************************************************
//
//	Copyright 2015 Microsoft Corporation
//
//	Licensed under the Apache License, Version 2.0 (the "License");
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at
//
//	http ://www.apache.org/licenses/LICENSE-2.0
//
//	Unless required by applicable law or agreed to in writing, software
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	See the License for the specific language governing permissions and
//	limitations under the License.
//
//*****************************************************************************

#pragma once
#ifdef ENABLE_VIDEO
#include <queue>

extern "C"
{
#include <libavformat/avformat.h>
}


namespace Light {
	namespace Video {
		ref class FFmpegInteropMSS;
		ref class FFmpegReader;

		ref class MediaSampleProvider
		{
		public:
			virtual ~MediaSampleProvider();
			virtual Windows::Media::Core::MediaStreamSample^ GetNextSample();
			virtual void Flush();
			virtual void SetCurrentStreamIndex(int streamIndex);

		internal:
			void PushPacket(AVPacket packet);
			AVPacket PopPacket();

		private:
			std::queue<AVPacket> m_packetQueue;
			int m_streamIndex;

		internal:
			// The FFmpeg context. Because they are complex types
			// we declare them as internal so they don't get exposed
			// externally
			FFmpegReader^ m_pReader;
			AVFormatContext* m_pAvFormatCtx;
			AVCodecContext* m_pAvCodecCtx;

		internal:
			MediaSampleProvider(
				FFmpegReader^ reader,
				AVFormatContext* avFormatCtx,
				AVCodecContext* avCodecCtx);
			virtual HRESULT AllocateResources();
			virtual HRESULT WriteAVPacketToStream(Windows::Storage::Streams::DataWriter^ writer, AVPacket* avPacket);
			virtual HRESULT DecodeAVPacket(Windows::Storage::Streams::DataWriter^ dataWriter, AVPacket* avPacket);
		};
	}
}
#endif