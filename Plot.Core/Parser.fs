module public Plot.Core.Parser

open Plot.Core

// Grammar:
// <Expr>        ::= <Term> <ExprOpt>
// <ExprOpt>     ::= "+" <Term> <ExprOpt> | "-" <Term> <ExprOpt> | <empty>
// <Term>        ::= <Factor> <TermOpt>
// <TermOpt>     ::= "*" <Factor> <TermOpt> | "/" <Factor> <TermOpt> | "%" <Factor> <TermOpt> | <empty>
// <Factor>      ::= <Base> <FactorOpt>
// <FactorOpt>   ::= "^" <Base> <FactorOpt> | <empty>
// <Base>        ::= "NumI" <value> | "NumF" <value> | "(" <Expr> ")"

let Parse tokenList =
    let rec Expr tokenList = (Term >> ExprOpt) tokenList
    and ExprOpt tokenList =
        match tokenList with
        | TokenType.Add :: tail -> (Term >> ExprOpt) tail
        | TokenType.Sub :: tail -> (Term >> ExprOpt) tail
        | _ -> tokenList
    and Term tokenList = (Factor >> TermOpt) tokenList
    and TermOpt tokenList =
        match tokenList with
        | TokenType.Mul :: tail -> (Factor >> TermOpt) tail
        | TokenType.Div :: tail -> (Factor >> TermOpt) tail
        | TokenType.Mod :: tail -> (Factor >> TermOpt) tail
        | _ -> tokenList
    and Factor tokenList = (Base >> FactorOpt) tokenList
    and FactorOpt tokenList =
        match tokenList with
        | TokenType.Pow :: tail -> (Base >> FactorOpt) tail
        | _ -> tokenList
    and Base tokenList =
        match tokenList with
        | TokenType.NumI value :: tail -> tail
        | TokenType.LPar :: tail -> match Expr tail with
                                    | TokenType.RPar :: tail -> tail
                                    | _ -> failwith "Parser error"
        | _ -> failwith "Parser error"

    Expr tokenList
    
let ParseAndEval tList = 
    let rec Expr tList = (Term >> ExprOpt) tList
    and ExprOpt (tList, value) = 
        match tList with
        | Add :: tail -> let (tLst, tval) = Term tail
                         ExprOpt (tLst, value + tval)
        | Sub :: tail -> let (tLst, tval) = Term tail
                         ExprOpt (tLst, value - tval)
        | _ -> (tList, value)
    and Term tList = (Factor >> TermOpt) tList
    and TermOpt (tList, value) =
        match tList with
        | Mul :: tail -> let (tLst, tval) = Factor tail
                         TermOpt (tLst, value * tval)
        | Div :: tail -> let (tLst, tval) = Factor tail
                         TermOpt (tLst, value / tval)
        | Mod :: tail -> let (tLst, tval) = Factor tail
                         TermOpt (tLst, value % tval)
        | _ -> (tList, value)
    and Factor tList = (Base >> FactorOpt) tList
    and FactorOpt (tList, value) =
        match tList with
        | Pow :: tail -> let (tLst, tval) = Base tail
                         FactorOpt (tLst, System.Math.Pow(value, tval))
        | _ -> (tList, value)
    and Base tList =
        match tList with 
        | NumI value :: tail -> (tail, value)
        | NumF value :: tail -> (tail, value)
        | LPar :: tail -> let (tLst, tval) = Expr tail
                          match tLst with 
                          | RPar :: tail -> (tail, tval)
                          | _ -> failwith "Parser error"
        | _ -> failwith "Parser error"

    Expr tList
    