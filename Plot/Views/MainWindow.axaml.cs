using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using Plot.ViewModels;

namespace Plot.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        TransparencyLevelHint = App.TransparencyLevels;
    }

    protected override void OnLoaded(RoutedEventArgs _)
    {
        if (OperatingSystem.IsWindows() && DataContext is MainWindowViewModel _)
        {
            RegisterHotKeys(NativeMenu.GetMenu(this));
        }
    }

    // reigster hotkeys on platforms that don't "just work" out the box
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
}