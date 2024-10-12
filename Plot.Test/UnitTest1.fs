module PlotTest

open NUnit.Framework
open Plot.Core

[<SetUp>]
let Setup () =
    ()

[<Test>]
let Test1 () =
    let input = "8.5 + 1\n10 * 2\n"
    let tokens = Lexer.Parse (input.ToCharArray())
    Parser.ParseAndEval tokens