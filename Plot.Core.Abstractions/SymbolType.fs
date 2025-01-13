module Plot.Core.Extensibility.Symbols

open System
open Plot.Core.Extensibility.Tokens

type SymbolType =
    | Unit
    | Int of int
    | Float of double
    | List of SymbolType list
    | ObjectReference of Object
    | PlotScriptFunction of FunctionInfo
    | PlotScriptGraphingFunction of PlottingFunctionInfo
    | PlotScriptPolynomialFunction of PolynomialFunctionInfo

and FunctionInfo = {
    Function: SymbolType list -> SymbolType
    Tokens: TokenType list option
}

and PolynomialFunctionInfo = {
    Function: SymbolType list -> SymbolType
    Coefficients: float list
    RealRootRange: float * float
}

and PlottingFunctionInfo = {
    Function: SymbolType list -> SymbolType
    DefaultRange: float seq option
}

let isAssignableSymbolType(symbol: SymbolType): bool =
    match symbol with
    | Unit
    | PlotScriptGraphingFunction _ -> false
    | _ -> true

