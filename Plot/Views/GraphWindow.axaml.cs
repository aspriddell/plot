using System.Reactive;
using FluentAvalonia.UI.Windowing;
using Plot.ViewModels;
using ReactiveUI;

namespace Plot.Views;

public partial class GraphWindow : ReactiveAppWindow<GraphWindowViewModel>
{
    public GraphWindow()
    {
        InitializeComponent();
        
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        TransparencyLevelHint = App.TransparencyLevels;

        this.WhenActivated(d => d(ViewModel!.CloseWindowInteraction.RegisterHandler(HandleWindowCloseInteraction)));
    }

    private void HandleWindowCloseInteraction(InteractionContext<Unit, Unit> obj)
    {
        Close();
        obj.SetOutput(Unit.Default);
    }
}