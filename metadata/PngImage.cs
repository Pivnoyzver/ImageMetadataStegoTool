using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace metadata;

public class PngImage : IImage
{
    private static readonly byte[] PngSignature =
    [
        137, 80, 78, 71, 13, 10, 26, 10
    ];

    private const string MetadataKeyword = "HiddenData";

    public string FilePath { get; private set; }

    public PngImage(string filePath)
    {
        FilePath = filePath;
        
        Span<byte> header = stackalloc byte[8];
        using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
        if (fs.Read(header) < 8)
            throw new ArgumentException("Image too short");
            
        for (var i = 0; i < PngSignature.Length; i++)
        {
            if (header[i] != PngSignature[i])
                throw new ArgumentException("Invalid PNG signature");
        }
    }

    public IImage Write(byte[] package)
    {
        var imageBytes = File.ReadAllBytes(FilePath);
        var chunks = ReadChunks(imageBytes);
        
        chunks.RemoveAll(chunk => chunk.Type == "tEXt" && IsOurTextChunk(chunk.Data));

        var textChunk = CreateTextChunk(package);

        var iendIndex = chunks.FindIndex(chunk => chunk.Type == "IEND");
        if (iendIndex < 0)
            throw new InvalidOperationException("PNG does not contain IEND chunk");

        chunks.Insert(iendIndex, textChunk);
        var newImageBytes = BuildPng(chunks);
        
        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutputImages");
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var newFileName = $"{Path.GetFileNameWithoutExtension(FilePath)}_{Guid.NewGuid():N}{Path.GetExtension(FilePath)}";
        var newFilePath = Path.Combine(outputDirectory, newFileName);

        File.WriteAllBytes(newFilePath, newImageBytes);

        return new PngImage(newFilePath);
    }

    public byte[] Read()
    {
        var imageBytes = File.ReadAllBytes(FilePath);
        var chunks = ReadChunks(imageBytes);

        var chunk = chunks.FirstOrDefault(c => c.Type == "tEXt" && IsOurTextChunk(c.Data));

        if (chunk == null)
            throw new ArgumentException("PNG metadata not found");

        var zeroIndex = Array.IndexOf(chunk.Data, (byte)0);
        if (zeroIndex < 0 || zeroIndex == chunk.Data.Length - 1)
            throw new ArgumentException("Corrupted PNG metadata");

        var base64 = Encoding.ASCII.GetString(
            chunk.Data,
            zeroIndex + 1,
            chunk.Data.Length - zeroIndex - 1);

        return Convert.FromBase64String(base64);
    }

    private static List<PngChunk> ReadChunks(byte[] imageBytes)
    {
        var chunks = new List<PngChunk>();
        var offset = PngSignature.Length;

        while (offset < imageBytes.Length)
        {
            if (offset + 8 > imageBytes.Length)
                throw new ArgumentException("Invalid PNG structure");

            var length = BinaryPrimitives.ReadInt32BigEndian(imageBytes.AsSpan(offset, 4));
            offset += 4;

            if (length < 0)
                throw new ArgumentException("Invalid PNG chunk length");

            var type = Encoding.ASCII.GetString(imageBytes, offset, 4);
            offset += 4;

            if (offset + length + 4 > imageBytes.Length)
                throw new ArgumentException("Invalid PNG chunk data");

            var data = imageBytes.AsSpan(offset, length).ToArray();
            offset += length;

            var crc = BinaryPrimitives.ReadUInt32BigEndian(imageBytes.AsSpan(offset, 4));
            offset += 4;

            chunks.Add(new PngChunk(type, data, crc));

            if (type == "IEND")
                break;
        }

        return chunks;
    }

    private static byte[] BuildPng(List<PngChunk> chunks)
    {
        var result = new List<byte>(PngSignature);

        foreach (var chunk in chunks)
        {
            var lengthBytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthBytes, chunk.Data.Length);
            result.AddRange(lengthBytes);

            var typeBytes = Encoding.ASCII.GetBytes(chunk.Type);
            result.AddRange(typeBytes);
            result.AddRange(chunk.Data);

            var crc = CalculateCrc(typeBytes, chunk.Data);

            var crcBytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
            result.AddRange(crcBytes);
        }

        return result.ToArray();
    }

    private static PngChunk CreateTextChunk(byte[] package)
    {
        var keywordBytes = Encoding.ASCII.GetBytes(MetadataKeyword);
        var textBytes = Encoding.ASCII.GetBytes(Convert.ToBase64String(package));

        var chunkData = new byte[keywordBytes.Length + 1 + textBytes.Length];
        Buffer.BlockCopy(keywordBytes, 0, chunkData, 0, keywordBytes.Length);
        chunkData[keywordBytes.Length] = 0;
        Buffer.BlockCopy(textBytes, 0, chunkData, keywordBytes.Length + 1, textBytes.Length);

        return new PngChunk("tEXt", chunkData, 0);
    }

    private static bool IsOurTextChunk(byte[] data)
    {
        var zeroIndex = Array.IndexOf(data, (byte)0);
        if (zeroIndex < 0)
            return false;

        var keyword = Encoding.ASCII.GetString(data, 0, zeroIndex);
        return keyword == MetadataKeyword;
    }

    private static uint CalculateCrc(byte[] typeBytes, byte[] data)
    {
        var bytes = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, bytes, 0, typeBytes.Length);
        Buffer.BlockCopy(data, 0, bytes, typeBytes.Length, data.Length);

        uint crc = 0xFFFFFFFF;

        foreach (var b in bytes)
        {
            crc ^= b;

            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xEDB88320
                    : crc >> 1;
            }
        }

        return ~crc;
    }

    private class PngChunk(string type, byte[] data, uint crc)
    {
        public string Type { get; } = type;
        public byte[] Data { get; } = data;
        public uint Crc { get; } = crc;
    }
}