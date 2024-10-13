
module Plot.Test.LexerTests

open NUnit.Framework
open Plot.Core

let LexerTestCases: TestCaseData list =
    [
      TestCaseData("10 * 10", [ NumI(10); Mul; NumI(10) ])
      TestCaseData("10 / 3", [ NumI(10); Div; NumI(3) ])
      TestCaseData("10 / 3.0", [ NumI(10); Div; NumF(3) ])
      TestCaseData("x = 120^4 % 32", [ Var("x"); Eq; NumI(120); Pow; NumI(4); Mod; NumI(32) ])
      TestCaseData("72.55^3 / (7 % 14.25 ^ 0.75)", [ NumF(72.55); Pow; NumI(3); Div; LPar; NumI(7); Mod; NumF(14.25); Pow; NumF(0.75); RPar ])
    ]

[<TestCaseSource(nameof LexerTestCases)>]
let TestLexerParsing (input: string, tree: TokenType list) =
    let tokens = Lexer.Parse input
    for token1, token2 in List.zip tokens tree do
        Assert.AreEqual(token1, token2)
