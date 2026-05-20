using System;
using System.IO;
using System.Text;

namespace ImageMetadataStegoTool;

public class JpgImage : IImage
{
    private readonly byte[] imageBytes;

    public JpgImage(byte[] originalImage)
    {
        if (originalImage == null)
            throw new ArgumentNullException(nameof(originalImage));
        if (originalImage.Length < 2)
            throw new ArgumentException("Image too short");
        if (originalImage[0] != 0xFF || originalImage[1] != 0xD8)
            throw new ArgumentException("Invalid Jpeg: missing SOI marker");

        imageBytes = (byte[])originalImage.Clone();
    }

    public byte[] Read()
    {
        var xmpHeader = "http://ns.adobe.com/xap/1.0/\0"u8.ToArray();
        var index = 2;

        while (index < imageBytes.Length - 1)
        {
            if (imageBytes[index] != 0xFF)
                throw new ArgumentException("Invalid Jpg segment marker");

            var marker = imageBytes[index + 1];
            index += 2;

            if (marker == 0xD9 || marker == 0xDA)
                break;

            if (index + 1 >= imageBytes.Length)
                throw new ArgumentException("Invalid JPG segment length");

            var segmentLength = (imageBytes[index] << 8) | imageBytes[index + 1];
            if (segmentLength < 2 || index + segmentLength > imageBytes.Length)
                throw new ArgumentException("Corrupted JPG segment");

            var dataStart = index + 2;
            var dataLength = segmentLength - 2;

            if (marker == 0xE1 && StartWith(imageBytes, dataStart, xmpHeader))
            {
                var xmlStart = dataStart + xmpHeader.Length;
                var xmlLength = dataLength - xmpHeader.Length;

                var xml = Encoding.UTF8.GetString(imageBytes, xmlStart, xmlLength);

                const string openTag = "<steg:Data>";
                const string closeTag = "</steg:Data>";

                var contentStart = xml.IndexOf(openTag, StringComparison.Ordinal);
                if (contentStart == -1)
                    throw new ArgumentException("Data tag not found");

                contentStart += openTag.Length;

                var contentEnd = xml.IndexOf(closeTag, contentStart, StringComparison.Ordinal);
                if (contentEnd == -1)
                    throw new ArgumentException("Closing data tag not found");

                var base64 = xml.Substring(contentStart, contentEnd - contentStart);
                return Convert.FromBase64String(base64);
            }

            index += segmentLength;
        }

        throw new ArgumentException("XMP metadata not found");
    }

    public byte[] Write(byte[] package)
    {
        var packageBase64 = Convert.ToBase64String(package);

        var xml =
        $"""
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description xmlns:steg="http://example.com/steg/">
                    <steg:Data>{packageBase64}</steg:Data>
                </rdf:Description>
            </rdf:RDF>
        </x:xmpmeta>
        """;
        var xmlBytes = Encoding.UTF8.GetBytes(xml);
        var xmpHeader = "http://ns.adobe.com/xap/1.0/\0"u8.ToArray();

        var segmentLength = xmpHeader.Length + xmlBytes.Length + 2;
        if (segmentLength > ushort.MaxValue)
            throw new ArgumentException("XMP packet is too large :(");

        using var ms = new MemoryStream(imageBytes.Length + segmentLength + 4);
        ms.WriteByte(0xFF);
        ms.WriteByte(0xD8);

        ms.WriteByte(0xFF);
        ms.WriteByte(0xE1);
        ms.WriteByte((byte)(segmentLength >> 8));
        ms.WriteByte((byte)(segmentLength & 0xFF));

        ms.Write(xmpHeader, 0, xmpHeader.Length);
        ms.Write(xmlBytes, 0, xmlBytes.Length);

        // Пишем оригинальное тело картинки, пропуская маркер SOI (первые 2 байта)
        ms.Write(imageBytes, 2, imageBytes.Length - 2);

        return ms.ToArray();
    }

    private static bool StartWith(byte[] source, int startIndex, ReadOnlySpan<byte> prefix)
    {
        if (startIndex + prefix.Length > source.Length) return false;

        for (var i = 0; i < prefix.Length; i++)
            if (source[startIndex + i] != prefix[i])
                return false;

        return true;
    }
}
