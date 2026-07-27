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
    private readonly Service encService = new();
    private readonly Service decService = new();

    private HelpWindow? helpWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Прикрепление файла для сокрытия (Drag&Drop)
    /// </summary>
    private void EncInput_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file != null)
            {
                string path = file.Path.LocalPath;
                encService.AttachInput(path, DataType.File);
                EncInputTextBox.Text = encService.GetMagicFileAttachMessage();
            }
        }
    }

    /// <summary>
    /// Прикрепление целевого изображения для зашифровки (Drag&Drop)
    /// </summary>
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

    /// <summary>
    /// Прикрепление целевого изображения для расшифровки (Drag&Drop)
    /// </summary>
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

    /// <summary>
    /// Открытие окна справки
    /// </summary>
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

    /// <summary>
    /// Прикрепление файла для сокрытия (кнопка)
    /// </summary>
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
            encService.AttachInput(path, DataType.File);
            EncInputTextBox.Text = encService.GetMagicFileAttachMessage();
        }
    }

    /// <summary>
    /// Прикрепление целевого изображения для зашифровки (кнопка)
    /// </summary>
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

    /// <summary>
    /// Шифрование (кнопка)
    /// </summary>
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
                encService.AttachInput(inputText, DataType.Text);

            else
            {
                var textFromAttachedFile = encService.GetMagicFileAttachMessage();

                if (inputText != textFromAttachedFile)
                    encService.AttachInput(inputText, DataType.Text);
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
            EncOutputNameText.Text = $"{ex.Message}";
        }
    }

    /// <summary>
    /// Сохранения зашифрованного изображения (кнопка)
    /// </summary>
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
            EncOutputNameText.Text = $"{ex.Message}";
        }
    }

    /// <summary>
    /// Прикрепление целевого изображения для расшифровки (кнопка)
    /// </summary>
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

    /// <summary>
    /// Расшифровка (кнопка)
    /// </summary>
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
            DecOutputTextBox.Text = $"{ex.Message}";
        }
    }

    /// <summary>
    /// Сохранения расшифрованного контента (кнопка)
    /// </summary>
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
            DecOutputTextBox.Text = $"{ex.Message}";
        }
    }
}