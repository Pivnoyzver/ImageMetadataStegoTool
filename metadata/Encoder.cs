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
        if (dataType == DataType.Text)
        {
            var package = new Package(Encoding.UTF8.GetBytes(input));

            var image = new JpgImage(target);
            var newImage = image.Write(package.Serialize());
            return newImage.FilePath;
        }

        if (dataType == DataType.File)
        {
            var fileBytes = File.ReadAllBytes(input);
            var fileExtension = Path.GetExtension(input);

            var package = new Package(fileBytes, fileExtension);

            var image = new JpgImage(target);
            var newImage = image.Write(package.Serialize());
            return newImage.FilePath;
        }

        throw new NotSupportedException("Unsupported data type.");
    }
}
