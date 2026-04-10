using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metadata;

public enum PayloadType
{
    Text,
    Binary,
    Json,
    File
}

public class BinaryData
{
    public const string Magic = "STEG";
    public PayloadType DataType { get; set; }
    public int Length => Data.Length;
    public byte[] Data { get; set; } = [];

    public byte[] Serialize()
    {
        throw new NotImplementedException();
    }

    public void Deserialize(byte[] data)
    {
        throw new NotImplementedException();
    }
}

