/*
 * ****************************************************
 *     Copyright (c) Aimeast.  All rights reserved.
 * ****************************************************
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Light.Text
{
    sealed class DbcsEncoding : System.Text.Encoding
    {
        private const char LeadByteChar = '\uFFFE';
        private char[] _dbcsToUnicode;
        private ushort[] _unicodeToDbcs;
        private string _webName;

        private static readonly Dictionary<string, Tuple<char[], ushort[]>> Cache;

        static DbcsEncoding()
        {
            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("Not supported big endian platform.");

            Cache = new Dictionary<string, Tuple<char[], ushort[]>>();
        }

        private DbcsEncoding() { }

        public static async Task<Encoding> GetDbcsEncoding(string name)
        {
            try
            {
                return GetEncoding(name);
            }
            catch
            {
                //not supported by system
            }
            name = name.ToLower();
            var encoding = new DbcsEncoding {_webName = name};
            if (Cache.ContainsKey(name))
            {
                var tuple = Cache[name];
                encoding._dbcsToUnicode = tuple.Item1;
                encoding._unicodeToDbcs = tuple.Item2;
                return encoding;
            }

            var dbcsToUnicode = new char[0x10000];
            var unicodeToDbcs = new ushort[0x10000];
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(
                        new Uri(string.Format("ms-appx:///EncodingMaps/{0}.bin", name)));
                using (var fs = await file.OpenReadAsync())
                using (Stream stream = fs.AsStreamForRead())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    for (int i = 0; i < 0xffff; i++)
                    {
                        ushort u = reader.ReadUInt16();
                        unicodeToDbcs[i] = u;
                    }
                    for (int i = 0; i < 0xffff; i++)
                    {
                        ushort u = reader.ReadUInt16();
                        dbcsToUnicode[i] = (char)u;
                    }
                }

                Cache[name] = new Tuple<char[], ushort[]>(dbcsToUnicode, unicodeToDbcs);
                encoding._dbcsToUnicode = dbcsToUnicode;
                encoding._unicodeToDbcs = unicodeToDbcs;
                return encoding;
            }
            catch
            {
                return null;
            }
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            var byteCount = 0;

            for (var i = 0; i < count; index++, byteCount++, i++)
            {
                var c = chars[index];
                var u = _unicodeToDbcs[c];
                if (u > 0xff)
                    byteCount++;
            }

            return byteCount;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            var byteCount = 0;

            for (var i = 0; i < charCount; charIndex++, byteIndex++, byteCount++, i++)
            {
                var c = chars[charIndex];
                var u = _unicodeToDbcs[c];
                if (u == 0 && c != 0)
                {
                    bytes[byteIndex] = 0x3f;    // 0x3f == '?'
                }
                else if (u < 0x100)
                {
                    bytes[byteIndex] = (byte)u;
                }
                else
                {
                    bytes[byteIndex] = (byte)((u >> 8) & 0xff);
                    byteIndex++;
                    byteCount++;
                    bytes[byteIndex] = (byte)(u & 0xff);
                }
            }

            return byteCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, null);
        }

        private int GetCharCount(byte[] bytes, int index, int count, DbcsDecoder decoder)
        {
            var charCount = 0;

            for (var i = 0; i < count; index++, charCount++, i++)
            {
                ushort u = 0;
                if (decoder != null && decoder.PendingByte != 0)
                {
                    u = decoder.PendingByte;
                    decoder.PendingByte = 0;
                }

                u = (ushort)(u << 8 | bytes[index]);
                var c = _dbcsToUnicode[u];
                if (c != LeadByteChar) continue;
                if (i < count - 1)
                {
                    index++;
                    i++;
                }
                else if (decoder != null)
                {
                    decoder.PendingByte = bytes[index];
                    return charCount;
                }
            }

            return charCount;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex, null);
        }

        private int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, DbcsDecoder decoder)
        {
            var charCount = 0;

            for (var i = 0; i < byteCount; byteIndex++, charIndex++, charCount++, i++)
            {
                ushort u = 0;
                if (decoder != null && decoder.PendingByte != 0)
                {
                    u = decoder.PendingByte;
                    decoder.PendingByte = 0;
                }

                u = (ushort)(u << 8 | bytes[byteIndex]);
                var c = _dbcsToUnicode[u];
                if (c == LeadByteChar)
                {
                    if (i < byteCount - 1)
                    {
                        byteIndex++;
                        i++;
                        u = (ushort)(u << 8 | bytes[byteIndex]);
                        c = _dbcsToUnicode[u];
                    }
                    else if (decoder == null)
                    {
                        c = '\0';
                    }
                    else
                    {
                        decoder.PendingByte = bytes[byteIndex];
                        return charCount;
                    }
                }
                if (c == 0 && u != 0)
                    chars[charIndex] = '?';
                else
                    chars[charIndex] = c;
            }

            return charCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount));
            long count = charCount + 1;
            count *= 2;
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(charCount));
            return (int)count;

        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            long count = byteCount + 3;
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            return (int)count;
        }

        public override Decoder GetDecoder() => new DbcsDecoder(this);

        public override string WebName => _webName;

        private sealed class DbcsDecoder : Decoder
        {
            private readonly DbcsEncoding _encoding;

            public DbcsDecoder(DbcsEncoding encoding)
            {
                _encoding = encoding;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return _encoding.GetCharCount(bytes, index, count, this);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex, this);
            }

            public byte PendingByte;
        }
    }
}
