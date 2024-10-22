using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using Plot.Core;
using Plot.Models;
using ReactiveUI;

namespace Plot.ViewModels;

public class DocumentEditorViewModel : ReactiveObject, IDisposable
{
    private IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> _graphingFunctions;

    public DocumentEditorViewModel()
        : this(new PlotScriptDocument())
    {
    }

    public DocumentEditorViewModel(PlotScriptDocument document)
    {
        Document = document;

        SourceDocument.Text = document.SourceText ?? string.Empty;
        SourceDocument.TextChanged += (sender, args) => document.SourceText = ((TextDocument)sender)!.Text;
    }

    public PlotScriptDocument Document { get; }

    public TextDocument SourceDocument { get; } = new();
    public TextDocument OutputDocument { get; } = new();

    public string FileName => Document.FileName;

    public IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphingFunctions
    {
        get => _graphingFunctions;
        private set => this.RaiseAndSetIfChanged(ref _graphingFunctions, value);
    }

    public async Task SaveDocument(IStorageFile saveAs = null)
    {
        await Document.SaveDocument(saveAs);
        this.RaisePropertyChanged(nameof(FileName));
    }

    public void ExecuteScript()
    {
        if (SourceDocument.TextLength == 0 || string.IsNullOrWhiteSpace(SourceDocument.Text))
        {
            return;
        }

        try
        {
            Document.SourceText = SourceDocument.Text;

            if (OutputDocument.TextLength > 0)
            {
                OutputDocument.Insert(OutputDocument.TextLength, "\n\n");
            }

            OutputDocument.Insert(OutputDocument.TextLength, $"----- RUN {DateTime.Now:G} -----\n");

            LinkedList<Symbols.SymbolType.PlotScriptGraphingFunction> graphingFunctionsList = null;

            foreach (var outputToken in Document.ExecuteScript())
            {
                switch (outputToken)
                {
                    case Symbols.SymbolType.PlotScriptGraphingFunction gf:
                        graphingFunctionsList ??= [];
                        graphingFunctionsList.AddLast(gf);
                        break;

                    default:
                        OutputDocument.Insert(OutputDocument.TextLength, $"\n> {outputToken}");
                        break;
                }
            }

            GraphingFunctions = graphingFunctionsList;
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

    public void Dispose()
    {
        Document?.Dispose();
    }
}