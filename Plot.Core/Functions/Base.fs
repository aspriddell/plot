module Plot.Core.Functions.Base

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
