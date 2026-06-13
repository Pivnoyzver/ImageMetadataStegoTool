using System;
using System.IO;
using System.Text;

namespace ImageMetadataStegoTool;

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

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(target);
        var ext = Path.GetExtension(target);
        var newFilePath = GetUniqueFilePath(outputDirectory, fileNameWithoutExt, ext);

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

            var newFilePath = GetUniqueFilePath(outputDirectory, "decoded", package.FileExtension);

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

    private static string GetUniqueFilePath(string directory, string fileNameWithoutExtension, string extension)
    {
        var newFileName = $"{fileNameWithoutExtension}{extension}";
        var newFilePath = Path.Combine(directory, newFileName);
        int count = 1;

        while (File.Exists(newFilePath))
        {
            newFileName = $"{fileNameWithoutExtension}({count}){extension}";
            newFilePath = Path.Combine(directory, newFileName);
            count++;
        }

        return newFilePath;
    }
}
