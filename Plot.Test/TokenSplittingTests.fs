module Plot.Test.TokenSplittingTests

open NUnit.Framework
open Plot.Core

let TokenSplitTestCases: TestCaseData list = [
    TestCaseData([NumI(100); Add; Identifier("x"); Comma; NumI(200); Comma; Identifier("a")], [[NumI(100); Add; Identifier("x")]; [NumI(200)]; [Identifier("a")]])
]

[<TestCaseSource(nameof TokenSplitTestCases)>]
let TestTokenSplitting (input: TokenType list, output: TokenType list list) =
    let outputSeq = TokenUtils.splitTokenArguments input
    Assert.That(outputSeq, Is.EqualTo(output))
