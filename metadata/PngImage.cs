using System;

namespace metadata;

public class PngImage : Image
{
    public PngImage(byte[] originalImage) : base(originalImage)
    {
        ValidatePng();
    }

    private void ValidatePng()
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] data)
    {
        throw new NotImplementedException();
    }

    public override byte[] Read()
    {
        throw new NotImplementedException();
    }
}