using System;
using NUnit.Framework;

namespace metadata.Tests
{
    [TestFixture]
    public class IImageTests
    {
        private byte[] _pngBytes;
        private byte[] _jpgBytes;

        [OneTimeSetUp]
        public void Setup()
        {
            // Minimal 1x1 PNG
            _pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

            // Minimal 1x1 JPG
            _jpgBytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEYGh0dHx8fExciJCEeJBweHx7/2wBDAQUFBQcGBw4ICA4eFBEUHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh7/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOElZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD6WooooA//2Q==");
        }

        private IImage GetImageInstance(string extension, byte[] imageBytes)
        {
            return extension.ToLower() switch
            {
                "png" => new PngImage(imageBytes),
                "jpg" => new JpgImage(imageBytes),
                _ => throw new ArgumentException("Unknown extension")
            };
        }

        [TestCase("png")]
        [TestCase("jpg")]
        public void Write_ValidPackage_ShouldReturnNewImageBytes(string extension)
        {
            // Arrange
            byte[] originalBytes = extension == "png" ? _pngBytes : _jpgBytes;
            IImage image = GetImageInstance(extension, originalBytes);
            byte[] package = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            byte[] newImageBytes = image.Write(package);

            // Assert
            Assert.That(newImageBytes, Is.Not.Null);
            Assert.That(newImageBytes, Is.Not.EqualTo(originalBytes));
            Assert.That(newImageBytes.Length, Is.GreaterThan(originalBytes.Length));
        }

        [TestCase("png")]
        [TestCase("jpg")]
        public void Read_AfterWrite_ShouldReturnSamePackage(string extension)
        {
            // Arrange
            byte[] originalBytes = extension == "png" ? _pngBytes : _jpgBytes;
            IImage originalImage = GetImageInstance(extension, originalBytes);
            byte[] package = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
            
            byte[] newImageBytes = originalImage.Write(package);
            IImage newImage = GetImageInstance(extension, newImageBytes);

            // Act
            byte[] readPackage = newImage.Read();

            // Assert
            Assert.That(readPackage, Is.EqualTo(package));
        }

        [TestCase("png")]
        [TestCase("jpg")]
        public void MultipleWrites_ShouldOverwriteDataAndNotAccumulate(string extension)
        {
            // Arrange
            byte[] originalBytes = extension == "png" ? _pngBytes : _jpgBytes;
            IImage originalImage = GetImageInstance(extension, originalBytes);
            byte[] package1 = new byte[] { 0xAA, 0xBB };
            byte[] package2 = new byte[] { 0xCC, 0xDD, 0xEE };

            // Act
            byte[] intermediateImageBytes = originalImage.Write(package1);
            IImage intermediateImage = GetImageInstance(extension, intermediateImageBytes);
            
            byte[] finalImageBytes = intermediateImage.Write(package2);
            IImage finalImage = GetImageInstance(extension, finalImageBytes);
            
            byte[] readPackage = finalImage.Read();

            // Assert
            Assert.That(readPackage, Is.EqualTo(package2));
        }
    }
}