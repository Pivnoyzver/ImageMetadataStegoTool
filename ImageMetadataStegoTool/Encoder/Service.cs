using System;
using System.IO;

namespace ImageMetadataStegoTool;

public class Service
{
    public string? TargetImagePath { get; private set; }
    public string? AttachedFilePath { get; private set; }
    public string? AttachedText { get; private set; }
    public DataType CurrentDataType { get; private set; } = DataType.Text;

    public string? LastEncodedImagePath { get; private set; }
    public string? LastDecodedFilePath { get; private set; }
    public string? LastDecodedText { get; private set; }

    /// <summary>
    /// Выбор целевого изображения (в которое прячем или из которого читаем)
    /// </summary>
    public void SetTargetImage(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл изображения не найден.", path);

        TargetImagePath = path;
    }

    /// <summary>
    /// Ввод данных (текст или файл) для сокрытия
    /// </summary>
    public void AttachInput(string input, DataType dataType)
    {
        AttachedFilePath = null;
        AttachedText = null;
        
        switch (dataType)
        {
            case DataType.Text:
                AttachedText = input;
                break;
                
            case DataType.File:
                if (!File.Exists(input)) throw new FileNotFoundException("Прикрепляемый файл не найден.", input);
                AttachedFilePath = input;
                break;
                
            default:
                throw new ArgumentException("Неверный тип данных для прикрепления.", nameof(dataType));
        }

        CurrentDataType = dataType;
    }

    /// <summary>
    /// Зашифровать (спрятать данные (текст или файл) в целевом изображении)
    /// </summary>
    public void Encrypt()
    {
        ResetResults();

        if (string.IsNullOrEmpty(TargetImagePath))
            throw new InvalidOperationException("Целевое изображение не выбрано.");

        var input = CurrentDataType == DataType.Text ? AttachedText : AttachedFilePath;
        
        if (string.IsNullOrEmpty(input))
            throw new InvalidOperationException("Ввод шифруемых данных пуст.");

        LastEncodedImagePath = Encoder.Encode(TargetImagePath, input, CurrentDataType);
    }

    /// <summary>
    /// Расшифровать (извлечь данные из изображения)
    /// </summary>
    public void Decrypt()
    {
        ResetResults();

        if (string.IsNullOrEmpty(TargetImagePath))
            throw new InvalidOperationException("Не выбрано изображение для расшифровки.");

        var result = Encoder.Decode(TargetImagePath);

        if (result.Type == DataType.Text)
            LastDecodedText = result.Output;

        else if (result.Type == DataType.File)
            LastDecodedFilePath = result.Output;
    }

    /// <summary>
    /// Сохранить как... (сохраняет результат последней операции по указанному пути)
    /// </summary>
    public void SaveAs(string destinationPath)
    {
        if (!string.IsNullOrEmpty(LastEncodedImagePath))
            File.Copy(LastEncodedImagePath, destinationPath, overwrite: true);

        else if (!string.IsNullOrEmpty(LastDecodedFilePath))
            File.Copy(LastDecodedFilePath, destinationPath, overwrite: true);

        else if (!string.IsNullOrEmpty(LastDecodedText))
            File.WriteAllText(destinationPath, LastDecodedText);

        else
            throw new InvalidOperationException("Нет результатов для сохранения.");
    }

    public string? GetMagicFileAttachMessage()
    {
        return string.IsNullOrEmpty(AttachedFilePath)
            ? null
            : $"Файл прикреплен:\n{Path.GetFileName(AttachedFilePath)}";
    }

    private void ResetResults()
    {
        LastEncodedImagePath = null;
        LastDecodedFilePath = null;
        LastDecodedText = null;
    }
}
