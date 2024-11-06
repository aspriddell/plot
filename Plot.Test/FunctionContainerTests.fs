module Plot.Test.FunctionContainerTests

open System
open System.Collections.Generic
open NUnit.Framework
open Plot.Core
open Plot.Core.Symbols

let FunctionContainerTests: TestCaseData list = [
    TestCaseData("sin", [Float(Math.PI)], Float(0))
    TestCaseData("cos", [Float(Math.PI)], Float(-1))
    TestCaseData("tan", [Float(Math.PI / 4.0)], Float(1))
]

[<TestCaseSource(nameof FunctionContainerTests)>]
let TestFunctionContainer (name: string, input: SymbolType list, expected: SymbolType) =
    let actual = PlotScriptFunctionContainer.Default.FunctionTable[name] (input, Dictionary<string, SymbolType>())
    match actual, expected with
    | Float(a), Float(e) -> Assert.That(a, Is.EqualTo(e).Within(0.00001))
    | _ -> Assert.That(actual, Is.EqualTo(expected))