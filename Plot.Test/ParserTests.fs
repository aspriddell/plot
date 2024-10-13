module Plot.Test.ParserTests

open NUnit.Framework
open Plot.Core
open Plot.Core.Symbols

let ParserTestCases: TestCaseData list = [
    TestCaseData("10 * 10", SymbolType.Int(100))
    TestCaseData("10 / 3", SymbolType.Int(3))
    TestCaseData("10 / 3.0", SymbolType.Float(float 10 / 3.0))
    TestCaseData("10 % 3", SymbolType.Int(1))
    TestCaseData("10 ^ 3", SymbolType.Int(1000))
    TestCaseData("120^4 % 32", SymbolType.Int(0))
    TestCaseData("3.456", SymbolType.Float(float 3.456))
]

[<TestCaseSource(nameof ParserTestCases)>]
let TestSimpleExpressionParsing (input: string, output: SymbolType) =
    let outputSeq = input |> Lexer.Parse |> Parser.ParseAndEval
    match output, outputSeq |> Seq.head with
    | SymbolType.Float floatExpected, SymbolType.Float floatOut ->
        // floats are expected to work for 6-9 digits
        // see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types#characteristics-of-the-floating-point-types
        Assert.That(floatOut, Is.EqualTo(floatExpected).Within(0.00001))
     | expected, head ->
        Assert.That(head, Is.EqualTo(expected))
