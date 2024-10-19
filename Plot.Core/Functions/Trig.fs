module Plot.Core.Functions.Trig

open Plot.Core.Symbols

let public sin (x: SymbolType list) : SymbolType =
    match x |> List.head with
    | Int i -> Float(float i |> sin)
    | Float f -> Float(f |> sin)

    | _ -> invalidOp "Sin not defined for the given type"
    
let public cos (x: SymbolType list) : SymbolType =
    match x |> List.head with
    | Int i -> Float(float i |> cos)
    | Float f -> Float(f |> cos)

    | _ -> invalidOp "Cos not defined for the given type"
    
let public tan (x: SymbolType list) : SymbolType =
    match x |> List.head with
    | Int i -> Float(float i |> tan)
    | Float f -> Float(f |> tan)

    | _ -> invalidOp "Tan not defined for the given type"