using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Microsoft.FSharp.Collections;
using OxyPlot;
using OxyPlot.Axes;
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
    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<LineSeries>> _series;

    private (double lower, double upper)? _bounds;

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

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Solid,
            AxislineStyle = LineStyle.Solid
        };

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = -10,
            Maximum = 10,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Solid,
            AxislineStyle = LineStyle.Solid
        };

        GraphModel = new PlotModel
        {
            PlotType = PlotType.Cartesian,
            Axes =
            {
                xAxis, yAxis
            }
        };

        Observable.FromEventPattern<AxisChangedEventArgs>(h => xAxis.AxisChanged += h, h => xAxis.AxisChanged -= h)
            .Buffer(TimeSpan.FromMilliseconds(25))
            .Where(x => x.Count > 0)
            .Select(x =>
            {
                var deltaMinSum = x.Sum(e => e.EventArgs.DeltaMinimum);
                var deltaMaxSum = x.Sum(e => e.EventArgs.DeltaMaximum);
                return ((Bounds?.lower ?? -10) + deltaMinSum, (Bounds?.upper ?? 10) + deltaMaxSum);
            })
            .Subscribe(b => { Bounds = b; });

        this.WhenAnyValue(x => x.GraphFunctions, x => x.Bounds)
            .Where(x => x.Item1 != null)
            .Select(x =>
            {
                var series = x.Item1.Select(f =>
                {
                    IEnumerable<double> range;

                    if (x.Item2.HasValue)
                    {
                        var start = x.Item2.Value.lower;
                        var end = x.Item2.Value.upper;

                        range = Utils.generateRange(start, end, 1);
                    }
                    else
                    {
                        range = f.Item.DefaultRange?.Value ?? Utils.generateRange(-10, 10, 0.1);
                    }

                    var points = range.Select(p => ConvertToDataPoint(p, f.Item.Function.Invoke(PlotFunctionInvoke(p))));
                    var series = new LineSeries();

                    series.Points.AddRange(points);
                    return series;
                });

                return series.ToList();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.Series, out _series);

        this.WhenAnyValue(x => x.Series)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                GraphModel.Series.Clear();

                foreach (var s in x ?? [])
                {
                    GraphModel.Series.Add(s);
                }

                GraphModel.InvalidatePlot(true);
            });


        CloseWindow = ReactiveCommand.CreateFromTask(async () => await CloseWindowInteraction.Handle(Unit.Default));
    }

    /// <summary>
    /// The currently displayed <see cref="PlotModel"/>
    /// </summary>
    public PlotModel GraphModel { get; }

    public IReadOnlyCollection<LineSeries> Series => _series.Value;

    private (double lower, double upper)? Bounds
    {
        get => _bounds;
        set => this.RaiseAndSetIfChanged(ref _bounds, value);
    }

    /// <summary>
    /// The currently available graphing functions.
    /// </summary>
    private IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphFunctions => _graphFunctions.Value;

    public ICommand CloseWindow { get; }

    public Interaction<Unit, Unit> CloseWindowInteraction { get; } = new();

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