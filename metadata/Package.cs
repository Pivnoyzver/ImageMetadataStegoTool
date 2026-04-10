using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace metadata;

public enum DataType
{
    Text,
    File
}

public class Package
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("stEG");
    public int Length => Data.Length;
    public DataType DataType { get; private set; }
    public string FileExtension { get; private set; } //TODO
    public byte[] Data { get; private set; }

    public Package(DataType dataType, byte[] data)
    {
        DataType = dataType;
        Data = (byte[])data.Clone();
    }

    public byte[] Serialize()
    {
        var result = new byte[12 + Length];
        var span = result.AsSpan();

        Magic.CopyTo(span.Slice(0, 4));
        BitConverter.TryWriteBytes(span.Slice(4, 4), Length);
        BitConverter.TryWriteBytes(span.Slice(8, 4), (int)DataType);
        Data.AsSpan().CopyTo(span.Slice(12));

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
        var data = span.Slice(startIndex + 12, length).ToArray();

        return new Package(dataType, data);
    }
}
