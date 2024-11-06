using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Plot.Core;
using ReactiveUI;

namespace Plot.ViewModels;

public record PlotFunctionsChangedEvent(
    bool TabChanged,
    IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> Functions);

public class GraphWindowViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposable = new();

    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction>> _graphFunctions;
    
    public GraphWindowViewModel()
    {
        MessageBus.Current.ListenIncludeLatest<PlotFunctionsChangedEvent>()
            .Where(x => x != null)
            .Select(x => x.Functions)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.GraphFunctions, out _graphFunctions)
            .DisposeWith(_disposable);

        CloseWindow = ReactiveCommand.CreateFromTask(async () => await CloseWindowInteraction.Handle(Unit.Default));
    }

    /// <summary>
    /// The currently available graphing functions.
    /// </summary>
    public IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphFunctions => _graphFunctions.Value;

    public ICommand CloseWindow { get; }

    public Interaction<Unit, Unit> CloseWindowInteraction { get; } = new();
    
    public void Dispose()
    {
        _disposable?.Dispose();
    }
}