using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Windowing;
using Plot.ViewModels;
using ReactiveUI;

namespace Plot.Views;

public partial class MainWindow : ReactiveAppWindow<MainWindowViewModel>
{
    private GraphWindow _graphWindow;

    public MainWindow()
    {
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        TransparencyLevelHint = App.TransparencyLevels;

        this.WhenActivated(disposables =>
        {
            ViewModel!.CopyToClipboardInteraction.RegisterHandler(CopyToClipboard).DisposeWith(disposables);
            ViewModel!.OpenFileDialogInteraction.RegisterHandler(HandleFileOpenPicker).DisposeWith(disposables);
            ViewModel!.SaveFileDialogInteraction.RegisterHandler(HandleFileSavePicker).DisposeWith(disposables);
            
            // subscribe to graphing function change events
            MessageBus.Current.Listen<PlotFunctionsChangedEvent>()
                .Where(x => x?.Functions?.Count > 0) // ignore all null/empty function lists
                .ObserveOn(RxApp.MainThreadScheduler) // run on ui thread (using UI calls)
                .Subscribe(e => EnsureGraphWindow(!e.TabChanged))
                .DisposeWith(disposables);
        });
    }

    private async Task CopyToClipboard(InteractionContext<string, Unit> ctx)
    {
        await (Clipboard?.SetTextAsync(ctx.Input) ?? Task.CompletedTask);
        ctx.SetOutput(Unit.Default);
    }

    private async Task HandleFileOpenPicker(InteractionContext<FilePickerOpenOptions, IReadOnlyCollection<IStorageFile>> ctx)
    {
        var root = await GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(ctx.Input);
        ctx.SetOutput(root);
    }

    private async Task HandleFileSavePicker(InteractionContext<FilePickerSaveOptions, IStorageFile> ctx)
    {
        var root = await GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(ctx.Input);
        ctx.SetOutput(root);
    }

    private void EnsureGraphWindow(bool activateWindow = true)
    {
        if (_graphWindow?.PlatformImpl == null)
        {
            _graphWindow = null;
        }
        
        // if the graphwindow is closed, dispose the viewmodel and reconstruct
        _graphWindow ??= new GraphWindow
        {
            DataContext = new GraphWindowViewModel()
        };

        _graphWindow.Closed += (sender, _) =>
        {
            (((GraphWindow)sender)!.DataContext as IDisposable)?.Dispose();
        };

        if (!_graphWindow.IsVisible)
        {
            _graphWindow.Show(this);
        }

        if (activateWindow)
        {
            _graphWindow.Activate();
        }
    }
}