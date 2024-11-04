using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using Plot.Core;
using Plot.Models;
using ReactiveUI;

namespace Plot.ViewModels;

public class DocumentEditorViewModel : ReactiveObject, IDisposable
{
    private readonly Subject<Guid> _modificationSignal = new();
    private readonly ObservableAsPropertyHelper<bool> _sourceModified;

    // md5 of the last saved version of the document.
    // it's not great but the change tracker in the TextDocument can't work out if a series of changes amount to nothing
    private byte[] _lastSavedVersion; 
    private IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> _graphingFunctions;

    public DocumentEditorViewModel()
        : this(new PlotScriptDocument())
    {
    }

    public DocumentEditorViewModel(PlotScriptDocument document)
    {
        Document = document;

        SourceDocument.Text = document.SourceText ?? string.Empty;
        SourceDocument.TextChanged += (sender, _) =>
        {
            document.SourceText = ((TextDocument)sender)!.Text;
            _modificationSignal.OnNext(Guid.NewGuid());
        };

        if (Document.IsBackedByFile)
        {
            LastSavedVersion = MD5.HashData(Encoding.UTF8.GetBytes(Document.SourceText));
        }

        this.WhenAnyValue(x => x.LastSavedVersion)
            .CombineLatest(_modificationSignal.StartWith(Guid.Empty)) // used to trigger forced re-evaluations
            .Throttle(TimeSpan.FromMilliseconds(500))                 // throttle to prevent overly-frequent updates
            .Select(x =>
            {
                if (x.First != null)
                {
                    return !x.First.SequenceEqual(MD5.HashData(Encoding.UTF8.GetBytes(Document.SourceText)));
                }

                return Document.SourceText?.Length > 0;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.IsModified, out _sourceModified);

        this.WhenAnyValue(x => x.IsModified)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(TabContent)));
    }

    public PlotScriptDocument Document { get; }

    public TextDocument SourceDocument { get; } = new();
    public TextDocument OutputDocument { get; } = new();

    public bool IsModified => _sourceModified.Value;

    private byte[] LastSavedVersion
    {
        get => _lastSavedVersion;
        set => this.RaiseAndSetIfChanged(ref _lastSavedVersion, value);
    }

    public string FileName => Document.FileName;
    public string TabContent => $"{FileName}{(IsModified ? "*" : string.Empty)}";
    
    public Interaction<System.Reactive.Unit, System.Reactive.Unit> OpenGraphWindowInteraction { get; } = new();

    public IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphingFunctions
    {
        get => _graphingFunctions;
        private set => this.RaiseAndSetIfChanged(ref _graphingFunctions, value);
    }

    public async Task SaveDocument(IStorageFile saveAs = null)
    {
        await Document.SaveDocument(saveAs);

        this.RaisePropertyChanged(nameof(FileName));
        LastSavedVersion = MD5.HashData(Encoding.UTF8.GetBytes(Document.SourceText));
    }

    internal void ExecuteScript()
    {
        if (SourceDocument.TextLength == 0 || string.IsNullOrWhiteSpace(SourceDocument.Text))
        {
            return;
        }

        try
        {
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
                        if (!outputToken.IsUnit)
                        {
                            OutputDocument.Insert(OutputDocument.TextLength, $"\n> {outputToken}");
                        }

                        break;
                }
            }

            GraphingFunctions = graphingFunctionsList;
            MessageBus.Current.SendMessage(new PlotFunctionsChangedEvent(GraphingFunctions));
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