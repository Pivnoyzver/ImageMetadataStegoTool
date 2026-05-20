using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;

using Avalonia.Input;
using System.Linq;

namespace metadata
{
    public partial class MainWindow : Window
    {
        private Service service = new Service();

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
                    service.AttachFile(path);
                    EncInputTextBox.Text = $"Файл прикреплен:\n{path}";
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
                        service.SetTargetImage(path);
                        EncTargetImageText.Text = Path.GetFileName(path);
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
                        service.SetTargetImage(path);
                        DecTargetImageText.Text = Path.GetFileName(path);
                        DecTargetImagePreview.Source = new Bitmap(path);
                    }
                }
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
                service.AttachFile(path);
                EncInputTextBox.Text = $"Файл прикреплен:\n{path}";
            }
        }

        private async void EncAttachImageBtn_Click(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите картинку",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } }
                }
            });

            if (files.Count > 0)
            {
                string path = files[0].Path.LocalPath;
                service.SetTargetImage(path);
                EncTargetImageText.Text = Path.GetFileName(path);
                EncTargetImagePreview.Source = new Bitmap(path);
            }
        }

        private void EncodeBtn_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (service.CurrentDataType == DataType.Text || string.IsNullOrEmpty(service.AttachedFilePath))
                {
                    // Если пользователь вписал текст самостоятельно
                    if (!string.IsNullOrWhiteSpace(EncInputTextBox.Text) && !EncInputTextBox.Text.StartsWith("Файл прикреплен:"))
                    {
                        service.AttachText(EncInputTextBox.Text);
                    }
                    else if (string.IsNullOrEmpty(service.AttachedFilePath))
                    {
                        throw new Exception("Введите текст или выберите файл.");
                    }
                }

                service.Encrypt();
                EncOutputNameText.Text = $"Успех!\nЗакодировано:\n{Path.GetFileName(service.LastEncodedImagePath)}";
                EncOutputImagePreview.Source = new Bitmap(service.LastEncodedImagePath);
            }
            catch (Exception ex)
            {
                EncOutputNameText.Text = $"Ошибка:\n{ex.Message}";
            }
        }

        private async void HelpBtn_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Справка",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "пупупу",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    FontSize = 20
                }
            };
            await dialog.ShowDialog(this);
        }

        private async void EncSaveBtn_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? suggestPath = null;

                if (!string.IsNullOrEmpty(service.LastEncodedImagePath))
                    suggestPath = service.LastEncodedImagePath;

                else
                    throw new Exception("Нет результатов для сохранения.");

                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Сохранить закодированное изображение как...",
                    SuggestedFileName = Path.GetFileName(suggestPath)
                });

                if (file != null)
                {
                    service.SaveAs(file.Path.LocalPath);
                    EncOutputNameText.Text += "\n(Успешно сохранено)";
                }
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
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg"} }
                }
            });

            if (files.Count > 0)
            {
                string path = files[0].Path.LocalPath;
                service.SetTargetImage(path);
                DecTargetImageText.Text = Path.GetFileName(path);
                DecTargetImagePreview.Source = new Bitmap(path);
            }
        }

        private void DecodeBtn_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                service.Decrypt();

                if (service.LastDecodedText != null)
                {
                    DecOutputTextBox.Text = service.LastDecodedText;
                }
                else if (service.LastDecodedFilePath != null)
                {
                    DecOutputTextBox.Text = $"Извлечен файл:\n{Path.GetFileName(service.LastDecodedFilePath)}";
                }
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

                if (!string.IsNullOrEmpty(service.LastDecodedFilePath))
                    suggestPath = service.LastDecodedFilePath;

                else if (!string.IsNullOrEmpty(service.LastDecodedText))
                    suggestPath = "decoded_text.txt";

                else
                    throw new Exception("Нет результатов для сохранения.");

                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Сохранить расшифрованное как...",
                    SuggestedFileName = Path.GetFileName(suggestPath)
                });

                if (file != null)
                {
                    service.SaveAs(file.Path.LocalPath);
                    if (service.LastDecodedText != null)
                        DecOutputTextBox.Text = service.LastDecodedText + "\n\n(Успешно сохранено)";
                    else if (service.LastDecodedFilePath != null)
                        DecOutputTextBox.Text = $"Извлечен файл:\n{Path.GetFileName(service.LastDecodedFilePath)}\n\n(Успешно сохранено)";
                }
            }
            catch (Exception ex)
            {
                DecOutputTextBox.Text = $"Ошибка сохранения:\n{ex.Message}";
            }
        }
    }
}