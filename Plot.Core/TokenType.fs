namespace Plot.Core

type public TokenType = 
    | Add // add (+)
    | Sub // subtract (-)
    | Mul // multiply (*)
    | Div // divide (/)
    | Mod // moudlo (%)
    | Pow // power (^)
    | LPar // left parenthesis (
    | RPar // right parenthesis )
    | Var of string // variable
    | NumI of int64 // integer number
    | NumF of float // floating point number