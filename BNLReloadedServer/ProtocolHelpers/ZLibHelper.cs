using System.IO.Compression;

namespace BNLReloadedServer.ProtocolHelpers;

public static class ZLibHelper
{
    extension(byte[] buffer)
    {
        public MemoryStream UnZip()
        {
            var compressedStream = new MemoryStream(buffer);
            var decompressedStream = new MemoryStream();
            using var zStream = new ZLibStream(compressedStream, CompressionMode.Decompress, true);
            zStream.CopyTo(decompressedStream);
            decompressedStream.Seek(0L, SeekOrigin.Begin);
            return decompressedStream;
        }

        public MemoryStream Zip(int level)
        {
            var compressedStream = new MemoryStream();
            var compressor = new ZLibStream(compressedStream, (CompressionLevel) level, true);
            compressor.Write(buffer, 0, buffer.Length);
            compressor.Close();
            compressedStream.Seek(0L, SeekOrigin.Begin);
            return compressedStream;
        }
    }
}