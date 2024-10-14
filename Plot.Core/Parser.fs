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
    and ExprOpt (tList, value, isAssignment) =
        match tList with
        | TokenType.Add :: tail -> let (tLst, tVal, _) = Term tail
                                   ExprOpt (tLst, addValues(value, tVal), isAssignment)
        | TokenType.Sub :: tail -> let (tLst, tVal, _) = Term tail
                                   ExprOpt (tLst, subValues(value, tVal), isAssignment)
        | _ -> (tList, value, isAssignment)
    and Term tList = (Factor >> TermOpt) tList
    and TermOpt (tList, value, isAssignment) =
        match tList with
        | TokenType.Mul :: tail -> let (tLst, tVal, _) = Factor tail
                                   TermOpt (tLst, mulValues(value, tVal), isAssignment)
        | TokenType.Div :: tail -> let (tLst, tVal, _) = Factor tail
                                   TermOpt (tLst, divValues(value, tVal), isAssignment)
        | TokenType.Mod :: tail -> let (tLst, tVal, _) = Factor tail
                                   TermOpt (tLst, modValues(value, tVal), isAssignment)
        | _ -> (tList, value, isAssignment)
    and Factor tList =
        match tList with
        | TokenType.Sub :: tail -> let (tLst, tVal, _) = Base tail
                                   FactorOpt (tLst, subValues(SymbolType.Int 0, tVal), false)
        | _ -> (Base >> FactorOpt) tList
    and FactorOpt (tList, value, isAssignment) =
        match tList with
        | TokenType.Pow :: tail -> let (tLst, tVal, _) = Base tail
                                   FactorOpt (tLst, powValues(value, tVal), isAssignment)
        | _ -> (tList, value, isAssignment)
    and Base tList =
        match tList with
        | TokenType.NumI value :: tail -> (tail, SymbolType.Int value, false)
        | TokenType.NumF value :: tail -> (tail, SymbolType.Float value, false)
        | TokenType.Var name :: Eq :: tail ->
            let (remaining, result, _) = Expr tail
            symbolTable[name] <- result
            (remaining, result, true)
        | TokenType.Var name :: tail ->
            match symbolTable.TryGetValue(name) with
            | true, value -> (tail, value, false)
            | _ -> raise (VariableError($"\"{name}\" is not defined", name))
        | TokenType.LPar :: tail -> let (tLst, tVal, _) = Expr tail
                                    match tLst with
                                    | TokenType.RPar :: tail -> (tail, tVal, false)
                                    | _ -> raise (ParserError "One or more set of parenthesis were not closed.")
        | _ -> raise (ParserError "Parser error")

    and ParseStatements tList =
        seq {
            match tList with
            | [] -> ()
            | TokenType.NewLine :: tail -> yield! ParseStatements tail
            | _ ->
                let remaining, result, isAssignment = Expr tList
                if not isAssignment then
                    yield result

                yield! ParseStatements remaining
        }

    ParseStatements tList
    