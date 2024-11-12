module Plot.Core.Functions.Calculus

open Plot.Core
open Plot.Core.Symbols

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
