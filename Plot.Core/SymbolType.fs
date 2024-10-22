module Plot.Core.Symbols

open System

type SymbolType =
    | Int of int
    | Float of float

    /// <summary>
    /// Represents a function that takes a list of symbols and produces an output.
    /// The "source" tokens are also provided for advanced processing.
    /// </summary>
    | PlotScriptFunction of ((SymbolType list -> SymbolType) * TokenType list)
    
    /// <summary>
    /// Represents a function that is used with a graph to plot something.
    /// </summary>
    | PlotScriptGraphingFunction of (SymbolType list -> SymbolType)

let internal isAssignableSymbolType(symbol: SymbolType): bool =
    match symbol with
    | PlotScriptGraphingFunction _ -> false
    | _ -> true
    
let internal negateValue (v: SymbolType): SymbolType =
    match v with
    | Int i -> Int(-i)
    | Float f -> Float(-f)

    | _ -> invalidOp "Negation not defined for the given type"

let internal addValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int(i1 + i2)
    | Float f1, Float f2 -> Float(f1 + f2)
    | Int i, Float f
    | Float f, Int i -> Float(float i + f)
    
    | _ -> invalidOp "Addition not defined for the given types"

let internal subValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int(i1 - i2)
    | Float f1, Float f2 -> Float(f1 - f2)
    | Int i, Float f
    | Float f, Int i -> Float(float i - f)
    
    | _ -> invalidOp "Subtraction not defined for the given types"

let internal mulValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int(i1 * i2)
    | Float f1, Float f2 -> Float(f1 * f2)
    | Int i, Float f
    | Float f, Int i -> Float(float i * f)
    
    | _ -> invalidOp "Multiplication not defined for the given types"

let internal divValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 when i2 <> 0 -> Int(i1 / i2)
    | Float f1, Float f2 when f2 <> 0.0 -> Float(f1 / f2)
    | Int i, Float f when f <> 0.0 -> Float(float i / f)
    | Float f, Int i when i <> 0 -> Float(f / float i)

    // div/0
    | Int _, Int _
    | Int _, Float _
    | Float _, Int _ 
    | Float _, Float _ -> raise (DivideByZeroException())
    
    | _ -> invalidOp "Division not defined for the given types"

let internal modValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 when i2 <> 0 ->
        let result = i1 % i2
        // negative modulus - (-11) % 7 should be 3 but is -4
        // to correct this, i2 needs to be added to the result - see https://math.stackexchange.com/a/2179581
        if result < 0 then Int(result + i2) else Int(result)

    | Int _, Int _ -> raise (DivideByZeroException())
    | _ -> invalidOp "Modulus operator is only defined for integers"

let internal powValues (v1: SymbolType, v2: SymbolType): SymbolType =
    match v1, v2 with
    | Int i1, Int i2 -> Int(int (float i1 ** float i2))
    | Float f1, Float f2 -> Float(f1 ** f2)
    | Int i, Float f
    | Float f, Int i -> Float(float i ** f)
    
    | _ -> invalidOp "Power operator is only defined for integers and floats"
