module Plot.Core.Functions.Calculus

open System
open Microsoft.FSharp.Collections
open Plot.Core
open Plot.Core.Symbols


/// <summary>
/// Differentiates a polynomial function once
/// </summary>
let private derivativeCalc (coeffs: float list) =
    let coeffsCount = List.length coeffs

    if coeffsCount <= 1 then [ 0.0 ]
    else
        coeffs
        |> List.take (coeffsCount - 1) // skip constant term
        |> List.mapi (fun i c -> c * float (coeffsCount - i - 1))

/// <summary>
/// Calculates the output of a polynomial given the coefficients and a value to evaluate
/// </summary>
let private polyCalc (coeffs: float list) (x: float) =
    let coeffsCount = List.length coeffs
    coeffs |> Seq.mapi (fun i c -> c * Math.Pow(x, float (coeffsCount - i - 1))) |> Seq.sum

/// <summary>
/// Calculates the Cauchy bound for the provided coefficients.
/// </summary>
/// <returns>
/// <c>m</c>, the value that bounds the real roots of the polynomial in the range <c>[-(m + 1), (m + 1)]</c>.
/// </returns>
/// <remarks>
/// See https://math.libretexts.org/Courses/Highline_College/MATHP_141%3A_Corequisite_Precalculus/04%3A_Polynomial_and_Rational_Functions/4.05%3A_Zeros_of_Polynomials for more information
/// </remarks>
let private cauchyBound (coeffs: float seq) =
    let max = coeffs |> Seq.skip 1 |> Seq.map abs |> Seq.max
    max / (coeffs |> Seq.head |> abs)

/// <summary>
/// Generates a collection of intervals using the Cauchy bound, checking for sign changes and applying the Newton-Raphson method to find the roots of the remaining intervals.
/// </summary>
let internal findRoots (coeffs: float list) (step: float) =
    let generateIntervals coeffs step =
        let m = cauchyBound coeffs

        [| -(m + 1.0) .. step .. (m + 1.0) |]
        |> Array.pairwise
        |> Array.Parallel.filter (fun (a, b) -> Math.Sign(polyCalc coeffs a) <> Math.Sign(polyCalc coeffs b))
        |> Array.Parallel.map (fun (a, b) -> (a + b) / 2.0) // use the midpoint of the interval

    // https://personal.math.ubc.ca/~anstee/math104/newtonmethod.pdf
    let rec newtonRaphson coeffs coeffs' guess tolerance iterationsLeft =
        if iterationsLeft = 0 then nan
        else
            let nextGuess = guess - (polyCalc coeffs guess / polyCalc coeffs' guess)
            if abs (nextGuess - guess) < tolerance then nextGuess
            else newtonRaphson coeffs coeffs' nextGuess tolerance (iterationsLeft - 1)

    let coeffs' = derivativeCalc coeffs

    generateIntervals coeffs step
    |> Seq.map (fun guess -> Math.Round(newtonRaphson coeffs coeffs' guess 1e-7 1000, 6))
    |> Seq.filter (fun root -> not (Double.IsNaN root))
    |> Seq.distinct

/// <summary>
/// Given two polynomial functions, finds the locations where they intersect.
/// </summary>
let private findIntersection (c1: float list) (c2: float list) =
    let padWithZeros (list1: float list) (list2: float list) =
        let len1 = List.length list1
        let len2 = List.length list2

        if len1 > len2 then
            list1, List.replicate (len1 - len2) 0.0 @ list2
        else
            List.replicate (len2 - len1) 0.0 @ list1, list2

    // make sure both polynomials are of the same degree (fill the smaller order with zeros on the left)
    let coeffs1, coeffs2 = padWithZeros c1 c2
    let roots = findRoots (List.map2 (fun a b -> a - b) coeffs1 coeffs2) 0.1
    
    roots |> Seq.map (fun x -> x, polyCalc c1 x)

[<PlotScriptFunction("polyfn")>]
[<PlotScriptFunction("polynomial")>]
let public polyFn (x: SymbolType list) : SymbolType =
    match x with
    | [ List coeffs ] ->
        let convertedCoeffs = coeffs |> List.map Base.unwrap
        let m = cauchyBound convertedCoeffs
        let callback =
            fun (inputs: SymbolType list) ->
                match inputs with
                | [ Int i ] -> polyCalc convertedCoeffs (float i) |> Float
                | [ Float f ] -> polyCalc convertedCoeffs f |> Float
                | _ -> invalidArg "*" "expected a single float or int type"

        PlotScriptPolynomialFunction { Function = callback; RealRootRange = (-(m + 1.0), (m + 1.0)); Coefficients = coeffs |> List.map Base.unwrap }
    | _ -> invalidArg "*" "polyfn requires a single list of coefficients"

[<PlotScriptFunction("diff")>]
[<PlotScriptFunction("differentiate")>]
let rec public differentiate (x: SymbolType list) : SymbolType =
    let rec performWithOrder (coeffs: float list, order: int) : float list =
        match order with
        | 0 -> if coeffs.Length = 0 then [ 0 ] else coeffs
        | _ -> performWithOrder (derivativeCalc coeffs, order - 1)

    match x with
    | [ List _ ] -> differentiate (x @ [ Int 1 ])
    | [ List list; Int order ] when order > 0 -> List(performWithOrder (list |> List.map Base.unwrap, order) |> List.map Float)
    | _ -> invalidArg "*" "differentiate requires a single list of symbols and an optional, positive integer order"

[<PlotScriptFunction("solve")>]
[<PlotScriptFunction("findRoots")>]
let rec public solveFunction (x: SymbolType list) : SymbolType =
    match x with
    // handle missing step size
    | [ List _ ] -> solveFunction (x @ [ Float 0.1 ])
    | [ List list; Int step ] -> solveFunction [ List list; Float(float step) ]

    // handle incorrect coefficient count
    | [ List list; Float _ ] when List.length list < 2 -> List []
    | [ List list; Float step ] when step > 0 ->
        findRoots (list |> List.map Base.unwrap) step
        |> Seq.map Float
        |> List.ofSeq
        |> List

    | _ -> invalidArg "*" "solve requires a single list of coefficients and an optional, positive step size"

[<PlotScriptFunction("intersection")>]
let public intersection (x: SymbolType list) : SymbolType =
    match x with
    | [ List c1; List c2 ] ->
        findIntersection (c1 |> List.map Base.unwrap) (c2 |> List.map Base.unwrap) 
        |> Seq.map (fun (x, y) -> List([Float x; Float y]))
        |> List.ofSeq
        |> List

    | _ -> invalidArg "*" "intersection requires two lists of coefficients"
