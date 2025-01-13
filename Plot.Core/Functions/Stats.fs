module Plot.Core.Functions.Stats

open Plot.Core.Extensibility.Functions
open Plot.Core.Extensibility.Symbols

let internal varianceCalc (vals: float list) : float =
    // calculate mean, subtract each value from mean, square and sum
    let avg = vals |> List.average
    let sum = Seq.map (fun x -> (x - avg) ** 2.0) vals |> Seq.sum
    
    // result is the sum divided by the original length
    sum / float (List.length vals)
    
// https://www.calculatorsoup.com/calculators/discretemathematics/combinations.php
let internal ncrCalc (n: int) (r: int): int =
    if n <= 0 || r <= 0 then 1
    else
        let nFactorial = Seq.fold (*) 1 { 1 .. n }
        let rFactorial = Seq.fold (*) 1 { 1 .. r }
        let nSubRFactorial = Seq.fold (*) 1 { 1 .. (n - r) }
        
        nFactorial / (rFactorial * nSubRFactorial)

// nPr = n! / (n - r)!
let internal nprCalc (n: int) (r: int): int =
    if n <= 0 || r <= 0 then 1
    else
        let nFactorial = Seq.fold (*) 1 { 1 .. n }
        let nSubRFactorial = Seq.fold (*) 1 { 1 .. (n - r) }

        nFactorial / nSubRFactorial

[<PlotScriptFunction("avg")>]
[<PlotScriptFunction("average")>]
[<PlotScriptFunction("mean")>]
let public average (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> Seq.map Base.unwrap lst |> Seq.average |> Float
    | _ -> invalidArg "*" "average requires a single list of numbers"
    
[<PlotScriptFunction("med")>]
[<PlotScriptFunction("median")>]
let public median (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> 
        let sorted = Seq.map Base.unwrap lst |> Seq.sort |> Seq.toList
        let length = List.length sorted
        // handle even sized lists
        if length % 2 = 0 then
            let mid = length / 2
            let left = sorted[mid - 1]
            let right = sorted[mid]
            Float((left + right) / 2.0)
        else
            let mid = length / 2
            Float(sorted[mid])
    | _ -> invalidArg "*" "median requires a single list of numbers"

[<PlotScriptFunction("mode")>]
let public mode (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> 
        let grouped = Seq.map Base.unwrap lst |> Seq.groupBy id |> Seq.toList // id = self
        
        if List.length grouped = List.length lst
        then
            List([])
        else 
            let maxGroupSize = grouped |> Seq.map (fun (_, x) -> Seq.length x) |> Seq.max
            let matchingGroups = grouped |> Seq.filter (fun (_, x) -> Seq.length x = maxGroupSize)
            
            List(Seq.map (fun (k, _) -> Float(k)) matchingGroups |> List.ofSeq)
    | _ -> invalidArg "*" "mode requires a single list of numbers"
    
[<PlotScriptFunction("sum")>]
let public sum (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> Seq.map Base.unwrap lst |> Seq.sum |> Float
    | _ -> invalidArg "*" "sum requires a single list of numbers"

[<PlotScriptFunction("min")>]
let public min (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> Seq.map Base.unwrap lst |> Seq.min |> Float
    | _ -> invalidArg "*" "min requires a single list of numbers"

[<PlotScriptFunction("max")>]
let public max (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> Seq.map Base.unwrap lst |> Seq.max |> Float
    | _ -> invalidArg "*" "min requires a single list of numbers"

[<PlotScriptFunction("variance")>]
let public variance (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] -> Float(varianceCalc (List.map Base.unwrap lst))
    | _ -> invalidArg "*" "variance requires a single list of numbers"

[<PlotScriptFunction("stdev")>]
let public stdev (x: SymbolType list) : SymbolType =
    match x with
    | [ List lst ] ->
        let variance = varianceCalc (List.map Base.unwrap lst)
        Float(sqrt variance) // stdev = sqrt(variance)
    | _ -> invalidArg "*" "stdev requires a single list of numbers"

[<PlotScriptFunction("ncr")>]
let public ncr (x: SymbolType list) : SymbolType =
    match x with
    | [Int n; Int r] -> Int(ncrCalc n r)
    | _ -> invalidArg "*" "ncr requires two integer parameters"

[<PlotScriptFunction("npr")>]
let public npr (x: SymbolType list) : SymbolType =
    match x with
    | [Int n; Int r] -> Int(nprCalc n r)
    | _ -> invalidArg "*" "npr requires two integer parameters"

[<PlotScriptFunction("binomial")>]
[<PlotScriptFunction("binomialpd")>]
let public binomialDist (x: SymbolType list) : SymbolType =
    match x with
    | [Int x; Int N; Float p] when x >= 0 && N >= 0 && N >= x && (p >= 0.0 && p <= 1.0) ->
        // https://www.ncl.ac.uk/webtemplate/ask-assets/external/maths-resources/business/probability/binomial-distribution.html
        Float (float (ncrCalc N x) * (p ** x) * ((1.0 - p) ** float (N - x)))
    | _ -> invalidArg "*" "binomialDist requires three parameters: x, N, and p"
