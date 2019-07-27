#pragma once

namespace Light {
	//[uuid(6218A804-1E0F-490F-BAEA-BE09169101AB)]
	public interface class IMediaFile {
		property Windows::Storage::Streams::IBuffer^ FrontCover {
			Windows::Storage::Streams::IBuffer^ get();
		}
		property Windows::Storage::Streams::IBuffer^ BackCover {
			Windows::Storage::Streams::IBuffer^ get();
		}
		property Windows::Storage::Streams::IBuffer^ DiscScan {
			Windows::Storage::Streams::IBuffer^ get();
		}
		property Windows::Storage::Streams::IBuffer^ ArtistImage {
			Windows::Storage::Streams::IBuffer^ get();
		}
		property Windows::Storage::Streams::IBuffer^ Icon {
			Windows::Storage::Streams::IBuffer^ get();
		}
		property Platform::String^ CueSheet {
			Platform::String^ get();
		}
		Windows::Media::Core::MediaStreamSource^ LoadTrack();
		Windows::Media::Core::MediaStreamSource^ LoadTrack(Light::AudioIndexCue^ range);
		IMediaInfo^ GetTrackInfo();
	};
}