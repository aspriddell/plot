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
    | Eq // assignment (=)
    | Var of string // variable
    | NumI of int // integer number
    | NumF of float // floating point number