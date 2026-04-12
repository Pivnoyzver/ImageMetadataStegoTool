namespace metadata.Tests;

[TestFixture]
public class EncoderTests
{
    private string GetTestFilePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../test", fileName));
    }

    [Test]
    public void EncodeAndDecode_Text_Jpg_ShouldWorkCorrectly()
    {
        var jpgPath = GetTestFilePath("test.jpg");
        var textToEncode = "Hello, this is a secret text in JPG!";

        var encodedImagePath = Encoder.Encode(jpgPath, textToEncode, DataType.Text);

        Assert.That(File.Exists(encodedImagePath), Is.True, "Encoded image should be created.");

        var (output, type) = Encoder.Decode(encodedImagePath);

        Assert.That(type, Is.EqualTo(DataType.Text));
        Assert.That(output, Is.EqualTo(textToEncode));

        // Cleanup
        if (File.Exists(encodedImagePath)) File.Delete(encodedImagePath);
    }

    [Test]
    public void EncodeAndDecode_Text_Png_ShouldWorkCorrectly()
    {
        var pngPath = GetTestFilePath("test.png");
        var textToEncode = "Hello, this is a secret text in PNG!";

        var encodedImagePath = Encoder.Encode(pngPath, textToEncode, DataType.Text);

        Assert.That(File.Exists(encodedImagePath), Is.True, "Encoded image should be created.");

        var (output, type) = Encoder.Decode(encodedImagePath);

        Assert.That(type, Is.EqualTo(DataType.Text));
        Assert.That(output, Is.EqualTo(textToEncode));

        // Cleanup
        if (File.Exists(encodedImagePath)) File.Delete(encodedImagePath);
    }

    [TestCase("test.jpg")]
    [TestCase("test.png")]
    public void SuperFunctionalTest_EncodeAndDecodeTestFile(string imageName)
    {
        var imagePath = GetTestFilePath(imageName);
        var txtPath = GetTestFilePath("test.TXT"); // note: exact casing as seen in dir

        // Ensure test files exist
        Assert.That(File.Exists(imagePath), Is.True, $"Test image not found at {imagePath}");
        Assert.That(File.Exists(txtPath), Is.True, $"Test text file not found at {txtPath}");

        var originalFileBytes = File.ReadAllBytes(txtPath);

        // 1. Encode the file into the image
        var encodedImagePath = Encoder.Encode(imagePath, txtPath, DataType.File);

        Assert.That(File.Exists(encodedImagePath), Is.True, "Encoded image should be created.");

        var newImageBytes = File.ReadAllBytes(encodedImagePath);
        var oldImageBytes = File.ReadAllBytes(imagePath);
        Assert.That(newImageBytes, Is.Not.EqualTo(oldImageBytes), "The encoded image should differ from the original.");

        // 2. Decode the file from the image
        var (outputFilePath, type) = Encoder.Decode(encodedImagePath);

        Assert.That(type, Is.EqualTo(DataType.File));
        Assert.That(File.Exists(outputFilePath), Is.True, "Decoded file should be created.");

        // 3. Verify the decoded file matches the original file
        var decodedFileBytes = File.ReadAllBytes(outputFilePath);
        Assert.That(decodedFileBytes, Is.EqualTo(originalFileBytes), "Decoded file content should match the original file content.");
        Assert.That(Path.GetExtension(outputFilePath).ToLower(), Is.EqualTo(Path.GetExtension(txtPath).ToLower()), "Decoded file extension should match.");

        // Cleanup
        if (File.Exists(encodedImagePath)) File.Delete(encodedImagePath);
        if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
    }
}
