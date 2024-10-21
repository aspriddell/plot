using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Plot.Models;
using Plot.ViewModels;

namespace Plot;

public partial class App : Application
{
    /// <summary>
    /// Transparency level hints passed to windows to enable transparency effects (if supported).
    /// </summary>
    public static readonly IReadOnlyList<WindowTransparencyLevel> TransparencyLevels;

    public static string Version { get; }
    
    static App()
    {
        // enable mica effect on Windows 11 and above
        TransparencyLevels = OperatingSystem.IsWindowsVersionAtLeast(10, 22000)
            ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur]
            : [WindowTransparencyLevel.AcrylicBlur];

#if DEBUG
        Version = "Dev Edition";
#else
        Version = $"v{typeof(App).Assembly.GetName().Version?.ToString(2)}" ?? "Unknown";
#endif
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            
            // macOS activates the window to open a file
            if (OperatingSystem.IsMacOS() && TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime lifetime)
            {
                lifetime.Activated += (_, e) =>
                {
                    switch (e)
                    {
                        case FileActivatedEventArgs fileArgs when fileArgs.Files.OfType<IStorageFile>().Any():
                            PlotScriptDocument.LoadFileAsync(fileArgs.Files.OfType<IStorageFile>().First())
                                .ContinueWith(t =>
                                {
                                    ((MainWindowViewModel)desktop.MainWindow.DataContext).ActiveDocument = t.Result;
                                    desktop.MainWindow.BringIntoView();
                                });
                            break;
                    }
                };
            }
            // whereas other systems just boot a new instance with the first arg as the file
            if (!OperatingSystem.IsMacOS() && desktop.Args?.Length > 0 && desktop.MainWindow.DataContext is MainWindowViewModel vm)
            {
                LoadFileAsync(desktop.MainWindow.StorageProvider, desktop.Args.First(), vm);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private static async void LoadFileAsync(IStorageProvider storageProvider, string filePath, MainWindowViewModel viewModel)
    {
        var file = await storageProvider.TryGetFileFromPathAsync(filePath);
        var document = await PlotScriptDocument.LoadFileAsync(file);

        if (file != null)
        {
            viewModel.ActiveDocument = document;
        }
    }
}