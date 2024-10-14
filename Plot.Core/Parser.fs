module public Plot.Core.Parser

open System.Collections.Generic
open Plot.Core
open Plot.Core.Symbols

exception ParserError of message: string
exception VariableError of message: string * varName: string

// Grammar:
// <Expr>        ::= <Term> <ExprOpt>
// <ExprOpt>     ::= "+" <Term> <ExprOpt> | "-" <Term> <ExprOpt> | <empty>
// <Term>        ::= <Factor> <TermOpt>
// <TermOpt>     ::= "*" <Factor> <TermOpt> | "/" <Factor> <TermOpt> | "%" <Factor> <TermOpt> | <empty>
// <Factor>      ::= <Base> <FactorOpt> | "-" <Base> <FactorOpt>
// <FactorOpt>   ::= "^" <Base> <FactorOpt> | <empty>
// <Base>        ::= <Number> | <Identifier> "=" <Expr> | <Identifier> | "(" <Expr> ")"
// <Number>      ::= "NumI" <value> | "NumF" <value>
// <Variable>    ::= "Var" <name>

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
    and Factor tokenList =
       match tokenList with
        | TokenType.Sub :: tail -> (Base >> FactorOpt) tail
        | _ -> (Base >> FactorOpt) tokenList
    and FactorOpt tokenList =
        match tokenList with
        | TokenType.Pow :: tail -> (Base >> FactorOpt) tail
        | _ -> tokenList
    and Base tokenList =
        match tokenList with
        | TokenType.NumI _ :: tail -> tail
        | TokenType.NumF _ :: tail -> tail
        | TokenType.Var _ :: Eq :: tail -> (Expr >> Base) tail
        | TokenType.Var _ :: tail -> tail
        | TokenType.LPar :: tail -> match Expr tail with
                                    | TokenType.RPar :: tail -> tail
                                    | _ -> raise (ParserError "One or more set of parenthesis were not closed.")

        | _ -> raise (ParserError "Parser error")

    Expr tokenList

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
        // negative numbers (unary operator) is the same as subtracting from 0
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
        // inline symbol (not defined in the symbol table)
        | TokenType.NumI value :: tail -> (tail, SymbolType.Int value)
        | TokenType.NumF value :: tail -> (tail, SymbolType.Float value)

        // variable assignment
        | TokenType.Var name :: Eq :: tail ->
            let (remaining, result) = Expr tail
            symbolTable[name] <- result
            (remaining, result)

        // variable lookup
        | TokenType.Var name :: tail ->
            match symbolTable.TryGetValue(name) with
            | true, value -> (tail, value)
            | _ -> raise (VariableError($"\"{name}\" is not defined", name))

        // parenthesis
        | TokenType.LPar :: tail -> let (tLst, tVal) = Expr tail
                                    match tLst with 
                                    | TokenType.RPar :: tail -> (tail, tVal)
                                    | _ -> raise (ParserError "One or more set of parenthesis were not closed.")

        | _ -> raise (ParserError "Parser error")

    and ParseStatements tList =
        seq {
            match tList with
            | [] -> ()
            | TokenType.NewLine :: tail -> yield! ParseStatements tail
            | _ ->
                let remaining, result = Expr tList

                yield result
                yield! ParseStatements remaining
        }

    ParseStatements tList
    