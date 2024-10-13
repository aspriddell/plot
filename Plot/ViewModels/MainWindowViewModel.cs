using System;
using System.Windows.Input;
using AvaloniaEdit.Document;
using Plot.Core;
using ReactiveUI;

namespace Plot.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel()
        {
            ClearOutput = ReactiveCommand.Create(() => OutputDocument.Text = "");
            ExecuteSource = ReactiveCommand.Create(ParseAndExecute);
        }

        public TextDocument SourceDocument { get; } = new();
        public TextDocument OutputDocument { get; } = new();
        
        public ICommand ClearOutput { get; }
        public ICommand ExecuteSource { get; }

        private void ParseAndExecute()
        {
            if (SourceDocument.TextLength == 0 || string.IsNullOrWhiteSpace(SourceDocument.Text))
            {
                return;
            }

            try
            {
                var tokenChain = Lexer.Parse(SourceDocument.Text);

                OutputDocument.Insert(OutputDocument.TextLength, $"\n----- RUN {DateTime.Now:G} -----\n\n");

                foreach (var outputToken in Parser.ParseAndEval(tokenChain))
                {
                    OutputDocument.Insert(OutputDocument.TextLength, $"> {outputToken}\n");
                }
            }
            catch (Lexer.LexerException e)
            {
                OutputDocument.Insert(OutputDocument.TextLength, $"\n----- {e.Message} -----\n");
            }
            catch (Exception e)
            {
                OutputDocument.Insert(OutputDocument.TextLength, $"\n----- Unexpected error: {e.Message} -----\n");
            }
        }
    }
}
