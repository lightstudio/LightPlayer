#pragma once
#ifdef ENABLE_LEGACY
#include <tiostream.h>
#include "AsyncHelper.h"

using namespace TagLib;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

class taglib_readonly_io :
	public IOStream
{
public:
	taglib_readonly_io(IStorageFile^ file);
	taglib_readonly_io(IRandomAccessStream^ stream, TagLib::wstring extension);
	void io_close();
	FileName name() const;
	ByteVector readBlock(ulong length);
	void writeBlock(const ByteVector &data);
	void insert(const ByteVector &data, ulong start = 0, ulong replace = 0);
	void removeBlock(ulong start = 0, ulong length = 0);
	bool readOnly() const;
	bool isOpen() const;
	void seek(long offset, Position p = Beginning);
	void clear();
	long tell() const;
	long length();
	void truncate(long length);
private:
	IRandomAccessStream^ stream;
	TagLib::wstring fileName;
	bool close;
};
#endif