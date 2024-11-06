using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using Plot.ViewModels;
using ReactiveUI;

namespace Plot.Views;

public partial class GraphWindow : ReactiveAppWindow<GraphWindowViewModel>
{
    public GraphWindow()
    {
        InitializeComponent();

        this.WhenActivated(d => d(ViewModel!.CloseWindowInteraction.RegisterHandler(HandleWindowCloseInteraction)));
    }

    private void HandleWindowCloseInteraction(InteractionContext<Unit, Unit> obj)
    {
        Close();

        obj.SetOutput(Unit.Default);
    }
}