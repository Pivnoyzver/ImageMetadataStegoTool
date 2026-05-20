using System.Text;

namespace ImageMetadataStegoTool.Tests;

[TestFixture]
public class PackageTests
{
    [Test]
    public void Serialize_GeneralCase_ShouldReturnByteArray()
    {
        // Arrange
        var payloadString = "HelloWorld";
        var payloadBytes = Encoding.UTF8.GetBytes(payloadString);
        var binaryData = new Package(payloadBytes);

        // Act
        var result = binaryData.Serialize();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(16 + payloadBytes.Length));

        // Verify the actual payload content
        var resultPayload = result.AsSpan(16).ToArray();
        var resultString = Encoding.UTF8.GetString(resultPayload);
        Assert.That(resultString, Is.EqualTo(payloadString));
    }

    [Test]
    public void Serialize_HeaderVerification_ShouldFormCorrectSections()
    {
        // Arrange
        var payloadType = DataType.File;
        var payloadBytes = new byte[] { 0x01, 0x02, 0x03 };
        var extension = "txt";
        var extensionBytes = Encoding.UTF8.GetBytes(extension);
        var binaryData = new Package(payloadBytes, extension);

        // Act
        var result = binaryData.Serialize();

        // Assert
        // Section 1: Magic word
        var magicBytes = Encoding.ASCII.GetBytes("stEG");
        var resultMagic = result.AsSpan(0, 4).ToArray();
        Assert.That(resultMagic, Is.EqualTo(magicBytes), "Magic header is incorrect");

        // Section 2: Length
        var expectedLength = BitConverter.GetBytes(payloadBytes.Length);
        var resultLength = result.AsSpan(4, 4).ToArray();
        Assert.That(resultLength, Is.EqualTo(expectedLength), "Length header is incorrect");

        // Section 3: DataType
        var expectedDataType = BitConverter.GetBytes((int)payloadType);
        var resultDataType = result.AsSpan(8, 4).ToArray();
        Assert.That(resultDataType, Is.EqualTo(expectedDataType), "DataType header is incorrect");

        // Section 4: FileExtension Length
        var expectedExtLength = BitConverter.GetBytes(extensionBytes.Length);
        var resultExtLength = result.AsSpan(12, 4).ToArray();
        Assert.That(resultExtLength, Is.EqualTo(expectedExtLength), "FileExtension length header is incorrect");

        // Section 5: FileExtension
        var resultExt = result.AsSpan(16, extensionBytes.Length).ToArray();
        Assert.That(resultExt, Is.EqualTo(extensionBytes), "FileExtension data is incorrect");

        // Section 6: Data itself
        var resultData = result.AsSpan(16 + extensionBytes.Length).ToArray();
        Assert.That(resultData, Is.EqualTo(payloadBytes), "Payload data is incorrect");
    }

    [Test]
    public void Deserialize_SimpleCase_ShouldPopulateClassVariables()
    {
        // Arrange
        var payloadType = DataType.File;
        var payloadBytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var extension = "png";
        var extensionBytes = Encoding.UTF8.GetBytes(extension);

        var serializedData = new byte[16 + extensionBytes.Length + payloadBytes.Length];
        Encoding.ASCII.GetBytes("stEG").CopyTo(serializedData, 0);
        BitConverter.GetBytes(payloadBytes.Length).CopyTo(serializedData, 4);
        BitConverter.GetBytes((int)payloadType).CopyTo(serializedData, 8);
        BitConverter.GetBytes(extensionBytes.Length).CopyTo(serializedData, 12);
        extensionBytes.CopyTo(serializedData, 16);
        payloadBytes.CopyTo(serializedData, 16 + extensionBytes.Length);

        // Act
        var binaryData = Package.Deserialize(serializedData);

        // Assert
        Assert.That(binaryData.DataType, Is.EqualTo(payloadType));
        Assert.That(binaryData.Data, Is.EqualTo(payloadBytes));
        Assert.That(binaryData.FileExtension, Is.EqualTo(extension));
    }

    [Test]
    public void Deserialize_WithOffset_ShouldFindMagicAndPopulate()
    {
        // Arrange
        var payloadType = DataType.File;
        var payloadBytes = new byte[] { 0xFF };

        var properSerializedData = new byte[16 + payloadBytes.Length];
        Encoding.ASCII.GetBytes("stEG").CopyTo(properSerializedData, 0);
        BitConverter.GetBytes(payloadBytes.Length).CopyTo(properSerializedData, 4);
        BitConverter.GetBytes((int)payloadType).CopyTo(properSerializedData, 8);
        BitConverter.GetBytes(0).CopyTo(properSerializedData, 12);
        payloadBytes.CopyTo(properSerializedData, 16);

        // Prefix some garbage data
        var garbage = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var dataWithOffset = garbage.Concat(properSerializedData).ToArray();

        // Act
        var binaryData = Package.Deserialize(dataWithOffset);

        // Assert
        Assert.That(binaryData.DataType, Is.EqualTo(payloadType));
        Assert.That(binaryData.Data, Is.EqualTo(payloadBytes), "Should extract proper data sequence ignoring initial garbage");
        Assert.That(binaryData.FileExtension, Is.Null);
    }

    [Test]
    public void FullCycle_SerializeAndDeserialize_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var initialPayloadType = DataType.File;
        var originalBytes = Encoding.UTF8.GetBytes("Testing full cycle completeness here");
        var extension = "docx";

        var sourceData = new Package(originalBytes, extension);

        // Act
        var serialized = sourceData.Serialize();
        var restoredData = Package.Deserialize(serialized);

        // Assert
        Assert.That(restoredData.DataType, Is.EqualTo(initialPayloadType));
        Assert.That(restoredData.Data, Is.EqualTo(originalBytes), "Data integrity compromised during full cycle");
        Assert.That(restoredData.FileExtension, Is.EqualTo(extension), "FileExtension compromised during full cycle");
    }
}
