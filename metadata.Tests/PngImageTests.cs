using System;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace metadata.Tests;

[TestFixture]
public class PngImageTests
{
    [Test]
    public void Constructor_ShouldThrow_WhenImageIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new PngImage(null!));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenImageIsNotPng()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Throws<ArgumentException>(() => new PngImage(bytes));
    }

    [Test]
    public void Read_ShouldThrow_WhenMetadataDoesNotExist()
    {
        var image = new PngImage(CreateMinimalPng());

        Assert.Throws<ArgumentException>(() => image.Read());
    }

    [Test]
    public void Write_ThenRead_ShouldReturnWrittenBytes()
    {
        var image = new PngImage(CreateMinimalPng());
        var package = Encoding.UTF8.GetBytes("hello png");

        image.Write(package);
        var result = image.Read();

        Assert.That(result, Is.EqualTo(package));
    }

    [Test]
    public void Write_Twice_ShouldReplaceOldMetadata()
    {
        var image = new PngImage(CreateMinimalPng());
        var firstPackage = Encoding.UTF8.GetBytes("first");
        var secondPackage = Encoding.UTF8.GetBytes("second");

        image.Write(firstPackage);
        image.Write(secondPackage);

        var result = image.Read();

        Assert.That(result, Is.EqualTo(secondPackage));
    }

    [Test]
    public void GetBytes_ShouldReturnValidPngAfterWrite()
    {
        var image = new PngImage(CreateMinimalPng());
        var package = Encoding.UTF8.GetBytes("secret");

        image.Write(package);
        var bytes = image.GetBytes;

        Assert.That(bytes.Take(8).ToArray(), Is.EqualTo(new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10
        }));
    }

    [Test]
    public void WrittenBytes_ShouldBeReadableFromNewInstance()
    {
        var package = Encoding.UTF8.GetBytes("shared data");
        var image = new PngImage(CreateMinimalPng());

        image.Write(package);
        var savedBytes = image.GetBytes;

        var restoredImage = new PngImage(savedBytes);
        var result = restoredImage.Read();

        Assert.That(result, Is.EqualTo(package));
    }

    private static byte[] CreateMinimalPng()
    {
        return new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10,

            0, 0, 0, 13,
            73, 72, 68, 82,
            0, 0, 0, 1,
            0, 0, 0, 1,
            8,
            2,
            0,
            0,
            0,
            144, 119, 83, 222,

            0, 0, 0, 0,
            73, 68, 65, 84,
            53, 175, 6, 30,

            0, 0, 0, 0,
            73, 69, 78, 68,
            174, 66, 96, 130
        };
    }
}