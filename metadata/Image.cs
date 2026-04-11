using System;

namespace metadata;

public abstract class Image
{
    protected byte[] imageBytes;

    protected Image(byte[] originalImage)
    {
        ValidateImage(originalImage);
        imageBytes = (byte[])originalImage.Clone();
    }

    public byte[] ImageBytes => (byte[])imageBytes.Clone();
    
    private static void ValidateImage(byte[] bytes) => ArgumentNullException.ThrowIfNull(bytes);
}