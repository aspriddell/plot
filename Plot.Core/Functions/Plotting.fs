module Plot.Core.Functions.Plotting

open System.Collections.Generic
open Plot.Core
open Plot.Core.Extensibility.Functions
open Plot.Core.Extensibility.Symbols
open Plot.Core.Functions.Calculus
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
        let roots = findRoots info.Coefficients 1.0 |> List.ofSeq

        if List.length roots = 0 then
            PlotScriptGraphingFunction { Function = info.Function; DefaultRange = None }
        else
            let min = (roots |> List.min) * 1.5
            let max = (roots |> List.max) * 1.5
            let step = (max - min) / 500.0
            let points = [min .. step .. max]

            PlotScriptGraphingFunction { Function = info.Function; DefaultRange = Some(points |> Seq.ofList) }

    | _ -> invalidArg "function" "plot accepts a single function as the only parameter"
