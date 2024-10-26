module Plot.Core.Functions.Base

open System.Diagnostics
open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("pow")>]
let public pow (x: SymbolType list) : SymbolType =
    match x with
    | [ Int i1; Int i2 ] -> Int(int (float i1 ** i2))
    | [ Int i; Float f ] -> Float(float i ** f)
    | [ Float f; Int i ] -> Float(f ** float i)
    | [ Float f1; Float f2 ] -> Float(f1 ** f2)

    | [ _; _ ] -> invalidOp "pow not defined for the given types"
    | _ -> invalidArg "*" "pow expects two arguments"

[<PlotScriptFunction("roll")>]
let public rickRoll(_x: SymbolType list): SymbolType =
    let processStartInfo = ProcessStartInfo("https://www.youtube.com/watch?v=dQw4w9WgXcQ")

    processStartInfo.UseShellExecute <- true
    processStartInfo.Verb <- "open"

    Process.Start(processStartInfo) |> ignore
    Unit
