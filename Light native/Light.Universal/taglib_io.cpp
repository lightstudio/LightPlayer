#include "pch.h"
#ifdef ENABLE_LEGACY
#include "taglib_io.h"



taglib_readonly_io::taglib_readonly_io(IStorageFile^ file)
{
	this->stream = AWait(file->OpenReadAsync());
	this->fileName = TagLib::wstring(file->Name->Data());
	close = true;
}
taglib_readonly_io::taglib_readonly_io(IRandomAccessStream^ stream, TagLib::wstring extension)
{
	this->stream = stream;
	this->fileName = L"File." + extension;
	close = false;
}
void taglib_readonly_io::io_close()
{
	if (close&&stream)
	{
		delete stream;
		stream = nullptr;
	}

}
FileName taglib_readonly_io::name() const
{
	return FileName(fileName.data());
}
ByteVector taglib_readonly_io::readBlock(ulong length)
{
	Platform::Array<BYTE>^ buffer = ref new Platform::Array<BYTE>(length);
	auto reader = ref new DataReader(stream);
	auto bytes = AWait(reader->LoadAsync(length));
	auto pBytes = ref new Platform::Array<unsigned char>(bytes);
	reader->ReadBytes(pBytes);
	reader->DetachStream();
	auto buf = new unsigned char[bytes];
	memcpy(buf, pBytes->Data, bytes);
	return ByteVector((char*) buf, bytes);
}
void taglib_readonly_io::writeBlock(const ByteVector &data)
{
	//Not implemented
}
void taglib_readonly_io::insert(const ByteVector &data, ulong start, ulong replace)
{
	//Not implemented
}
void taglib_readonly_io::removeBlock(ulong start, ulong length)
{
	//Not implemented
}
bool taglib_readonly_io::readOnly() const
{
	return true;
}
bool taglib_readonly_io::isOpen() const
{
	return stream != nullptr;
}
void taglib_readonly_io::seek(long offset, Position p)
{
	//file->seek(offset, p);
	switch (p)
	{
	case Position::Beginning:
		stream->Seek(offset);
		break;
	case Position::Current:
		stream->Seek(stream->Position + offset);
		break;
	case Position::End:
		stream->Seek(stream->Size - offset);
		break;
	}
}
void taglib_readonly_io::clear()
{
	//Not implemented
}
long taglib_readonly_io::tell() const
{
	return stream->Position;
}
long taglib_readonly_io::length()
{
	return stream->Size;
}
void taglib_readonly_io::truncate(long length)
{
	//Not implemented
}
#endif