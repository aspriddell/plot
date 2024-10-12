module internal Plot.Core.Symbols

/// <summary>
/// Discriminated union representing the type of symbol.
/// </summary>
type SymbolType =
    | Int of int
    | Float of float

let addValues v1 v2 =
    match v1, v2 with
    | Int i1, Int i2 -> Int (i1 + i2)
    | Float f1, Float f2 -> Float (f1 + f2)
    | Int i, Float f | Float f, Int i -> Float (float i + f)

let subValues v1 v2 =
    match v1, v2 with
    | Int i1, Int i2 -> Int (i1 - i2)
    | Float f1, Float f2 -> Float (f1 - f2)
    | Int i, Float f | Float f, Int i -> Float (float i - f)

let mulValues v1 v2 =
    match v1, v2 with
    | Int i1, Int i2 -> Int (i1 * i2)
    | Float f1, Float f2 -> Float (f1 * f2)
    | Int i, Float f | Float f, Int i -> Float (float i * f)

let divValues v1 v2 =
    match v1, v2 with
    | Int i1, Int i2 when i2 <> 0 -> Int (i1 / i2)
    | Float f1, Float f2 when f2 <> 0.0 -> Float (f1 / f2)
    | Int i, Float f when f <> 0.0 -> Float (float i / f)
    | Float f, Int i when i <> 0 -> Float (f / float i)
    | _ -> failwith "Division by zero"

let modValues v1 v2 =
    match v1, v2 with
    | Int i1, Int i2 when i2 <> 0 -> Int (i1 % i2)
    | Int _, Int _ -> failwith "Division by zero"
    | _ -> failwith "Modulo operation is only valid for integers"

let powValues v1 v2 =
    match v1, v2 with
    | Int i1, Int i2 -> Int (int (float i1 ** float i2))
    | Float f1, Float f2 -> Float (f1 ** f2)
    | Int i, Float f | Float f, Int i -> Float (float i ** f)