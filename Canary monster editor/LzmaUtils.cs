using System;
using System.IO;

namespace Canary_monster_editor
{
    public static class LzmaUtils
    {
        public static byte[] DecompressLzma(byte[] data)
        {
            if (data == null || data.Length < 13) return null;

            var patched = PatchLzmaHeader(data);
            if (patched != null && TryDecodeLzma(patched, useHeaderSize: false, out var decoded)) return decoded;

            if (TryDecodeLzma(data, useHeaderSize: false, out decoded)) return decoded;

            if (TryDecodeLzma(data, useHeaderSize: true, out decoded)) return decoded;
            return null;
        }

        private static bool TryDecodeLzma(byte[] data, bool useHeaderSize, out byte[] outputBytes)
        {
            outputBytes = null;
            try
            {
                using (var input = new MemoryStream(data))
                using (var output = new MemoryStream())
                {
                    byte[] props = new byte[5];
                    if (input.Read(props, 0, 5) != 5) return false;

                    var decoder = new SevenZip.Compression.LZMA.Decoder();
                    decoder.SetDecoderProperties(props);

                    byte[] sizeBytes = new byte[8];
                    if (input.Read(sizeBytes, 0, 8) != 8) return false;

                    long outSize = BitConverter.ToInt64(sizeBytes, 0);
                    if (!useHeaderSize || outSize <= 0)
                    {
                        outSize = long.MaxValue;
                    }

                    long inSize = input.Length - input.Position;
                    decoder.Code(input, output, inSize, outSize, null);
                    outputBytes = output.ToArray();
                    return outputBytes != null && outputBytes.Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static byte[] PatchLzmaHeader(byte[] data)
        {
            if (data == null || data.Length < 13) return null;
            byte[] copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);
            for (int i = 5; i < 13; i++)
            {
                copy[i] = 0xFF;
            }
            return copy;
        }
    }
}