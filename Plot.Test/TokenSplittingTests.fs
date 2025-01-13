module Plot.Test.TokenSplittingTests

open NUnit.Framework
open Plot.Core
open Plot.Core.Extensibility.Tokens

let TokenSplitTestCases: TestCaseData list = [
    TestCaseData([NumI(100); Add; Identifier("x"); Comma; NumI(200); Comma; Identifier("a")], [[NumI(100); Add; Identifier("x")]; [NumI(200)]; [Identifier("a")]])
]

let FnCallExtractionTestCases: TestCaseData list = [
    TestCaseData([LPar; NumI(100); Mul; Identifier("sin"); LPar; NumF(1); RPar; RPar; NewLine; Identifier("x")], [[NumI(100); Mul; Identifier("sin"); LPar; NumF(1); RPar; RPar]; [NewLine; Identifier("x")]])
]

[<TestCaseSource(nameof TokenSplitTestCases)>]
let TestTokenSplitting (input: TokenType list, output: TokenType list list) =
    let outputSeq = TokenUtils.splitTokenArguments input
    Assert.That(outputSeq, Is.EqualTo(output))

[<TestCaseSource(nameof FnCallExtractionTestCases)>]
let TestFnCallExtraction (input: TokenType list, output: TokenType list list) =
    let (outputSeq, remaining) = TokenUtils.extractFnCallTokens input
    Assert.That(outputSeq, Is.EquivalentTo(output))
    Assert.That(remaining, Is.EquivalentTo(remaining))
