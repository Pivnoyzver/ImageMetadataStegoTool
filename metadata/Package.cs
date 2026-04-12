using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace metadata;

public class Package
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("stEG");
    public int Length => Data.Length;
    public DataType DataType { get; private set; }
    public string FileExtension { get; private set; }
    public byte[] Data { get; private set; }

    private Package(DataType dataType, byte[] data, string fileExtension = null)
    {
        DataType = dataType;
        Data = (byte[])data.Clone();
        FileExtension = fileExtension;
    }

    public Package(byte[] text)
    {
        DataType = DataType.Text;
        Data = (byte[])text.Clone();
    }

    public Package(byte[] bytes, string fileExtension)
    {
        DataType = DataType.File;
        Data = (byte[])bytes.Clone();
        FileExtension = fileExtension;
    }

    public byte[] Serialize()
    {
        var fileExtensionBytes = Array.Empty<byte>();
        if (DataType == DataType.File && !string.IsNullOrEmpty(FileExtension))
            fileExtensionBytes = Encoding.UTF8.GetBytes(FileExtension);

        var result = new byte[16 + fileExtensionBytes.Length + Data.Length];
        var span = result.AsSpan();

        Magic.CopyTo(span.Slice(0, 4));
        BitConverter.TryWriteBytes(span.Slice(4, 4), Data.Length);
        BitConverter.TryWriteBytes(span.Slice(8, 4), (int)DataType);
        BitConverter.TryWriteBytes(span.Slice(12, 4), fileExtensionBytes.Length);

        if (fileExtensionBytes.Length > 0)
            fileExtensionBytes.CopyTo(span.Slice(16));

        Data.AsSpan().CopyTo(span.Slice(16 + fileExtensionBytes.Length));

        return result;
    }

    public static Package Deserialize(byte[] package)
    {
        ReadOnlySpan<byte> span = package.AsSpan();

        var startIndex = span.IndexOf(Magic);
        if (startIndex == -1) throw new ArgumentException("Magic not found");

        var length = BitConverter.ToInt32(span.Slice(startIndex + 4, 4));
        if (length < 0) throw new ArgumentException("Invalid payload length");

        var dataType = (DataType)BitConverter.ToInt32(span.Slice(startIndex + 8, 4));
        var fileExtensionLength = BitConverter.ToInt32(span.Slice(startIndex + 12, 4));

        string fileExtension = null;
        if (fileExtensionLength > 0)
            fileExtension = Encoding.UTF8.GetString(span.Slice(startIndex + 16, fileExtensionLength));

        var data = span.Slice(startIndex + 16 + fileExtensionLength, length).ToArray();

        return new Package(dataType, data, fileExtension);
    }
}
