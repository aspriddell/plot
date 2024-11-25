using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    private readonly ObservableAsPropertyHelper<(double lower, double upper)?> _currentPlotBounds;
    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction>> _graphFunctions;

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
            AxislineStyle = LineStyle.Solid,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.LongDash
        };

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = -10,
            Maximum = 10,
            AxislineStyle = LineStyle.Solid,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.LongDash
        };

        GraphModel = new PlotModel
        {
            PlotType = PlotType.Cartesian,
            Axes = { xAxis, yAxis }
        };

#pragma warning disable CS0618 // Type or member is obsolete
        Observable.FromEventPattern<AxisChangedEventArgs>(h => xAxis.AxisChanged += h, h => xAxis.AxisChanged -= h)
#pragma warning restore CS0618 // Type or member is obsolete
            .Buffer(TimeSpan.FromMilliseconds(25))
            .Select(x => x.Aggregate(Vector2.Zero, (acc, e) => acc + new Vector2((float)e.EventArgs.DeltaMinimum, (float)e.EventArgs.DeltaMaximum)))
            .Where(acc => acc != Vector2.Zero)
            .Select(delta => (ValueTuple<double, double>?)((CurrentPlotBounds?.lower ?? -10) + delta.X, (CurrentPlotBounds?.upper ?? 10) + delta.Y))
            .ToProperty(this, x => x.CurrentPlotBounds, out _currentPlotBounds);

        this.WhenAnyValue(x => x.GraphFunctions, x => x.CurrentPlotBounds)
            .Where(x => x.Item1 != null)
            .Select(BuildPlotSeries)
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

    /// <summary>
    /// The upper and lower limits of the current plot (x axis).
    /// </summary>
    private (double lower, double upper)? CurrentPlotBounds => _currentPlotBounds.Value;

    /// <summary>
    /// The currently available graphing functions.
    /// </summary>
    private IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction> GraphFunctions => _graphFunctions.Value;

    public ICommand CloseWindow { get; }

    public Interaction<Unit, Unit> CloseWindowInteraction { get; } = new();
    
    private static List<LineSeries> BuildPlotSeries((IReadOnlyCollection<Symbols.SymbolType.PlotScriptGraphingFunction>, (double lower, double upper)?) x)
    {
        var series = x.Item1.Select(f =>
        {
            IEnumerable<double> range;

            if (x.Item2.HasValue)
            {
                var start = x.Item2.Value.lower;
                var end = x.Item2.Value.upper;

                range = Utils.generateRange(start, end, (end - start) / 500d);
            }
            else
            {
                range = f.Item.DefaultRange?.Value ?? Utils.generateRange(-10, 10, 0.1);
            }

            var series = new LineSeries();
            series.Points.AddRange(range.AsParallel().Select(p => ConvertToDataPoint(p, f.Item.Function.Invoke(PlotFunctionInvoke(p)))).OrderBy(x => x.X));

            return series;
        });

        return series.ToList();
    }
    
    private static FSharpList<Symbols.SymbolType> PlotFunctionInvoke(double i)
    {
        return FSharpList<Symbols.SymbolType>.Cons(Symbols.SymbolType.NewFloat(i), FSharpList<Symbols.SymbolType>.Empty);
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