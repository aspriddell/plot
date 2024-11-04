using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows() && DataContext is MainWindowViewModel _)
        {
            RegisterHotKeys(NativeMenu.GetMenu(this));
        }

        // needed to allow WhenActivated calls to fire
        base.OnLoaded(e);
    }

    // register hotkeys on platforms that don't "just work" out the box
    // loosely based on https://github.com/AvaloniaUI/Avalonia/issues/2441#issuecomment-2151522663
    private void RegisterHotKeys(NativeMenu control)
    {
        foreach (var item in control.Items.OfType<NativeMenuItem>())
        {
            if (item.Command != null && item.Gesture != null)
            {
                KeyBindings.Add(new KeyBinding
                {
                    Gesture = item.Gesture,
                    Command = item.Command
                });
            }

            foreach (var childItem in item.Menu?.OfType<NativeMenuItem>() ?? [])
            {
                RegisterHotKeys(childItem.Parent);
            }
        }
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
            _graphWindow.Show();
        }

        if (activateWindow)
        {
            _graphWindow.Activate();
        }
    }
}