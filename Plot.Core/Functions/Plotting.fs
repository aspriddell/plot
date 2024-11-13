module Plot.Core.Functions.Plotting

open System.Collections.Generic
open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("plot", InjectSymbolTable = true)>]
let public createPlottableFunction (input: SymbolType list, symTable: IDictionary<string, SymbolType>) : SymbolType =
    match input with
    | [PlotScriptFunction info] when info.Tokens.IsSome ->
        let callback = fun (inputs: SymbolType list) ->
            let symbolTableClone = Dictionary<string, SymbolType>(symTable)
            Seq.zip TokenUtils.asciiVariableSequence inputs |> Seq.iter (fun (k, v) -> symbolTableClone[k] <- v)
            Parser.ParseAndEval(info.Tokens.Value, symbolTableClone, PlotScriptFunctionContainer.Default) |> Seq.exactlyOne

        // we can't work out the range of the function so use the default
        PlotScriptGraphingFunction { Function = callback; DefaultRange = None }

    // fallback to using the function directly if no tokens are provided
    // (i.e. a polynomial function has no state and has no tokens to create from)
    | [PlotScriptFunction info] -> PlotScriptGraphingFunction { Function = info.Function; DefaultRange = None }
    | [PlotScriptPolynomialFunction info] ->
        // ensure the range is at least 50% larger on either side, and that there's at least 500 points displayed
        let step = (snd info.RealRootRange - fst info.RealRootRange) / 500.0
        let points = [(fst info.RealRootRange * 1.5) .. step .. (snd info.RealRootRange * 1.5)]

        PlotScriptGraphingFunction { Function = info.Function; DefaultRange = Some(points |> Seq.ofList) }
            
    | _ -> invalidArg "function" "plot accepts a single function as the only parameter"