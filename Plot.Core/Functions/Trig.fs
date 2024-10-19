module Plot.Core.Functions.Trig

open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("sin")>]
let public sin (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i |> sin)
    | Float f -> Float(f |> sin)

    | _ -> invalidOp "Sin not defined for the given type"

[<PlotScriptFunction("cos")>]
let public cos (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i |> cos)
    | Float f -> Float(f |> cos)

    | _ -> invalidOp "Cos not defined for the given type"

[<PlotScriptFunction("tan")>]
let public tan (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i |> tan)
    | Float f -> Float(f |> tan)

    | _ -> invalidOp "Tan not defined for the given type"
    
[<PlotScriptFunction("asin")>]
let public asin (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i |> asin)
    | Float f -> Float(f |> asin)

    | _ -> invalidOp "Asin not defined for the given type"

[<PlotScriptFunction("acos")>]
let public acos (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i |> acos)
    | Float f -> Float(f |> acos)

    | _ -> invalidOp "Acos not defined for the given type"

[<PlotScriptFunction("atan")>]
let public atan (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i |> atan)
    | Float f -> Float(f |> atan)

    | _ -> invalidOp "Atan not defined for the given type"

[<PlotScriptFunction("atan2")>]
let public atan2 (x: SymbolType list) : SymbolType =
    match x with
    | [Int i1; Int i2] -> Float(float i1 |> atan2 (float i2))
    | [Int i; Float f] -> Float(float i |> atan2 f)
    | [Float f1; Float f2] -> Float(f1 |> atan2 f2)

    | [_; _] -> invalidOp "Atan2 not defined for the given types"
    | _ -> invalidArg "*" "Atan2 expects two arguments"
