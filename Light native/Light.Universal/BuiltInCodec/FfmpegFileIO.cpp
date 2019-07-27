#include "pch.h"
#include "AsyncHelper.h"
#include <wrl.h>
#include <shcore.h>
#include "FfmpegFileIO.h"
extern "C"
{
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
}
using namespace Microsoft::WRL;
using namespace Windows::Storage::Streams;
using namespace Platform;

int IStreamRead(void* ptr, uint8_t* buf, int bufSize)
{
	IStream* pStream = reinterpret_cast<IStream*>(ptr);
	ULONG bytesRead = 0;
	HRESULT hr = pStream->Read(buf, bufSize, &bytesRead);

	if (FAILED(hr))
	{
		return -1;
	}

	// If we succeed but don't have any bytes, assume end of file
	if (bytesRead == 0)
	{
		return AVERROR_EOF;  // Let FFmpeg know that we have reached eof
	}

	return bytesRead;
}

// Static function to seek in file stream. Credit to Philipp Sch http://www.codeproject.com/Tips/489450/Creating-Custom-FFmpeg-IO-Context
int64_t IStreamSeek(void* ptr, int64_t pos, int whence)
{
	IStream* pStream = reinterpret_cast<IStream*>(ptr);
	if (whence == AVSEEK_SIZE)
	{
		STATSTG stats;
		if (FAILED(pStream->Stat(&stats, 0)))
			return -1;
		return stats.cbSize.QuadPart;
	}
	LARGE_INTEGER in;
	in.QuadPart = pos;
	ULARGE_INTEGER out = { 0 };

	if (FAILED(pStream->Seek(in, whence, &out)))
	{
		return -1;
	}

	return out.QuadPart; // Return the new position:
}

int crt_read_packet(void *opaque, uint8_t *buf, int buf_size)
{
	return (int)fread(buf, sizeof(unsigned char), buf_size, (FILE*) opaque);
}
int crt_write_packet(void *opaque, uint8_t *buf, int buf_size)
{
	//not implemented
	return -1;
}
int64_t crt_seek(void *opaque, int64_t offset, int whence)
{
	if (whence == AVSEEK_SIZE)
	{
		struct _stat64 buf;
		auto fd = _fileno((FILE*)opaque);
		_fstat64(fd, &buf);
		return buf.st_size;
	}
	else
	{
		return fseek((FILE*) opaque, (long)offset, whence);
	}
}