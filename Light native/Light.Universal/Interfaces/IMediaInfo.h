#pragma once

namespace Light {
	//[uuid(F437FD17-FE53-4730-83C4-F9EA92908A24)]
	public interface class IMediaInfo {
		property Platform::String^ Title { Platform::String^ get(); }
		property Platform::String^ Artist { Platform::String^ get(); }
		property Platform::String^ Album { Platform::String^ get(); }
		property Platform::String^ Date { Platform::String^ get(); }
		property Platform::String^ Composer { Platform::String^ get(); }
		property Platform::String^ Performer { Platform::String^ get(); }
		property Platform::String^ AlbumArtist { Platform::String^ get(); }
		property Platform::String^ TrackNumber { Platform::String^ get(); }
		property Platform::String^ Genre { Platform::String^ get(); }
		property Platform::String^ Grouping { Platform::String^ get(); }
		property Platform::String^ Comments { Platform::String^ get(); }
		property Platform::String^ Copyright { Platform::String^ get(); }
		property Platform::String^ Description { Platform::String^ get(); }
		property Platform::String^ TotalTracks { Platform::String^ get(); }
		property Platform::String^ DiscNumber { Platform::String^ get(); }
		property Platform::String^ TotalDiscs { Platform::String^ get(); }
		property Windows::Foundation::TimeSpan Duration { Windows::Foundation::TimeSpan get(); }
		property Windows::Foundation::Collections::IMapView<Platform::String^, Platform::String^>^ AllProperties {
			Windows::Foundation::Collections::IMapView<Platform::String^, Platform::String^>^ get();
		}
	};
}