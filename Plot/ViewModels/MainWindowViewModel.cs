using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.Core;
using Plot.Core;
using Plot.Models;
using Plot.Views;
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

    private DocumentEditorViewModel _activeEditor;

    public MainWindowViewModel()
    {
        var editorSelected = this.WhenAnyValue(x => x.ActiveEditor)
            .Select(x => x != null)
            .ObserveOn(RxApp.MainThreadScheduler);
        
        ClearOutput = ReactiveCommand.Create(ClearOutputImpl, editorSelected);
        SaveOutput = ReactiveCommand.CreateFromTask(SaveOutputImpl, editorSelected);
        CopyOutput = ReactiveCommand.CreateFromTask(CopyOutputImpl, editorSelected);
            
        OpenScript = ReactiveCommand.CreateFromTask(OpenScriptImpl);
        SaveScript = ReactiveCommand.CreateFromTask(SaveScriptImpl, editorSelected);
        SaveScriptAs = ReactiveCommand.CreateFromTask(SaveScriptAsImpl, editorSelected);
        
        ExecuteActiveScript = ReactiveCommand.Create(() => ActiveEditor?.ExecuteScript(), editorSelected);

        NewEditor = ReactiveCommand.Create(() => AddEditor(new DocumentEditorViewModel(), false));

        CloseActiveEditor = ReactiveCommand.Create(() => CloseEditorImpl(ActiveEditor), editorSelected);
        
        CopyToClipboardInteraction = new Interaction<string, Unit>();
        SaveFileDialogInteraction = new Interaction<FilePickerSaveOptions, IStorageFile>();
        OpenFileDialogInteraction = new Interaction<FilePickerOpenOptions, IReadOnlyCollection<IStorageFile>>();

        this.WhenAnyValue(x => x.ActiveEditor)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(editor =>
            {
                this.RaisePropertyChanged(nameof(FileLoaded));
                this.RaisePropertyChanged(nameof(WindowTitle));

                // push update to graphing functions
                MessageBus.Current.SendMessage(new PlotFunctionsChangedEvent(true, editor?.GraphingFunctions ?? []));
            });

        OpenEditors.ToObservableChangeSet()
            .Delay(TimeSpan.FromMilliseconds(10))
            .Throttle(TimeSpan.FromMilliseconds(25))
            .Subscribe(c =>
            {
                if (c.Adds > 0)
                {
                    ActiveEditor = c.First(x => x.Reason is ListChangeReason.Add or ListChangeReason.AddRange).Item.Current;
                }

                foreach (var removal in c.Where(x => x.Reason is ListChangeReason.Remove or ListChangeReason.RemoveRange))
                {
                    removal.Item.Current.Dispose();
                }
            });
        
        OpenEditors.Add(new DocumentEditorViewModel());
    }

    // these aren't reactive because they need manual handling (i.e. PropertyChanged)
    public string WindowTitle => ActiveEditor == null ? $"{App.Current.Name} {App.Version}" : $"{App.Current.Name} - {ActiveEditor.Document.FileName}";
    public bool FileLoaded => ActiveEditor?.Document.IsBackedByFile == true;

    public DocumentEditorViewModel ActiveEditor
    {
        get => _activeEditor;
        set => this.RaiseAndSetIfChanged(ref _activeEditor, value);
    }

    public ObservableCollection<DocumentEditorViewModel> OpenEditors { get; } = [];

    public Interaction<string, Unit> CopyToClipboardInteraction { get; }
    public Interaction<FilePickerSaveOptions, IStorageFile> SaveFileDialogInteraction { get; }
    public Interaction<FilePickerOpenOptions, IReadOnlyCollection<IStorageFile>> OpenFileDialogInteraction { get; }
        
    public ICommand CopyOutput { get; }
    public ICommand SaveOutput { get; }
    public ICommand ClearOutput { get; }
        
    public ICommand OpenScript { get; }
    public ICommand SaveScript { get; }
    public ICommand SaveScriptAs { get; }
    
    public ICommand ExecuteActiveScript { get; }

    public ICommand NewEditor { get; }
    public ICommand CloseActiveEditor { get; }

    private void ClearOutputImpl()
    {
        ActiveEditor.OutputDocument.Text = string.Empty;
    }
        
    private async Task CopyOutputImpl()
    {
        await CopyToClipboardInteraction.Handle(ActiveEditor.OutputDocument.Text).ToTask();
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

        await fileWriter.WriteAsync(ActiveEditor.OutputDocument.Text);
    }

    private async Task OpenScriptImpl()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Open file",
            AllowMultiple = true,
            FileTypeFilter = [PlotScriptType, new FilePickerFileType("All files")
            {
                AppleUniformTypeIdentifiers = ["public.item"],
                MimeTypes = ["*/*"],
                Patterns = ["*"]
            }]
        };
            
        var selectedFiles = await OpenFileDialogInteraction.Handle(options).ToTask();
        if (selectedFiles.Count == 0)
        {
            return;
        }

        if (OpenEditors.Count == 1 && !OpenEditors.Single().IsModified && !OpenEditors.Single().Document.IsBackedByFile)
        {
            OpenEditors.Clear();
        }
        
        foreach (var file in selectedFiles)
        {
            var document = await PlotScriptDocument.LoadFileAsync(file);
            AddEditor(new DocumentEditorViewModel(document));
        }
    }

    private async Task SaveScriptImpl()
    {
        if (ActiveEditor.Document.IsBackedByFile)
        {
            await ActiveEditor.SaveDocument();
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

        await ActiveEditor.SaveDocument(file);

        this.RaisePropertyChanged(nameof(WindowTitle));
        this.RaisePropertyChanged(nameof(FileLoaded));
    }

    internal void AddEditor(DocumentEditorViewModel editor, bool allowClosingEmpty = true)
    {
        if (OpenEditors.Contains(editor))
        {
            throw new InvalidOperationException("Duplicates not allowed");
        }

        OpenEditors.Add(editor);

        if (allowClosingEmpty && ActiveEditor?.IsModified != true && ActiveEditor?.Document.IsBackedByFile != true)
        {
            var target = ActiveEditor;
            Task.Delay(350).ContinueWith(_ => OpenEditors.Remove(target));
        }
    }

    private async Task CloseEditorImpl(DocumentEditorViewModel e)
    {
        if (ActiveEditor == e)
        {
            ActiveEditor = OpenEditors.LastOrDefault(x => x != e);
            await Task.Delay(325);
        }
        
        OpenEditors.Remove(e);
    }
}
