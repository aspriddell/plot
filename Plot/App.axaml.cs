using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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
            
            if (TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime lifetime)
            {
                lifetime.Activated += (sender, e) =>
                {
                    switch (e)
                    {
                        case FileActivatedEventArgs fileArgs when fileArgs.Files.OfType<IStorageFile>().Any() && desktop.MainWindow.DataContext is MainWindowViewModel vm:
                            desktop.MainWindow.BringIntoView();
                            
                            _ = vm.LoadFileInternal(fileArgs.Files.OfType<IStorageFile>().First());
                            break;
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}