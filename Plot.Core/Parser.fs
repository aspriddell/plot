module public Plot.Core.Parser

open System.Collections.Generic
open Plot.Core
open Plot.Core.Symbols

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
        | TokenType.NumI _ :: tail -> tail
        | TokenType.NumF _ :: tail -> tail
        | TokenType.Var _ :: Eq :: tail -> (Expr >> Base) tail
        | TokenType.Var _ :: tail -> tail
        | TokenType.LPar :: tail -> match Expr tail with
                                    | TokenType.RPar :: tail -> tail
                                    | _ -> failwith "Parser error"
        | _ -> failwith "Parser error"

    Expr tokenList
    
let ParseAndEval tList =
    let rec symbolTable = Dictionary<string, SymbolType>()

    and Expr tList = (Term >> ExprOpt) tList
    and ExprOpt (tList, value) = 
        match tList with
        | TokenType.Add :: tail -> let (tLst, tVal) = Term tail
                                   ExprOpt (tLst, addValues value tVal)
        | TokenType.Sub :: tail -> let (tLst, tVal) = Term tail
                                   ExprOpt (tLst, subValues value tVal)
        | _ -> (tList, value)
    and Term tList = (Factor >> TermOpt) tList
    and TermOpt (tList, value) =
        match tList with
        | TokenType.Mul :: tail -> let (tLst, tVal) = Factor tail
                                   TermOpt (tLst, mulValues value tVal)
        | TokenType.Div :: tail -> let (tLst, tVal) = Factor tail
                                   TermOpt (tLst, divValues value tVal)
        | TokenType.Mod :: tail -> let (tLst, tVal) = Factor tail
                                   TermOpt (tLst, modValues value tVal)
        | _ -> (tList, value)
    and Factor tList = (Base >> FactorOpt) tList
    and FactorOpt (tList, value) =
        match tList with
        | TokenType.Pow :: tail -> let (tLst, tVal) = Base tail
                                   FactorOpt (tLst, powValues value tVal)
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
            | _ -> failwith $"Variable \"{name}\" is not defined"

        // parenthesis
        | TokenType.LPar :: tail -> let (tLst, tVal) = Expr tail
                                    match tLst with 
                                    | TokenType.RPar :: tail -> (tail, tVal)
                                    | _ -> failwith "Parser error"

        | _ -> failwith "Parser error"

    and ParseStatements tList =
        match tList with
        | [] -> ()
        | TokenType.NewLine :: tail -> ParseStatements tail
        | _ ->
            let remaining, result = Expr tList
            // Print properly formatted numbers (whole numbers without tailing zeros and floats with appropriate precision) 
            printfn $"%s{result.ToString()}"
            ParseStatements remaining

    ParseStatements tList
    