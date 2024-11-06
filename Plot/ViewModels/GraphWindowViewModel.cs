using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.FSharp.Collections;
using OxyPlot;
using OxyPlot.Series;
using Plot.Core;
using ReactiveUI;

namespace Plot.ViewModels;

public record PlotFunctionsChangedEvent(
    bool TabChanged,
    IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> Functions);

public class GraphWindowViewModel : ReactiveObject, IDisposable
{
    private PlotModel _graphModel = new();
    private readonly ObservableAsPropertyHelper<List<LineSeries>> _points;

    public PlotModel GraphModel
    {
        get => _graphModel;
        set => this.RaiseAndSetIfChanged(ref _graphModel, value);
    }

    private List<LineSeries> Points => _points.Value;

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

        GraphModel = new PlotModel { Title = "Graph" };

        this.WhenAnyValue(x => x.GraphFunctions)
            .Select(functions =>
            {
                List<LineSeries> series = new();
                foreach (var func in functions)
                {
                    LineSeries temp = new()
                    {
                        Title = func.ToString()
                    };
                    temp.Points.AddRange(Enumerable.Range(1, 1000)
                        .AsParallel()
                        .Select(i => ConvertToDataPoint(i, func.Item.Invoke(PlotFunctionInvoke(i))))
                        .ToList());
                    series.Add(temp);
                }

                return series;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.Points, out _points);
    }

    private FSharpList<Symbols.SymbolType> PlotFunctionInvoke(int i)
    {
        return FSharpList<Symbols.SymbolType>.Cons(Symbols.SymbolType.NewFloat(i), FSharpList<Symbols.SymbolType>.Empty);
    }

    private DataPoint ConvertToDataPoint(int x, Symbols.SymbolType symbol)
    {
        return symbol switch
        {
            Symbols.SymbolType.Int i => new DataPoint(x, i.Item),
            Symbols.SymbolType.Float f => new DataPoint(x, f.Item),
            _ => throw new InvalidOperationException("Unsupported SymbolType")
        };
    }

    /// <summary>
    /// The currently available graphing functions.
    /// </summary>
    public IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphFunctions => _graphFunctions.Value;

    public void Dispose()
    {
        _disposable?.Dispose();
    }
}