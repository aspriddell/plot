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
using ReactiveUI;

namespace Plot.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private static readonly FilePickerFileType PlotScriptType = new("PlotScript")
        {
            AppleUniformTypeIdentifiers = ["moe.ppc.plotscript"],
            Patterns = ["*.plotscript"],
            MimeTypes = ["text/plain"]
        };

        private readonly ObservableAsPropertyHelper<string> _windowTitle;
        private readonly ObservableAsPropertyHelper<bool> _fileLoaded;
        
        private IStorageFile _openedFile;

        public MainWindowViewModel()
        {
            SymbolTable = new AvaloniaDictionary<string, Symbols.SymbolType>();

            ClearOutput = ReactiveCommand.Create(ClearOutputImpl);
            SaveOutput = ReactiveCommand.CreateFromTask(SaveOutputImpl);
            CopyOutput = ReactiveCommand.CreateFromTask(CopyOutputImpl);
            
            OpenScript = ReactiveCommand.CreateFromTask(OpenScriptImpl);
            SaveScript = ReactiveCommand.CreateFromTask(SaveScriptImpl);
            SaveScriptAs = ReactiveCommand.CreateFromTask(SaveScriptAsImpl);

            CopyToClipboardInteraction = new Interaction<string, Unit>();
            SaveFileDialogInteraction = new Interaction<FilePickerSaveOptions, IStorageFile>();
            OpenFileDialogInteraction = new Interaction<FilePickerOpenOptions, IReadOnlyCollection<IStorageFile>>();

            // window title binding
            this.WhenAnyValue(x => x.OpenedFile)
                .Select(x => $"{Application.Current!.Name} - {x?.Name ?? "Untitled.plotscript"}")
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.WindowTitle, out _windowTitle);

            this.WhenAnyValue(x => x.OpenedFile)
                .Select(x => x != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FileLoaded, out _fileLoaded);
        }

        /// <summary>
        /// The currently opened file
        /// </summary>
        public IStorageFile OpenedFile
        {
            get => _openedFile;
            private set => this.RaiseAndSetIfChanged(ref _openedFile, value);
        }

        public string WindowTitle => _windowTitle.Value;
        public bool FileLoaded => _fileLoaded.Value;

        public TextDocument SourceDocument { get; } = new();
        public TextDocument OutputDocument { get; } = new();

        public IDictionary<string, Symbols.SymbolType> SymbolTable { get; }

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
            SymbolTable.Clear();
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
                AllowMultiple = false,
                Title = "Open PlotScript file",
                FileTypeFilter = [PlotScriptType]
            };
            
            var selectedFiles = await OpenFileDialogInteraction.Handle(options).ToTask();
            if (selectedFiles.Count != 1)
            {
                return;
            }
            
            var file = selectedFiles.Single();
            
            await using (var readStream = await file.OpenReadAsync())
            using (var fileReader = new StreamReader(readStream, Encoding.UTF8))
            {
                SourceDocument.Text = await fileReader.ReadToEndAsync();
            }

            OpenedFile = file;
        }

        private async Task SaveScriptImpl()
        {
            if (OpenedFile == null)
            {
                await SaveScriptAsImpl();
                return;
            }
            
            await using var writeStream = await OpenedFile.OpenWriteAsync();
            await using var fileWriter = new StreamWriter(writeStream, Encoding.UTF8);
            
            await fileWriter.WriteAsync(SourceDocument.Text);
        }

        private async Task SaveScriptAsImpl()
        {
            var saveDialog = new FilePickerSaveOptions
            {
                DefaultExtension = ".plotscript",
                SuggestedFileName = "untitled.plotscript",
                FileTypeChoices = [PlotScriptType]
            };
            
            var file = await SaveFileDialogInteraction.Handle(saveDialog).ToTask();
            if (file == null)
            {
                return;
            }
            
            OpenedFile = file;
            await SaveScriptImpl();
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
