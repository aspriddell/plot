using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using Plot.Core;
using Plot.Models;
using ReactiveUI;

namespace Plot.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private static readonly FilePickerFileType PlotScriptType = new("PlotScript File")
    {
        AppleUniformTypeIdentifiers = ["moe.ppc.plotscript"],
        Patterns = ["*.plotscript"],
        MimeTypes = ["text/plain"]
    };
        
    private IStorageFile _openedFile;
    private PlotScriptDocument _activeDocument = new();

    public MainWindowViewModel()
    {
        ClearOutput = ReactiveCommand.Create(ClearOutputImpl);
        SaveOutput = ReactiveCommand.CreateFromTask(SaveOutputImpl);
        CopyOutput = ReactiveCommand.CreateFromTask(CopyOutputImpl);
            
        OpenScript = ReactiveCommand.CreateFromTask(OpenScriptImpl);
        SaveScript = ReactiveCommand.CreateFromTask(SaveScriptImpl);
        SaveScriptAs = ReactiveCommand.CreateFromTask(SaveScriptAsImpl);

        CopyToClipboardInteraction = new Interaction<string, Unit>();
        SaveFileDialogInteraction = new Interaction<FilePickerSaveOptions, IStorageFile>();
        OpenFileDialogInteraction = new Interaction<FilePickerOpenOptions, IReadOnlyCollection<IStorageFile>>();

        this.WhenAnyValue(x => x.ActiveDocument)
            .Where(x => x != null)
            .Subscribe(d =>
            {
                SourceDocument.Text = d.SourceText ?? "";
                
                this.RaisePropertyChanged(nameof(WindowTitle));
                this.RaisePropertyChanged(nameof(FileLoaded));
            });
    }

    public string WindowTitle => $"{App.Current.Name} - {ActiveDocument.FileName}";
    public bool FileLoaded => ActiveDocument?.IsBackedByFile == true;

    public TextDocument SourceDocument { get; } = new();
    public TextDocument OutputDocument { get; } = new();

    public PlotScriptDocument ActiveDocument
    {
        get => _activeDocument;
        set => this.RaiseAndSetIfChanged(ref _activeDocument, value);
    }

    public Interaction<string, Unit> CopyToClipboardInteraction { get; }
    public Interaction<FilePickerSaveOptions, IStorageFile> SaveFileDialogInteraction { get; }
    public Interaction<FilePickerOpenOptions, IReadOnlyCollection<IStorageFile>> OpenFileDialogInteraction { get; }
        
    public ICommand CopyOutput { get; }
    public ICommand SaveOutput { get; }
    public ICommand ClearOutput { get; }
        
    public ICommand OpenScript { get; }
    public ICommand SaveScript { get; }
    public ICommand SaveScriptAs { get; }

    private void ClearOutputImpl()
    {
        OutputDocument.Text = string.Empty;
    }
        
    private async Task CopyOutputImpl()
    {
        await CopyToClipboardInteraction.Handle(OutputDocument.Text).ToTask();
    }

    private async Task SaveOutputImpl()
    {
        var saveOptions = new FilePickerSaveOptions
        {
            DefaultExtension = ".txt",
            SuggestedFileName = "plot-output.txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Text file")
                {
                    AppleUniformTypeIdentifiers = ["public.plain-text"],
                    MimeTypes = ["text/plain"],
                    Patterns = ["*.txt"]
                }
            ]
        };
            
        var file = await SaveFileDialogInteraction.Handle(saveOptions).ToTask();
        if (file == null)
        {
            return;
        }

        await using var writeStream = await file.OpenWriteAsync();
        await using var fileWriter = new StreamWriter(writeStream, Encoding.UTF8);

        await fileWriter.WriteAsync(OutputDocument.Text);
    }

    private async Task OpenScriptImpl()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Open file",
            AllowMultiple = false,
            FileTypeFilter = [PlotScriptType, new FilePickerFileType("All files")
            {
                AppleUniformTypeIdentifiers = ["public.item"],
                MimeTypes = ["*/*"],
                Patterns = ["*"]
            }]
        };
            
        var selectedFiles = await OpenFileDialogInteraction.Handle(options).ToTask();
        if (selectedFiles.Count != 1)
        {
            return;
        }

        ActiveDocument = await PlotScriptDocument.LoadFileAsync(selectedFiles.Single());
    }

    private async Task SaveScriptImpl()
    {
        ActiveDocument.SourceText = SourceDocument.Text;
        
        if (ActiveDocument.IsBackedByFile)
        {
            await ActiveDocument.SaveDocument(SourceDocument.Text);
            return;
        }
        
        await SaveScriptAsImpl();
    }

    private async Task SaveScriptAsImpl()
    {
        var saveDialog = new FilePickerSaveOptions
        {
            DefaultExtension = ".plotscript",
            FileTypeChoices = [PlotScriptType]
        };

        var file = await SaveFileDialogInteraction.Handle(saveDialog).ToTask();
        if (file == null)
        {
            return;
        }

        await ActiveDocument.SaveDocument(SourceDocument.Text, file);
        
        this.RaisePropertyChanged(nameof(WindowTitle));
        this.RaisePropertyChanged(nameof(FileLoaded));
    }
        
    public void ExecuteSource()
    {
        if (SourceDocument.TextLength == 0 || string.IsNullOrWhiteSpace(SourceDocument.Text))
        {
            return;
        }

        try
        {
            ActiveDocument.SourceText = SourceDocument.Text;
            OutputDocument.Insert(OutputDocument.TextLength, $"\n----- RUN {DateTime.Now:G} -----\n\n");
            foreach (var outputToken in ActiveDocument.ExecuteScript())
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
