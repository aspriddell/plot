module Plot.Core.Functions.Calculus

open System
open Plot.Core
open Plot.Core.Symbols


let private derivative coeffs =
    coeffs
    |> List.take (coeffs.Length - 1) // skip final term (constant)
    |> List.mapi (fun i c -> c * float (coeffs.Length - i - 1)) // do multiplication
    
let private toFloat c =
    match c with
    | Int i -> float i
    | Float f -> f
    | _ -> invalidArg "*" "expected a float or int type"

[<PlotScriptFunction("polyfn")>]
[<PlotScriptFunction("polynomial")>]
let public polyFn (x: SymbolType list) : SymbolType =
    // create a plotscript function from the given coefficients
    // if the coefficients are [a, b, c], the function will be a^2 + bx + c
    let rec powMul xVal coeffs acc =
        match coeffs with
        | [] -> acc
        | Int v :: tail -> powMul xVal tail (Math.FusedMultiplyAdd(float v, Math.Pow(xVal, float tail.Length), acc))
        | Float v :: tail -> powMul xVal tail (Math.FusedMultiplyAdd(float v, Math.Pow(xVal, float tail.Length), acc))
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
let rec public differentiate (x: SymbolType list) : SymbolType =
    let rec performWithOrder (coeffs: float list, order: int) : float list =
        match order with
        | 0 -> if coeffs.Length = 0 then [0] else coeffs
        | _ -> performWithOrder (derivative coeffs, order - 1)

    match x with
    | [ List list ] -> differentiate (list @ [Int 1])
    | [ List list; Int order ] when order > 0 -> List(performWithOrder (list |> List.map toFloat, order) |> List.map (fun f -> Float(f)))
    | _ -> invalidArg "*" "differentiate requires a single list of symbols and an optional, positive integer order"

[<PlotScriptFunction("solve")>]
[<PlotScriptFunction("roots")>]
[<PlotScriptFunction("findroots")>]
let rec public findRoots (x: SymbolType list) : SymbolType =
    let polynomial (coeffs: float list) (x: float) : float =
        coeffs |> Seq.mapi (fun i c -> c * Math.Pow(x, float (List.length coeffs - i - 1))) |> Seq.sum

    // https://math.libretexts.org/Courses/Highline_College/MATHP_141%3A_Corequisite_Precalculus/04%3A_Polynomial_and_Rational_Functions/4.05%3A_Zeros_of_Polynomials
    let cauchyBound coeffs =
        let max = coeffs |> Seq.skip 1 |> Seq.map abs |> Seq.max
        max / (coeffs |> Seq.head |> abs)

    let generateIntervals coeffs step =
        let m = cauchyBound coeffs

        [-(m + 1.0) .. step .. (m + 1.0)]
        |> Seq.pairwise // pair up values
        |> Seq.filter (fun (a, b) -> Math.Sign(polynomial coeffs a) <> Math.Sign(polynomial coeffs b)) // check for sign change
        |> Seq.map (fun (a, b) -> (a + b) / 2.0) // use the midpoint of the interval

    // https://personal.math.ubc.ca/~anstee/math104/newtonmethod.pdf
    let rec newtonRaphson coeffs coeffs' guess tolerance iterationsLeft =
        if iterationsLeft = 0 then nan else

        let nextGuess = guess - (polynomial coeffs guess / polynomial coeffs' guess)
        if abs (nextGuess - guess) < tolerance then nextGuess
        else newtonRaphson coeffs coeffs' nextGuess tolerance (iterationsLeft - 1)

    match x with
    // handle missing step size
    | [ List _ ] -> findRoots (x @ [Float 0.1])
    | [ List list; Int step ] -> findRoots [List list; Float (float step)]
    
    // handle incorrect coefficient count
    | [ List list; Float _ ] when List.length list < 2 -> List []
    | [ List list; Float step ] when step > 0 ->
        let coeffs = list |> List.map toFloat
        let coeffs' = derivative coeffs

        generateIntervals coeffs step
        |> Seq.map (fun guess -> newtonRaphson coeffs coeffs' guess 1e-7 1000)
        |> Seq.filter (fun root -> not (Double.IsNaN root))
        |> Seq.distinct
        |> Seq.map Float
        |> List.ofSeq
        |> List

    | _ -> invalidArg "*" "solve requires a single list of coefficients and an optional, positive step size"
