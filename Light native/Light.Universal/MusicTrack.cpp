#include "pch.h"
#ifdef ENABLE_LEGACY
#include "MusicTrack.h"
using namespace Light;

#define WrapString(PropertyName) String^ MusicTrack::PropertyName::get(){\
if (track->PropertyName!=NULL)\
return ref new String(track->PropertyName);\
else \
return nullptr;\
}

//#define WrapRawBytes(PropertyName,Origin) Array<byte>^ MusicTrack::PropertyName::get(){\
//	if (track->PropertyName!=NULL)\
//	{\
//		Array<byte>^ b = ref new Array<byte>(); \
//	}\
//	else\
//		return nullptr;\
//}

#define WrapBuffer(PropertyName,Size) IBuffer^ MusicTrack::PropertyName::get(){\
if (track->PropertyName==NULL)\
return nullptr;\
byte* buffer=new byte(track->Size);\
memcpy(buffer,track->PropertyName,track->Size);\
Microsoft::WRL::ComPtr<InternalByteBuffer> bf;\
Microsoft::WRL::Details::MakeAndInitialize<InternalByteBuffer>(&bf, buffer, track->Size); \
return reinterpret_cast<Windows::Storage::Streams::IBuffer ^>(bf.Get());\
}


MusicTrack::MusicTrack(ITrack* track, memory_operation_base* memop)
{
	this->track = track;
	this->memop = memop;
}

MusicTrack::~MusicTrack()
{
	track->Destroy();
	memop->free(track);
}

WrapString(ArtistName);
WrapString(TrackTitle);
WrapString(AlbumTitle);
WrapString(Date);
WrapString(Genre);
WrapString(Composer);
WrapString(Performer);
WrapString(AlbumArtist);
WrapString(Comment);
WrapString(TrackNumber);
WrapString(TotalTracks);
WrapString(DiscNumber);
WrapString(TotalDiscs);

WrapBuffer(FrontCover, FrontCoverSize);
WrapBuffer(BackCover, FrontCoverSize);
WrapBuffer(DiscArt, DiscArtSize);
WrapBuffer(ArtistArt, ArtistArtSize);
WrapBuffer(Icon, IconSize);
#endif