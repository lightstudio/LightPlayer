#pragma once
#ifdef ENABLE_LEGACY
#include <tag.h>
#include <tiostream.h>
#include <tstring.h>
#include <id3v2frame.h>
#include <attachedpictureframe.h>
#include <tbytevector.h>
#include <mpegfile.h> //mp3 file
#include <id3v2tag.h>

using namespace TagLib::ID3v2;

#include "asffile.h"
#include "mpegfile.h"
#include "vorbisfile.h"
#include "flacfile.h"
#include "oggflacfile.h"
#include "mpcfile.h"
#include "mp4file.h"
#include "wavpackfile.h"
#include "speexfile.h"
#include "opusfile.h"
#include "trueaudiofile.h"
#include "aifffile.h"
#include "wavfile.h"
#include "apefile.h"
#include "modfile.h"
#include "s3mfile.h"
#include "itfile.h"
#include "xmfile.h"

#include "taglib_io.h"
using namespace Windows::Foundation;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

namespace Light
{
	public ref class MusicTag sealed
	{
	public:
		MusicTag(IStorageFile^ file);
		MusicTag(IStorageFile^ file, bool CorrectEncoding);
		MusicTag(IRandomAccessStream^ stream, Platform::String^ FileType_withDot);
		MusicTag(IRandomAccessStream^ stream, Platform::String^ FileType_withDot, bool CorrectEncoding);
		property Platform::String^ Album;
		property Platform::String^ Artist;
		property Platform::String^ Comment;
		property Platform::String^ Genre;
		property Platform::String^ Title;
		property unsigned int Year;
		property unsigned int TrackNumber;
	private:
		bool correctEncoding;
		~MusicTag();
		__forceinline void ReadTag();
		__forceinline Platform::String^ ToPlatformString(TagLib::String str);
		__forceinline File* InternalGetFile(TagLib::String ext, bool readAudioProperties = true, AudioProperties::ReadStyle audioPropertiesStyle = AudioProperties::Average);
		taglib_readonly_io *pIO;
	};
}
#endif