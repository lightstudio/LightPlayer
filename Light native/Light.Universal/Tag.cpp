#include "pch.h"
#ifdef ENABLE_LEGACY
#include "Tag.h"
#include "AppValidate.h"

using namespace Light;


MusicTag::MusicTag(IStorageFile^ file) 
{
	this->correctEncoding = false;
	pIO = new taglib_readonly_io(file);
	ReadTag();
}

MusicTag::MusicTag(IStorageFile^ file, bool CorrectEncoding)
{
	//ValidateLicense
	this->correctEncoding = CorrectEncoding;
	pIO = new taglib_readonly_io(file);
	ReadTag();
}
MusicTag::MusicTag(IRandomAccessStream^ stream, Platform::String^ FileType_withDot)
{
	//ValidateLicense
	this->correctEncoding = false;
	pIO = new taglib_readonly_io(stream, TagLib::wstring(FileType_withDot->Data()));
	ReadTag();
}

MusicTag::MusicTag(IRandomAccessStream^ stream, Platform::String^ FileType_withDot, bool CorrectEncoding)
{
	//ValidateLicense
	this->correctEncoding = CorrectEncoding;
	pIO = new taglib_readonly_io(stream, TagLib::wstring(FileType_withDot->Data()));
	ReadTag();
}

MusicTag::~MusicTag()
{
	pIO->io_close();
}

Platform::String^ MusicTag::ToPlatformString(TagLib::String str)
{
	if (correctEncoding)
	{
		auto bL = str.isLatin1();
		auto bytes = str.data(bL ? TagLib::String::Type::Latin1 : TagLib::String::Type::UTF8);
		auto b = bytes.data();
		Platform::Array<byte>^ pb = ref new Platform::Array<byte>(bytes.size());
		for (auto i = 0; i < bytes.size(); i++)
		{
			pb[i] = b[i];
		}
		return Light::dotNetTools::StringEncodingHelper::GetStringFromBytes(pb);
	}
	auto st_w = str.toWString();
	auto st = st_w.data();
	if (st != L""&&st != NULL)
		return ref new Platform::String(st);
	else
		return L"";
}

File* MusicTag::InternalGetFile(TagLib::String ext, bool readAudioProperties, AudioProperties::ReadStyle audioPropertiesStyle)
{
	if (!ext.isEmpty())
	{
		if (ext == "MP3")
			return new TagLib::MPEG::File(pIO, ID3v2::FrameFactory::instance());
		if (ext == "OGG")
			return new TagLib::Ogg::Vorbis::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "OGA") {
			/* .oga can be any audio in the Ogg container. First try FLAC, then Vorbis. */
			File *file = new TagLib::Ogg::FLAC::File(pIO, readAudioProperties, audioPropertiesStyle);
			if (file->isValid())
				return file;
			delete file;
			return new TagLib::Ogg::Vorbis::File(pIO, readAudioProperties, audioPropertiesStyle);
		}
		if (ext == "FLAC")
			return new TagLib::FLAC::File(pIO, ID3v2::FrameFactory::instance());
		
		if (ext == "MPC")
			return new TagLib::MPC::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "WV")
			return new TagLib::WavPack::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "SPX")
			return new TagLib::Ogg::Speex::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "OPUS")
			return new TagLib::Ogg::Opus::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "TTA")
			return new TagLib::TrueAudio::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "M4A" || ext == "M4R" || ext == "M4B" || ext == "M4P" || ext == "MP4" || ext == "3G2" || ext == "AAC")
			return new TagLib::MP4::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "WMA" || ext == "ASF")
			return new TagLib::ASF::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "AIF" || ext == "AIFF")
			return new TagLib::RIFF::AIFF::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "WAV")
			return new TagLib::RIFF::WAV::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "APE")
			return new TagLib::APE::File(pIO, readAudioProperties, audioPropertiesStyle);
		// module, nst and wow are possible but uncommon extensions
		if (ext == "MOD" || ext == "MODULE" || ext == "NST" || ext == "WOW")
			return new TagLib::Mod::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "S3M")
			return new TagLib::S3M::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "IT")
			return new TagLib::IT::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "XM")
			return new TagLib::XM::File(pIO, readAudioProperties, audioPropertiesStyle);
		if (ext == "TAK")
			return new TagLib::APE::File(pIO, readAudioProperties, audioPropertiesStyle);
	}
	return 0;
}

void MusicTag::ReadTag()
{
	TagLib::String ext;
	TagLib::String s = TagLib::String(pIO->name().wstr());
	const int pos = s.rfind(".");
	if (pos != -1)
		ext = s.substr(pos + 1).upper();
	auto tf = InternalGetFile(ext);
	if (!tf) return; //not supported
	auto tag = tf->tag();
	Album = ToPlatformString(tag->album());
	if (Album == L"") Album = nullptr;
	Artist = ToPlatformString(tag->artist());
	if (Artist == L"") Artist = nullptr;
	Comment = ToPlatformString(tag->comment());
	Genre = ToPlatformString(tag->genre());
	Title = ToPlatformString(tag->title());
	if (Title == L"") Title = nullptr;
	TrackNumber = tag->track();
	
	Year = tag->year();
}
#endif