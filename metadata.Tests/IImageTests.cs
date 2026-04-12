using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using metadata;

namespace metadata.Tests
{
    [TestFixture]
    public class IImageTests
    {
        private string _tempDir;
        private string _pngPath;
        private string _jpgPath;

        [OneTimeSetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "MetadataTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            _pngPath = Path.Combine(_tempDir, "test.png");
            _jpgPath = Path.Combine(_tempDir, "test.jpg");

            // Minimal 1x1 PNG
            var pngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            File.WriteAllBytes(_pngPath, pngBytes);

            // Minimal 1x1 JPG
            var jpgBytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEYGh0dHx8fExciJCEeJBweHx7/2wBDAQUFBQcGBw4ICA4eFBEUHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh7/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOElZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD6WooooA//2Q==");
            File.WriteAllBytes(_jpgPath, jpgBytes);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private IImage GetImageInstance(string extension)
        {
            return extension.ToLower() switch
            {
                "png" => new PngImage(_pngPath),
                "jpg" => new JpgImage(_jpgPath),
                _ => throw new ArgumentException("Unknown extension")
            };
        }

        [TestCase("png")]
        [TestCase("jpg")]
        public void Write_ValidPackage_ShouldReturnNewImageWithMetadata(string extension)
        {
            // Arrange
            IImage image = GetImageInstance(extension);
            byte[] package = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            IImage newImage = image.Write(package);

            // Assert
            Assert.That(newImage, Is.Not.Null);
            Assert.That(newImage.FilePath, Is.Not.EqualTo(image.FilePath));
            Assert.That(File.Exists(newImage.FilePath), Is.True);
        }

        [TestCase("png")]
        [TestCase("jpg")]
        public void Read_AfterWrite_ShouldReturnSamePackage(string extension)
        {
            // Arrange
            IImage originalImage = GetImageInstance(extension);
            byte[] package = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
            IImage newImage = originalImage.Write(package);

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
            IImage originalImage = GetImageInstance(extension);
            byte[] package1 = new byte[] { 0xAA, 0xBB };
            byte[] package2 = new byte[] { 0xCC, 0xDD, 0xEE };

            // Act
            IImage intermediateImage = originalImage.Write(package1);
            IImage finalImage = intermediateImage.Write(package2);
            byte[] readPackage = finalImage.Read();

            // Assert
            Assert.That(readPackage, Is.EqualTo(package2));
        }
    }
}