module Plot.Core.Functions.Base

open System
open System.Diagnostics
open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("int")>]
let public castToInt (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Int i
    | Float f -> Int(int f)
    | _ -> invalidArg "*" "int expects a single number argument"

[<PlotScriptFunction("float")>]
let public castToFloat (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Int i -> Float(float i)
    | Float f -> Float f
    | _ -> invalidArg "*" "float expects a single number argument"

[<PlotScriptFunction("pow")>]
let public pow (x: SymbolType list) : SymbolType =
    match x with
    | [ Int i1; Int i2 ] -> Int(int (float i1 ** i2))
    | [ Float f1; Float f2 ] -> Float(f1 ** f2)

    | [ Int i; Float f ]
    | [ Float f; Int i ] -> Float(f ** float i)

    | [ _; _ ] -> invalidOp "pow not defined for the given types"
    | _ -> invalidArg "*" "pow expects two arguments"

[<PlotScriptFunction("ln")>]
let public ln (x: SymbolType list) : SymbolType =
    match x with
    | [ Int i ] -> Float(log i)
    | [ Float f ] -> Float(log f)

    | _ -> invalidArg "*" "ln expects one argument"

[<PlotScriptFunction("log")>]
let public log (x: SymbolType list) : SymbolType =
    match x with
    // log(num) assumes base 10 unless specified
    | [ Int i ] -> Float(log10 i)
    | [ Float f ] -> Float(log10 f)

    | [ Int i; Int b ] -> Float(Math.Log(i, b))
    | [ Int i; Float b ] -> Float(Math.Log(i, b))
    | [ Float i; Int b ] -> Float(Math.Log(i, b))
    | [ Float i; Float b ] -> Float(Math.Log(i, b))

    | _ -> invalidArg "*" "log expects one or two arguments"

[<PlotScriptFunction("floor")>]
let public floor (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Float f -> Float(int f)
    | _ -> invalidArg "*" "floor expects a single floating-point argument"

[<PlotScriptFunction("ceil")>]
let public ceil (x: SymbolType list) : SymbolType =
    match x |> Seq.exactlyOne with
    | Float f -> Float(Math.Ceiling(f))
    | _ -> invalidArg "*" "ceil expects a single floating-point argument"

[<PlotScriptFunction("rand")>]
let public randInt (x: SymbolType list) : SymbolType =
    match x with
    | [] -> Int(Random.Shared.Next())
    | [ Int i ] -> Int(Random.Shared.Next(i))
    | [ Int i1; Int i2 ] -> Int(Random.Shared.Next(i1, i2))

    | [ Float _ ]
    | [ Float _; Int _ ]
    | [ Float _; Float _ ] -> invalidArg "*" "randInt expects integer arguments"

    | _ -> invalidArg "*" "randInt expects between zero and two integer arguments"

[<PlotScriptFunction("fact")>]
[<PlotScriptFunction("factorial")>]
let public factorial (x: SymbolType list) : SymbolType =
    match x with
    | [ Int i ] -> Int(Seq.fold (*) 1 { 1 .. i })
    | _ -> invalidArg "*" "factorial expects a single integer argument"

[<PlotScriptFunction("roll")>]
let public rickRoll (_x: SymbolType list) : SymbolType =
    let processStartInfo =
        ProcessStartInfo("https://www.youtube.com/watch?v=dQw4w9WgXcQ")

    processStartInfo.UseShellExecute <- true
    processStartInfo.Verb <- "open"

    Process.Start(processStartInfo) |> ignore
    Unit
