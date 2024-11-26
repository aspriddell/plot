using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using OxyPlot;
using Plot.Core;

namespace Plot.Models;

public class PlotScriptGraphFunctionContainer(Symbols.SymbolType.PlotScriptGraphingFunction function)
{
    private readonly ConcurrentDictionary<double, DataPoint> _cache = new();

    public Symbols.SymbolType.PlotScriptGraphingFunction Function => function;

    public IEnumerable<DataPoint> GetPoints(IEnumerable<double> xPoints)
    {
        foreach (var x in xPoints)
        {
            if (!_cache.TryGetValue(x, out var dataPoint))
            {
                var list = FSharpList<Symbols.SymbolType>.Cons(Symbols.SymbolType.NewFloat(x), FSharpList<Symbols.SymbolType>.Empty);

                dataPoint = function.Item.Function.Invoke(list) switch
                {
                    Symbols.SymbolType.Float f => new DataPoint(x, f.Item),
                    Symbols.SymbolType.Int i => new DataPoint(x, i.Item),

                    _ => throw new ArgumentOutOfRangeException()
                };

                _cache[x] = dataPoint;
            }

            yield return dataPoint;
        }
    }

    public void ClearCachedPoints()
    {
        _cache.Clear();
    }
}