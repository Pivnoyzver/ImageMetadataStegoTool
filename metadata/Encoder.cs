using System;
using System.Collections.Generic;
using System.IO;
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

        IImage image = extension switch
        {
            ".jpg" or ".jpeg" => new JpgImage(imageBytes),
            ".png" => new PngImage(imageBytes),
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
        };

        Package package;
        if (dataType == DataType.Text)
        {
            package = new Package(Encoding.UTF8.GetBytes(input));
        }
        else if (dataType == DataType.File)
        {
            var fileBytes = File.ReadAllBytes(input);
            var fileExtension = Path.GetExtension(input);
            package = new Package(fileBytes, fileExtension);
        }
        else
        {
            throw new NotSupportedException("Unsupported data type.");
        }

        var newImageBytes = image.Write(package.Serialize());

        var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutputImages");
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var newFileName = $"{Path.GetFileNameWithoutExtension(target)}_{Guid.NewGuid():N}{Path.GetExtension(target)}";
        var newFilePath = Path.Combine(outputDirectory, newFileName);

        File.WriteAllBytes(newFilePath, newImageBytes);
        return newFilePath;
    }
}
