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
        throw new System.NotImplementedException();
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
        if (bytes == null) throw new ArgumentException(nameof(bytes));
        if (bytes.Length < 2) throw new ArgumentException("Image too short");
        if (bytes[0] != 0xFF || bytes[1] != 0xD8) throw new ArgumentException("Invalid Jpeg: missing SOI marker");
    }
}