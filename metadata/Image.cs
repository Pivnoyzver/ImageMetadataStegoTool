using System;

namespace metadata;

public abstract class Image : IImage
{
    protected byte[] imageBytes;

    protected Image(byte[] originalImage)
    {
        ValidateImage(originalImage);
        imageBytes = (byte[])originalImage.Clone();
    }

    public byte[] GetBytes => (byte[])imageBytes.Clone();

    public abstract void Write(byte[] data);

    public abstract byte[] Read();
    
    private static void ValidateImage(byte[] bytes) => ArgumentNullException.ThrowIfNull(bytes);
}