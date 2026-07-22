using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;

using Avalonia.Input;
using System.Linq;

namespace ImageMetadataStegoTool;

public partial class MainWindow : Window
{
    private readonly Service encService = new Service();
    private readonly Service decService = new Service();

    private HelpWindow? helpWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void EncInput_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file != null)
            {
                string path = file.Path.LocalPath;
                encService.AttachFile(path);
                EncInputTextBox.Text = encService.GetMagicFileAttachMessage();
            }
        }
    }

    private void EncImage_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file != null)
            {
                string path = file.Path.LocalPath;
                string ext = Path.GetExtension(path).ToLower();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    encService.SetTargetImage(path);
                    EncTargetImageText.Text = Path.GetFileName(path);

                    (EncTargetImagePreview.Source as IDisposable)?.Dispose();
                    EncTargetImagePreview.Source = new Bitmap(path);
                }
            }
        }
    }

    private void DecImage_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file != null)
            {
                string path = file.Path.LocalPath;
                string ext = Path.GetExtension(path).ToLower();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    decService.SetTargetImage(path);
                    DecTargetImageText.Text = Path.GetFileName(path);

                    (DecTargetImagePreview.Source as IDisposable)?.Dispose();
                    DecTargetImagePreview.Source = new Bitmap(path);
                }
            }
        }
    }

    private void HelpBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (helpWindow == null || !helpWindow.IsVisible)
        {
            helpWindow = new HelpWindow();

            helpWindow.Closed += (s, args) => helpWindow = null;
            helpWindow.Show(this);
        }
        else
        {
            helpWindow.Activate();
        }
    }

    // --- ENCRYPTION -----------------------------------------

    private async void EncAttachFileBtn_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файл для скрытия",
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            string path = files[0].Path.LocalPath;
            encService.AttachFile(path);
            EncInputTextBox.Text = encService.GetMagicFileAttachMessage();
        }
    }

    private async void EncAttachImageBtn_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите картинку",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }
            ]
        });

        if (files.Count > 0)
        {
            string path = files[0].Path.LocalPath;
            encService.SetTargetImage(path);
            EncTargetImageText.Text = Path.GetFileName(path);

            (EncTargetImagePreview.Source as IDisposable)?.Dispose();
            EncTargetImagePreview.Source = new Bitmap(path);
        }
    }

    private void EncodeBtn_Click(object? sender, RoutedEventArgs e)
    {
        EncOutputImagePreview.Source = null;
        EncOutputNameText.Text = null;

        try
        {
            var inputText = EncInputTextBox.Text;

            if (string.IsNullOrEmpty(inputText))
                throw new Exception("Введите текст или выберите файл.");

            if (string.IsNullOrEmpty(encService.AttachedFilePath))
                encService.AttachText(inputText);

            else
            {
                var textFromAttachedFile = encService.GetMagicFileAttachMessage();

                if (inputText != textFromAttachedFile)
                    encService.AttachText(inputText);
            }

            encService.Encrypt();
            EncOutputNameText.Text = $"{Path.GetFileName(encService.LastEncodedImagePath)}";

            if (!string.IsNullOrEmpty(encService.LastEncodedImagePath))
            {
                (EncOutputImagePreview.Source as IDisposable)?.Dispose();
                EncOutputImagePreview.Source = new Bitmap(encService.LastEncodedImagePath);
            }
        }
        catch (Exception ex)
        {
            EncOutputNameText.Text = $"Ошибка:\n{ex.Message}";
        }
    }

    private async void EncSaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string? suggestPath = null;

            if (!string.IsNullOrEmpty(encService.LastEncodedImagePath))
                suggestPath = encService.LastEncodedImagePath;

            else
                throw new Exception("Нет результатов для сохранения.");

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить закодированное изображение как...",
                SuggestedFileName = Path.GetFileName(suggestPath)
            });

            if (file != null)
                encService.SaveAs(file.Path.LocalPath);
        }
        catch (Exception ex)
        {
            EncOutputNameText.Text = $"Ошибка сохранения:\n{ex.Message}";
        }
    }

    // --- DECRYPTION -----------------------------------------

    private async void DecAttachImageBtn_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите закодированную картинку",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }
            ]
        });

        if (files.Count > 0)
        {
            string path = files[0].Path.LocalPath;
            decService.SetTargetImage(path);
            DecTargetImageText.Text = Path.GetFileName(path);

            (DecTargetImagePreview.Source as IDisposable)?.Dispose();
            DecTargetImagePreview.Source = new Bitmap(path);
        }
    }

    private void DecodeBtn_Click(object? sender, RoutedEventArgs e)
    {
        DecOutputTextBox.Text = null;

        try
        {
            decService.Decrypt();

            if (decService.LastDecodedText != null)
                DecOutputTextBox.Text = decService.LastDecodedText;

            else if (decService.LastDecodedFilePath != null)
                DecOutputTextBox.Text = $"Извлечен файл:\n{Path.GetFileName(decService.LastDecodedFilePath)}";
        }
        catch (Exception ex)
        {
            DecOutputTextBox.Text = $"Ошибка:\n{ex.Message}";
        }
    }

    private async void DecSaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string? suggestPath = null;

            if (!string.IsNullOrEmpty(decService.LastDecodedFilePath))
                suggestPath = decService.LastDecodedFilePath;

            else if (!string.IsNullOrEmpty(decService.LastDecodedText))
                suggestPath = "decoded_text.txt";

            else
                throw new Exception("Нет результатов для сохранения.");

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить расшифрованное как...",
                SuggestedFileName = Path.GetFileName(suggestPath)
            });

            if (file != null)
                decService.SaveAs(file.Path.LocalPath);

        }
        catch (Exception ex)
        {
            DecOutputTextBox.Text = $"Ошибка сохранения:\n{ex.Message}";
        }
    }
}