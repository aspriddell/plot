#nullable enable

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using FluentAvalonia.UI.Windowing;
using ReactiveUI;

namespace Plot;

/// <summary>
/// A ReactiveUI <see cref="Window"/> that implements the <see cref="IViewFor"/> interface and will
/// activate your ViewModel automatically if the view model implements <see cref="IActivatableViewModel"/>. When
/// the DataContext property changes, this class will update the ViewModel property with the new DataContext value,
/// and vice versa.
/// </summary>
/// <typeparam name="TViewModel">ViewModel type.</typeparam>
/// <remarks>
/// This is a version of the ReactiveUI <see cref="ReactiveWindow{TViewModel}"/> class modified to support <see cref="AppWindow"/> and fix keyboard shortcut binding.
/// See https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.ReactiveUI/ReactiveWindow.cs for the original implementation.
/// </remarks>
public class ReactiveAppWindow<TViewModel> : AppWindow, IViewFor<TViewModel> where TViewModel : class
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1002", Justification = "Generic avalonia property is expected here.")]
    public static readonly StyledProperty<TViewModel?> ViewModelProperty = AvaloniaProperty.Register<ReactiveWindow<TViewModel>, TViewModel?>(nameof(ViewModel));

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveWindow{TViewModel}"/> class.
    /// </summary>
    protected ReactiveAppWindow()
    {
        // This WhenActivated block calls ViewModel's WhenActivated
        // block if the ViewModel implements IActivatableViewModel.
        this.WhenActivated(_ =>
        {
            Focus();
        });
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            RegisterHotKeys(NativeMenu.GetMenu(this));
        }

        // needed to allow WhenActivated calls to fire
        base.OnLoaded(e);
    }
    
    // register hotkeys on platforms that don't "just work" out the box
    // loosely based on https://github.com/AvaloniaUI/Avalonia/issues/2441#issuecomment-2151522663
    private void RegisterHotKeys(NativeMenu? control)
    {
        if (control == null)
        {
            return;
        }
        
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

    /// <summary>
    /// The ViewModel.
    /// </summary>
    public TViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel?)value;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataContextProperty)
        {
            if (ReferenceEquals(change.OldValue, ViewModel)
                && change.NewValue is null or TViewModel)
            {
                SetCurrentValue(ViewModelProperty, change.NewValue);
            }
        }
        else if (change.Property == ViewModelProperty)
        {
            if (ReferenceEquals(change.OldValue, DataContext))
            {
                SetCurrentValue(DataContextProperty, change.NewValue);
            }
        }
    }
}