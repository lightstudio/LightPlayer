#pragma once

namespace Light {
	namespace BuiltInCodec {
		//[uuid(6F758D5C-7C1C-44B3-8F1D-C237EC93C15F)]
		ref class FfmpegMediaFile sealed : public IMediaFile {
		public:
			property Windows::Storage::Streams::IBuffer^ FrontCover {
				virtual Windows::Storage::Streams::IBuffer^ get();
			}
			property Windows::Storage::Streams::IBuffer^ BackCover {
				virtual Windows::Storage::Streams::IBuffer^ get();
			}
			property Windows::Storage::Streams::IBuffer^ DiscScan {
				virtual Windows::Storage::Streams::IBuffer^ get();
			}
			property Windows::Storage::Streams::IBuffer^ ArtistImage {
				virtual Windows::Storage::Streams::IBuffer^ get();
			}
			property Windows::Storage::Streams::IBuffer^ Icon {
				virtual Windows::Storage::Streams::IBuffer^ get();
			}
			property Platform::String^ CueSheet {
				virtual Platform::String^ get();
			}
			virtual Windows::Media::Core::MediaStreamSource^ LoadTrack();
			virtual Windows::Media::Core::MediaStreamSource^ LoadTrack(Light::AudioIndexCue^ range);
			virtual IMediaInfo^ GetTrackInfo();
		internal:
			FfmpegMediaFile(Windows::Storage::Streams::IRandomAccessStream^ stream);
			FfmpegMediaFile(Windows::Storage::IStorageFile^ file);
		private:
			~FfmpegMediaFile();
			Windows::Media::Core::MediaStreamSource^ mss;
			FfmpegAudioReader* reader;
			void OnStarting(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceStartingEventArgs ^args);
			void OnClosed(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceClosedEventArgs ^args);
			void OnSampleRequested(Windows::Media::Core::MediaStreamSource ^sender, Windows::Media::Core::MediaStreamSourceSampleRequestedEventArgs ^args);
		};
	}
}