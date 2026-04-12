using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metadata;

public class Encoder
{
    public static string Encode(string target, string input, DataType dataType)
    {
        var imageBytes = File.ReadAllBytes(target);
        var extension = Path.GetExtension(target).ToLowerInvariant();

        var image = GetImage(extension, imageBytes);
        var package = GetPackage(dataType, input);

        var newImageBytes = image.Write(package.Serialize());

        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutputImages");
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        var newFileName = $"{Path.GetFileNameWithoutExtension(target)}_{Guid.NewGuid():N}{Path.GetExtension(target)}";

        var newFilePath = Path.Combine(outputDirectory, newFileName);

        File.WriteAllBytes(newFilePath, newImageBytes);
        return newFilePath;
    }

    public static (string Output, DataType Type) Decode(string target)
    {
        var imageBytes = File.ReadAllBytes(target);
        var extension = Path.GetExtension(target).ToLowerInvariant();

        var image = GetImage(extension, imageBytes);

        var payloadBytes = image.Read();
        var package = Package.Deserialize(payloadBytes);

        if (package.DataType == DataType.Text)
        {
            var text = Encoding.UTF8.GetString(package.Data);
            return (text, DataType.Text);
        }

        else if (package.DataType == DataType.File)
        {
            var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DecodedFiles");
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var newFileName = $"decoded_{Guid.NewGuid():N}{package.FileExtension}";
            var newFilePath = Path.Combine(outputDirectory, newFileName);

            File.WriteAllBytes(newFilePath, package.Data);
            return (newFilePath, DataType.File);
        }

        throw new NotSupportedException("Unsupported data type in package.");
    }

    private static IImage GetImage(string extension, byte[] imageBytes)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => new JpgImage(imageBytes),
            ".png" => new PngImage(imageBytes),
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
        };
    }

    private static Package GetPackage(DataType dataType, string input)
    {
        if (dataType == DataType.Text)
            return new Package(Encoding.UTF8.GetBytes(input));
        else if (dataType == DataType.File)
            return new Package(File.ReadAllBytes(input), Path.GetExtension(input));
        else
            throw new NotSupportedException("Unsupported data type.");
    }
}