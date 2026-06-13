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
            var target = this.FindControl<SelectableTextBlock>("HelpContentTextBox");
            if (target == null)
                return;

            try
            {
                var uri = new Uri("avares://ImageMetadataStegoTool/Instruction.md");

                using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                using var reader = new System.IO.StreamReader(stream);

                target.Text = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                target.Text = $"Ошибка при загрузке инструкции:\n{ex.Message}";
            }
        }
    }
}