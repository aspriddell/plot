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
]

[<TestCaseSource(nameof ParserTestCases)>]
let TestSimpleExpressionParsing (input: string, output: SymbolType) =
    let outputSeq = input |> Lexer.Parse |> Parser.ParseAndEval
    Assert.AreEqual(outputSeq |> Seq.head, output)
