#pragma once
#ifdef ENABLE_LEGACY
#include "Extension\config.h"
#include "Extension\lightapi_all.h"
#include "InternalByteBuffer.h"
using namespace Platform;
using namespace Windows::Storage::Streams;

namespace Light
{
	[Windows::Foundation::Metadata::DeprecatedAttribute(L"This class is deprecated. Use Light.IMediaFile or Light.IMediaInfo instead.", Windows::Foundation::Metadata::DeprecationType::Deprecate, 1)]
	public ref class MusicTrack sealed
	{
	public:
		DECLARE_READONLY_PROPERTY(String^, ArtistName);
		DECLARE_READONLY_PROPERTY(String^, TrackTitle);
		DECLARE_READONLY_PROPERTY(String^, AlbumTitle);
		DECLARE_READONLY_PROPERTY(String^, Date);
		DECLARE_READONLY_PROPERTY(String^, Genre);
		DECLARE_READONLY_PROPERTY(String^, Composer);
		DECLARE_READONLY_PROPERTY(String^, Performer);
		DECLARE_READONLY_PROPERTY(String^, AlbumArtist);
		DECLARE_READONLY_PROPERTY(String^, Comment);
		DECLARE_READONLY_PROPERTY(String^, TrackNumber);
		DECLARE_READONLY_PROPERTY(String^, TotalTracks);
		DECLARE_READONLY_PROPERTY(String^, DiscNumber);
		DECLARE_READONLY_PROPERTY(String^, TotalDiscs);

		DECLARE_READONLY_PROPERTY(IBuffer^, FrontCover);
		DECLARE_READONLY_PROPERTY(IBuffer^, BackCover);
		DECLARE_READONLY_PROPERTY(IBuffer^, DiscArt);
		DECLARE_READONLY_PROPERTY(IBuffer^, ArtistArt);
		DECLARE_READONLY_PROPERTY(IBuffer^, Icon);
	internal:
		MusicTrack(ITrack* track, memory_operation_base* memop);
		property ITrack* track;
		property memory_operation_base* memop;
	private:
		MusicTrack() {}
		~MusicTrack();
	};
}
#endif