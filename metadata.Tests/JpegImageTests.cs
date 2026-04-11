using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace metadata;

[TestFixture]
public class JpegImageTests
{
    private static byte[] CreateMinimalJpeg()
    {
        return
        [
            0xFF, 0xD8, // SOI
            0xFF, 0xD9  // EOI
        ];
    }

    [Test]
    public void Constructor_ShouldThrow_WhenImageIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new JpegImage(null!));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenImageIsTooShort()
    {
        var bytes = new byte[] { 0xFF };

        Assert.Throws<ArgumentException>(() => new JpegImage(bytes));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenImageIsNotJpeg()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        Assert.Throws<ArgumentException>(() => new JpegImage(bytes));
    }

    [Test]
    public void Write_Then_Read_ShouldReturnSamePackage()
    {
        var originalImage = CreateMinimalJpeg();
        var jpegImage = new JpegImage(originalImage);
        var package = Encoding.UTF8.GetBytes("hello world");

        jpegImage.Write(package);
        var result = jpegImage.Read();

        Assert.That(result, Is.EqualTo(package).AsCollection);
    }

    [Test]
    public void Write_ShouldNotModifyOriginalInputArray()
    {
        var originalImage = CreateMinimalJpeg();
        var originalCopy = (byte[])originalImage.Clone();
        var jpegImage = new JpegImage(originalImage);
        var package = Encoding.UTF8.GetBytes("secret");

        jpegImage.Write(package);

        Assert.That(originalImage, Is.EqualTo(originalCopy).AsCollection);
    }

    [Test]
    public void Write_ShouldChangeInternalImageBytes()
    {
        var originalImage = CreateMinimalJpeg();
        var jpegImage = new JpegImage(originalImage);
        var package = Encoding.UTF8.GetBytes("secret");

        jpegImage.Write(package);
        var resultBytes = jpegImage.GetBytes();

        Assert.That(originalImage.SequenceEqual(resultBytes), Is.False);
    }

    [Test]
    public void Read_ShouldThrow_WhenMetadataNotFound()
    {
        var originalImage = CreateMinimalJpeg();
        var jpegImage = new JpegImage(originalImage);

        Assert.Throws<ArgumentException>(() => jpegImage.Read());
    }

    [Test]
    public void GetBytes_ShouldReturnModifiedImage()
    {
        var originalImage = CreateMinimalJpeg();
        var jpegImage = new JpegImage(originalImage);
        var package = Encoding.UTF8.GetBytes("test");

        jpegImage.Write(package);
        var resultBytes = jpegImage.GetBytes();

        Assert.That(resultBytes, Has.Length.GreaterThan(originalImage.Length));
    }
}