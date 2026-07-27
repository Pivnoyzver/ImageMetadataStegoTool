using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.IO;

namespace ImageMetadataStegoTool
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            LoadInstructionContent();
        }

        private void LoadInstructionContent()
        {
            // Ищем наш новый элемент MarkdownScrollViewer
            var target = this.FindControl<Markdown.Avalonia.MarkdownScrollViewer>("HelpContentMarkdown");
            if (target == null)
                return;

            try
            {
                var uri = new Uri("avares://ImageMetadataStegoTool/Instruction.md");

                using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                using var reader = new System.IO.StreamReader(stream);

                // Записываем прочитанный текст в свойство Markdown
                target.Markdown = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                target.Markdown = $"**Ошибка при загрузке инструкции:**\n{ex.Message}";
            }
        }
    }
}