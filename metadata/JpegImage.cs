using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace metadata;

public class JpegImage : IImage
{

    private  byte[] imageBytes;

    public JpegImage(byte[] originalImage)
    {
        ValidateJpeg(originalImage);
        imageBytes = (byte[])originalImage.Clone();
    }
    public byte[] Read()
    {
        var xmpHeader = Encoding.ASCII.GetBytes(@"http://ns.adobe.com/xap/1.0/\0");
        var index = 2;

        while (index < imageBytes.Length - 1)
        {
            if (imageBytes[index] != 0xFF) throw new ArgumentException("Invalid Jpeg semgent marker");

            var marker = imageBytes[index + 1];
            index += 2;

            if (marker == 0xD9 || marker == 0xDA) break;

            if (index + 1 >= imageBytes.Length) throw new ArgumentException("Invalid JPEG segment length");

            var segmentLength = (imageBytes[index] << 8) | imageBytes[index + 1];
            if (segmentLength < 2 || index + segmentLength > imageBytes.Length) throw new ArgumentException("Corrupted JPEG segment");

            var dataStart = index + 2;
            var dataLength = segmentLength - 2;

            if (marker == 0xE1 && StartWith(imageBytes, dataStart, xmpHeader))
            {
                var xmlStart = dataStart + xmpHeader.Length;
                var xmlLength = dataLength - xmpHeader.Length;

                var xml = Encoding.UTF8.GetString(imageBytes, xmlStart, xmlLength);

                var openTag = "<steg:Data>";
                var closeTag = "</steg:Data>";

                var contentStart = xml.IndexOf(openTag, StringComparison.Ordinal);
                if (contentStart == -1)
                    throw new ArgumentException("Data tag not found");

                contentStart += openTag.Length;

                var contentEnd = xml.IndexOf(closeTag, contentStart, StringComparison.Ordinal);
                if (contentEnd == -1)
                    throw new ArgumentException("Closing data tag not found");

                var base64 = xml.Substring(contentStart, contentEnd);
                return Convert.FromBase64String(base64);
            }

            index += segmentLength;
        }

        throw new ArgumentException("XMP metadata not found");
    }

    public void Write(byte[] package)
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
        var xmpHeader = Encoding.ASCII.GetBytes(@"http://ns.adobe.com/xap/1.0/\0");
        var payload = new byte[xmpHeader.Length + xmlBytes.Length];

        Buffer.BlockCopy(xmpHeader, 0, payload, 0, xmpHeader.Length);
        Buffer.BlockCopy(xmlBytes, 0, payload, xmpHeader.Length, xmlBytes.Length);

        var segmentLength = payload.Length + 2;
        
        if (segmentLength > ushort.MaxValue) throw new ArgumentException("XMP packet is too large :(");

        var result = new List<byte>
        {
            0xFF,
            0xD8,
            0xFF,
            0xE1,
            (byte)(segmentLength >> 8),
            (byte)(segmentLength & 0xFF)
        };

        result.AddRange(payload);

        for (var i = 2; i < imageBytes.Length; i++)
            result.Add(imageBytes[i]);

        imageBytes = result.ToArray();
    }

    public byte[] GetBytes()
    {
        return (byte[])imageBytes.Clone();
    }

    private static void ValidateJpeg(byte[] bytes)
    {
        if (bytes == null) 
            throw new ArgumentException(nameof(bytes));

        if (bytes.Length < 2) 
            throw new ArgumentException("Image too short");
            
        if (bytes[0] != 0xFF || bytes[1] != 0xD8) 
            throw new ArgumentException("Invalid Jpeg: missing SOI marker");
    }

    private static bool StartWith(byte[] source, int startIndex, byte[] prefix)
    {
        if (startIndex + prefix.Length > source.Length) return false; 

        for (var i = 0; i < prefix.Length; i++)
            if (source[startIndex + i] != prefix[i])
                return false;

        return true;
    }
}