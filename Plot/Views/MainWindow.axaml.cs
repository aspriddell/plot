using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
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
    public MainWindow(string initialFilePath = null)
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

            if (!string.IsNullOrEmpty(initialFilePath) && File.Exists(initialFilePath))
            {
                StorageProvider
                    .TryGetFileFromPathAsync(new Uri(initialFilePath))
                    .ContinueWith(t => Dispatcher.UIThread.InvokeAsync(() => ViewModel!.LoadFileInternal(t.Result)), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
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
}