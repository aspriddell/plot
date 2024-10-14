module public Plot.Core.Parser

open System.Collections.Generic
open Plot.Core
open Plot.Core.Symbols

exception ParserError of message: string
exception VariableError of message: string * varName: string

// Grammar:
// <:Root:>      ::= <Assign> | <Expr> | <empty>
// <Assign>      ::= "Var" "=" <Expr>
// <Expr>        ::= <Term> <ExprOpt>
// <ExprOpt>     ::= "+" <Term> <ExprOpt> | "-" <Term> <ExprOpt> | <empty>
// <Term>        ::= <Factor> <TermOpt>
// <TermOpt>     ::= "*" <Factor> <TermOpt> | "/" <Factor> <TermOpt> | "%" <Factor> <TermOpt> | <empty>
// <Factor>      ::= <Base> <FactorOpt> | "-" <Base> <FactorOpt>
// <FactorOpt>   ::= "^" <Base> <FactorOpt> | <empty>
// <Base>        ::= <Number> | <Identifier> | "(" <Expr> ")" | <FnCall>
// <Number>      ::= "NumI" <value> | "NumF" <value>
// <FnCall>      ::= <Identifier> "(" <Arguments> ")"
// <Arguments>   ::= <Expr> | ("," <Expr>)* | <empty>

let public ParseAndEval(tList: TokenType list, symbolTable: IDictionary<string, SymbolType>): SymbolType seq =
    let rec Expr tList = (Term >> ExprOpt) tList
    and ExprOpt (tList, value) =
        match tList with
        | TokenType.Add :: tail -> let (tLst, tVal) = Term tail
                                   ExprOpt (tLst, addValues(value, tVal))
        | TokenType.Sub :: tail -> let (tLst, tVal) = Term tail
                                   ExprOpt (tLst, subValues(value, tVal))
        | _ -> (tList, value)
    and Term tList = (Factor >> TermOpt) tList
    and TermOpt (tList, value) =
        match tList with
        | TokenType.Mul :: tail -> let (tLst, tVal) = Factor tail
                                   TermOpt (tLst, mulValues(value, tVal))
        | TokenType.Div :: tail -> let (tLst, tVal) = Factor tail
                                   TermOpt (tLst, divValues(value, tVal))
        | TokenType.Mod :: tail -> let (tLst, tVal) = Factor tail
                                   TermOpt (tLst, modValues(value, tVal))
        | _ -> (tList, value)
    and Factor tList =
        match tList with
        | TokenType.Sub :: tail -> let (tLst, tVal) = Base tail
                                   FactorOpt (tLst, subValues(SymbolType.Int 0, tVal))
        | _ -> (Base >> FactorOpt) tList
    and FactorOpt (tList, value) =
        match tList with
        | TokenType.Pow :: tail -> let (tLst, tVal) = Base tail
                                   FactorOpt (tLst, powValues(value, tVal))
        | _ -> (tList, value)
    and Base tList =
        match tList with
        | TokenType.NumI value :: tail -> (tail, SymbolType.Int value)
        | TokenType.NumF value :: tail -> (tail, SymbolType.Float value)

        // prevent assignment in a block
        | TokenType.Identifier name :: Eq :: _ -> raise (VariableError("Assignment failed", name))      
        | TokenType.Identifier name :: tail ->
            match symbolTable.TryGetValue(name) with
            | true, value -> (tail, value)
            | _ -> raise (VariableError($"\"{name}\" is not defined", name))

        | TokenType.LPar :: tail -> let (tLst, tVal) = Expr tail
                                    match tLst with
                                    | TokenType.RPar :: tail -> (tail, tVal)
                                    | _ -> raise (ParserError "One or more set of parentheses were not closed.")

        | _ -> raise (ParserError "Parser error")
    and Assign tList =
        match tList with
        | TokenType.Identifier name :: TokenType.Eq :: tail ->
            let (remaining, result) = Expr tail
            symbolTable[name] <- result
            (remaining, result)
        | _ -> raise (VariableError("Variable assignment failed", ""))
    and Root tList =
        seq {
            match tList with
            | [] -> ()
            | TokenType.NewLine :: tail ->
                yield! Root tail
            | TokenType.Identifier _ :: TokenType.Eq :: _ ->
                let (remaining, _) = Assign tList
                yield! Root remaining
            | _ ->
                let remaining, result = Expr tList

                yield result
                yield! Root remaining
        }

    Root tList
    