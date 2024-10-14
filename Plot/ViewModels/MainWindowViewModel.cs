using System;
using System.Collections.Generic;
using Avalonia.Collections;
using AvaloniaEdit.Document;
using Plot.Core;
using ReactiveUI;

namespace Plot.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public TextDocument SourceDocument { get; } = new();
        public TextDocument OutputDocument { get; } = new();
        public IDictionary<string, Symbols.SymbolType> SymbolTable { get; } = new AvaloniaDictionary<string, Symbols.SymbolType>();

        public void ClearOutput()
        {
            SymbolTable.Clear();
            OutputDocument.Text = string.Empty;
        }

        public void ExecuteSource()
        {
            if (SourceDocument.TextLength == 0 || string.IsNullOrWhiteSpace(SourceDocument.Text))
            {
                return;
            }

            try
            {
                var tokenChain = Lexer.Parse(SourceDocument.Text);
 
                SymbolTable.Clear();
                OutputDocument.Insert(OutputDocument.TextLength, $"\n----- RUN {DateTime.Now:G} -----\n\n");

                foreach (var outputToken in Parser.ParseAndEval(tokenChain, SymbolTable))
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
