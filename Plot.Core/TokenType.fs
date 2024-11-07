namespace Plot.Core

type public TokenType =
    | NewLine // newline (\n)
    | Add // add (+)
    | Sub // subtract (-)
    | Mul // multiply (*)
    | Div // divide (/)
    | Mod // modulo (%)
    | Pow // power (^)
    | LPar // left parenthesis (
    | RPar // right parenthesis )
    | LInd // left indexer ([)
    | RInd // right indexer (])
    | Eq // assignment (=)
    | Comma // comma (,)
    | NumI of int // integer number
    | NumF of float // floating point number
    | Identifier of string // identifier (variable or fn name)
