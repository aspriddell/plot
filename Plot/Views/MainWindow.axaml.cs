using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Windowing;
using Plot.ViewModels;
using ReactiveUI;

namespace Plot.Views;

public partial class MainWindow : ReactiveAppWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        TransparencyLevelHint = App.TransparencyLevels;
        
        this.WhenActivated(action => action(ViewModel!.CopyToClipboardInteraction.RegisterHandler(CopyToClipboard)));
        this.WhenActivated(action => action(ViewModel!.OpenFileDialogInteraction.RegisterHandler(HandleFileOpenPicker)));
        this.WhenActivated(action => action(ViewModel!.SaveFileDialogInteraction.RegisterHandler(HandleFileSavePicker)));
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