using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.FSharp.Collections;
using OxyPlot;
using OxyPlot.Series;
using Plot.Core;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace Plot.ViewModels;

public record PlotFunctionsChangedEvent(
    bool TabChanged,
    IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> Functions);

public class GraphWindowViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposable = new();
    private readonly ObservableAsPropertyHelper<PlotModel> _graphModel;

    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction>>
        _graphFunctions;

    public GraphWindowViewModel()
    {
        MessageBus.Current.ListenIncludeLatest<PlotFunctionsChangedEvent>()
            .Where(x => x != null)
            .Select(x => x.Functions)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.GraphFunctions, out _graphFunctions)
            .DisposeWith(_disposable);

        this.WhenAnyValue(x => x.GraphFunctions)
            .Where(x => x != null)
            .Select(BuildPlotModel)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.GraphModel, out _graphModel);

        CloseWindow = ReactiveCommand.CreateFromTask(async () => await CloseWindowInteraction.Handle(Unit.Default));
    }

    /// <summary>
    /// The currently displayed <see cref="PlotModel"/>
    /// </summary>
    public PlotModel GraphModel => _graphModel.Value;

    /// <summary>
    /// The currently available graphing functions.
    /// </summary>
    public IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphFunctions => _graphFunctions.Value;

    public ICommand CloseWindow { get; }

    public Interaction<Unit, Unit> CloseWindowInteraction { get; } = new();

    private static PlotModel BuildPlotModel(IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> fn)
    {
        var series = fn.Select(f =>
        {
            var range = f.Item.DefaultRange == null
                ? Enumerable.Range(-100, 200).Select(x => (double)x)
                : f.Item.DefaultRange.Value;

            var points = range.Select(x => ConvertToDataPoint(x, f.Item.Function.Invoke(PlotFunctionInvoke(x))));
            var series = new LineSeries();

            series.Points.AddRange(points);
            return series;
        });

        var plot = new PlotModel
        {
            PlotType = PlotType.Cartesian
        };

        foreach (var s in series)
        {
            plot.Series.Add(s);
        }

        return plot;
    }

    private static FSharpList<Symbols.SymbolType> PlotFunctionInvoke(double i)
    {
        return FSharpList<Symbols.SymbolType>.Cons(Symbols.SymbolType.NewFloat(i),
            FSharpList<Symbols.SymbolType>.Empty);
    }

    private static DataPoint ConvertToDataPoint(double x, Symbols.SymbolType symbol) => symbol switch
    {
        Symbols.SymbolType.Int i => new DataPoint(x, i.Item),
        Symbols.SymbolType.Float f => new DataPoint(x, f.Item),
        _ => throw new InvalidOperationException("Unsupported SymbolType")
    };

    public void Dispose()
    {
        _disposable?.Dispose();
    }
}