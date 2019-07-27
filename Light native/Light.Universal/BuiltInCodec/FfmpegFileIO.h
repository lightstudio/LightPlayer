#pragma once

extern int IStreamRead(void* ptr, uint8_t* buf, int bufSize);
extern int64_t IStreamSeek(void* ptr, int64_t pos, int whence);

int crt_read_packet(void *opaque, uint8_t *buf, int buf_size);
int crt_write_packet(void *opaque, uint8_t *buf, int buf_size);
int64_t crt_seek(void *opaque, int64_t offset, int whence);