module Plot.Core.Functions.Calculus

open System
open Microsoft.FSharp.Collections
open Plot.Core
open Plot.Core.Symbols


let private derivativeCalc coeffs =
    if List.length coeffs <= 1 then [0.0]
    else coeffs
        |> List.take (coeffs.Length - 1) // skip constant term
        |> List.mapi (fun i c -> c * float (coeffs.Length - i - 1))

let private polyCalc (coeffs: float list) (x: float) : float =
    coeffs |> Seq.mapi (fun i c -> c * Math.Pow(x, float (List.length coeffs - i - 1))) |> Seq.sum
    
// https://math.libretexts.org/Courses/Highline_College/MATHP_141%3A_Corequisite_Precalculus/04%3A_Polynomial_and_Rational_Functions/4.05%3A_Zeros_of_Polynomials
let private cauchyBound coeffs =
    let max = coeffs |> Seq.skip 1 |> Seq.map abs |> Seq.max
    max / (coeffs |> Seq.head |> abs)
    
let internal symbolToFloat c =
    match c with
    | Int i -> float i
    | Float f -> f
    | _ -> invalidArg "*" "expected a float or int type"

[<PlotScriptFunction("polyfn")>]
[<PlotScriptFunction("polynomial")>]
let public polyFn (x: SymbolType list) : SymbolType =
    match x with
    | [ List coeffs ] ->
        let convertedCoeffs = coeffs |> List.map symbolToFloat
        let callback = fun (inputs: SymbolType list) ->
            match inputs with
            | [ Int i ] -> polyCalc convertedCoeffs (float i) |> Float
            | [ Float f ] -> polyCalc convertedCoeffs f |> Float
            | _ -> invalidArg "*" "expected a single float or int type"

        let m = cauchyBound convertedCoeffs
        PlotScriptPolynomialFunction({ Function = callback;
                                       RealRootRange = (-(m + 1.0), (m + 1.0));
                                       Coefficients = coeffs |> List.map symbolToFloat })
    | _ -> invalidArg "*" "polyfn requires a single list of coefficients"

[<PlotScriptFunction("diff")>]
[<PlotScriptFunction("differentiate")>]
let rec public differentiate (x: SymbolType list) : SymbolType =
    let rec performWithOrder (coeffs: float list, order: int) : float list =
        match order with
        | 0 -> if coeffs.Length = 0 then [0] else coeffs
        | _ -> performWithOrder (derivativeCalc coeffs, order - 1)

    match x with
    | [ List list ] -> differentiate (list @ [Int 1])
    | [ List list; Int order ] when order > 0 -> List(performWithOrder (list |> List.map symbolToFloat, order) |> List.map Float)
    | _ -> invalidArg "*" "differentiate requires a single list of symbols and an optional, positive integer order"

[<PlotScriptFunction("solve")>]
[<PlotScriptFunction("roots")>]
let rec public findRoots (x: SymbolType list) : SymbolType =
    let generateIntervals coeffs step =
        let m = cauchyBound coeffs
        [|-(m + 1.0) .. step .. (m + 1.0)|]
            |> Array.pairwise
            |> Array.Parallel.filter (fun (a, b) -> Math.Sign(polyCalc coeffs a) <> Math.Sign(polyCalc coeffs b))
            |> Array.Parallel.map (fun (a, b) -> (a + b) / 2.0) // use the midpoint of the interval

    // https://personal.math.ubc.ca/~anstee/math104/newtonmethod.pdf
    let rec newtonRaphson coeffs coeffs' guess tolerance iterationsLeft =
        if iterationsLeft = 0 then nan else

        let nextGuess = guess - (polyCalc coeffs guess / polyCalc coeffs' guess)
        if abs (nextGuess - guess) < tolerance then nextGuess
        else newtonRaphson coeffs coeffs' nextGuess tolerance (iterationsLeft - 1)

    match x with
    // handle missing step size
    | [ List _ ] -> findRoots (x @ [Float 0.1])
    | [ List list; Int step ] -> findRoots [List list; Float (float step)]

    // handle incorrect coefficient count
    | [ List list; Float _ ] when List.length list < 2 -> List []
    | [ List list; Float step ] when step > 0 ->
        let coeffs = list |> List.map symbolToFloat
        let coeffs' = derivativeCalc coeffs

        generateIntervals coeffs step
        |> Seq.map (fun guess -> Math.Round(newtonRaphson coeffs coeffs' guess 1e-7 1000, 6))
        |> Seq.filter (fun root -> not (Double.IsNaN root))
        |> Seq.distinct
        |> Seq.map Float
        |> List.ofSeq
        |> List

    | _ -> invalidArg "*" "solve requires a single list of coefficients and an optional, positive step size"
