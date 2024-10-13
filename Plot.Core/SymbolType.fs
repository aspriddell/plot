module Plot.Core.Symbols

open System

/// <summary>
/// Discriminated union representing the type of symbol.
/// </summary>
type SymbolType =
    | Int of int
    | Float of float

let internal addValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int (i1 + i2)
    | Float f1, Float f2 -> Float (f1 + f2)
    | Int i, Float f | Float f, Int i -> Float (float i + f)

let internal subValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int (i1 - i2)
    | Float f1, Float f2 -> Float (f1 - f2)
    | Int i, Float f | Float f, Int i -> Float (float i - f)

let internal mulValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int (i1 * i2)
    | Float f1, Float f2 -> Float (f1 * f2)
    | Int i, Float f | Float f, Int i -> Float (float i * f)

let internal divValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 when i2 <> 0 -> Int (i1 / i2)
    | Float f1, Float f2 when f2 <> 0.0 -> Float (f1 / f2)
    | Int i, Float f when f <> 0.0 -> Float (float i / f)
    | Float f, Int i when i <> 0 -> Float (f / float i)

    // handle div/0
    | _ -> raise (DivideByZeroException())

let internal modValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 when i2 <> 0 -> Int (i1 % i2)
    | Int _, Int _ -> raise (DivideByZeroException())
    | _ -> invalidOp "Modulus operator is only defined for integers"

let internal powValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int (int (float i1 ** float i2))
    | Float f1, Float f2 -> Float (f1 ** f2)
    | Int i, Float f | Float f, Int i -> Float (float i ** f)