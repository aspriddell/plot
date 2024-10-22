module Plot.Core.Functions.Plotting

open System.Collections.Generic
open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("plot", InjectSymbolTable = true)>]
let public createPlottableFunction(input: SymbolType list, symTable: IDictionary<string, SymbolType>): SymbolType =
    match input with
    | [SymbolType.PlotScriptFunction (_, fnTokens)] ->
        let callback = fun (inputs: SymbolType list) ->
            let symbolTableClone = Dictionary<string, SymbolType>(symTable)
            Seq.zip TokenUtils.asciiVariableSequence inputs |> Seq.iter (fun (k, v) -> symbolTableClone[k] <- v)
            Parser.ParseAndEval(fnTokens, symbolTableClone, PlotScriptFunctionContainer.Default) |> Seq.exactlyOne

        SymbolType.PlotScriptGraphingFunction callback
    | _ -> invalidArg "function" "plot accepts a single function as the only parameter"