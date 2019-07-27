#pragma once

#include "AVDictionaryView.h"

#define ReadOnlyPropertyWithBackend(name, type) private: type m_##name; \
		public: virtual property type name { type get() { return m_##name; } }

namespace Light {
	namespace BuiltInCodec {
		[/*uuid(DA02CC53-36C1-4508-9724-40E8EFBC8573),*/ Windows::UI::Xaml::Data::Bindable]
		public ref class FfmpegMediaInfo sealed : public IMediaInfo {
		public:
			ReadOnlyPropertyWithBackend(Title, Platform::String^)
			ReadOnlyPropertyWithBackend(Artist, Platform::String^)
			ReadOnlyPropertyWithBackend(Album, Platform::String^)
			ReadOnlyPropertyWithBackend(Date, Platform::String^)
			ReadOnlyPropertyWithBackend(Composer, Platform::String^)
			ReadOnlyPropertyWithBackend(Performer, Platform::String^)
			ReadOnlyPropertyWithBackend(AlbumArtist, Platform::String^)
			ReadOnlyPropertyWithBackend(TrackNumber, Platform::String^)
			ReadOnlyPropertyWithBackend(Genre, Platform::String^)
			ReadOnlyPropertyWithBackend(Grouping, Platform::String^)
			ReadOnlyPropertyWithBackend(Comments, Platform::String^)
			ReadOnlyPropertyWithBackend(Copyright, Platform::String^)
			ReadOnlyPropertyWithBackend(Description, Platform::String^)
			ReadOnlyPropertyWithBackend(TotalTracks, Platform::String^)
			ReadOnlyPropertyWithBackend(DiscNumber, Platform::String^)
			ReadOnlyPropertyWithBackend(TotalDiscs, Platform::String^)
			ReadOnlyPropertyWithBackend(Duration, Windows::Foundation::TimeSpan)

		private: 
			Windows::Foundation::Collections::IMapView<Platform::String^, Platform::String^>^ m_AllProperties;
		
		public: 
			virtual property Windows::Foundation::Collections::IMapView<Platform::String^, Platform::String^>^ AllProperties
			{
				Windows::Foundation::Collections::IMapView<Platform::String^, Platform::String^>^ get() 
				{ return m_AllProperties; }
			}
		
		internal:
			void Initialize(int64_t duration, AVDictionary *metadata)
			{
				m_Duration = Windows::Foundation::TimeSpan{ duration };
				
				m_Title = getDictValue(metadata, "title");
				m_Album = getDictValue(metadata, "album");
				m_AlbumArtist = getDictValue(metadata, "album_artist");
				m_Artist = getDictValue(metadata, "artist");
				m_Composer = getDictValue(metadata, "composer");
				m_Date = getDictValue(metadata, "date");

				splitDictValue(metadata, "track", m_TrackNumber, m_TotalTracks);
				splitDictValue(metadata, "disc", m_DiscNumber, m_TotalDiscs);
				
				m_Genre = getDictValue(metadata, "genre");
				m_Performer = getDictValue(metadata, "performer");
				m_Grouping = getDictValue(metadata, "grouping");
				m_Comments = getDictValue(metadata, "comment");
				m_Copyright = getDictValue(metadata, "copyright");
				m_Description = getDictValue(metadata, "description");

				Microsoft::WRL::Details::Make<AVDictionaryView>(metadata).CopyTo(
					reinterpret_cast<ABI::Windows::Foundation::Collections::IMapView<HSTRING, HSTRING>**>(&m_AllProperties));
			}
		};
	}
}
