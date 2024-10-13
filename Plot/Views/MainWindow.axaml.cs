using FluentAvalonia.UI.Windowing;

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
}