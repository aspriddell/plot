module Plot.Core.Functions.Calculus

open System
open Plot.Core
open Plot.Core.Symbols


[<PlotScriptFunction("polyfn")>]
[<PlotScriptFunction("polynomial")>]
let public polyFn (x: SymbolType list) : SymbolType =
    // create a plotscript function from the given coefficients
    // if the coefficients are [a, b, c], the function will be a^2 + bx + c
    let rec powMul xVal coeffs acc =
        match coeffs with
        | [] -> acc
        | Int v :: tail -> powMul xVal tail (float v * Math.Pow(xVal, float tail.Length) + acc)
        | Float v :: tail -> powMul xVal tail (float v * Math.Pow(xVal, float tail.Length) + acc)
        | _ -> invalidArg "*" "expected a float or int type"

    and processFromInput inputs coeffs =
        match inputs with
        | [ Int i ] -> Float(powMul (float i) coeffs 0)
        | [ Float f ] -> Float(powMul f coeffs 0)
        | _ -> invalidArg "*" "expected a single float or int type"

    match x with
    | [ List coeffs ] -> PlotScriptFunction((fun i -> processFromInput i coeffs), [])
    | _ -> invalidArg "*" "polyfn requires a single list of coefficients"

[<PlotScriptFunction("diff")>]
[<PlotScriptFunction("differentiate")>]
let public differentiate (x: SymbolType list) : SymbolType =
    let rec performInternal (coeffs: SymbolType list, out: SymbolType list) : SymbolType list =
        match coeffs with
        | []
        | [ Int _ ]
        | [ Float _ ] -> out

        | Int coeff :: tail ->
            let derivative = Int(coeff * tail.Length)
            performInternal (tail, out @ [ derivative ])
        | Float coeff :: tail ->
            let derivative = Float(coeff * float tail.Length)
            performInternal (tail, out @ [ derivative ])
        | _ -> invalidArg "*" "expected a float or int type"

    and performWithOrder (coeffs: SymbolType list, order: int) : SymbolType list =
        if order = 0 then coeffs
        else performWithOrder (performInternal (coeffs, []), order - 1)

    match x with
    | [ List list ] -> List(performWithOrder (list, 1))
    | [ List list; Int order ] when order > 0 -> List(performWithOrder (list, order))
    | _ -> invalidArg "*" "differentiate requires a single list of symbols and an optional, positive integer order"
