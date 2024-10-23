module Plot.Core.Functions.Consts

open System
open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("pi")>]
let pi (tokens: TokenType list): SymbolType =
    if tokens.Length <> 0 then
        raise (invalidArg "pi" "pi does not take any arguments")

    Float(Math.PI)

[<PlotScriptFunction("euler")>]
let euler (tokens: TokenType list): SymbolType =
    if tokens.Length <> 0 then
        raise (invalidArg "euler" "euler does not take any arguments")

    Float(Math.E)
    
[<PlotScriptFunction("tau")>]
let tau (tokens: TokenType list): SymbolType =
    if tokens.Length <> 0 then
        raise (invalidArg "tau" "tau does not take any arguments")

    Float(Math.Tau)