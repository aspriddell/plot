module public Plot.Core.Parser

open System.Collections.Generic
open Plot.Core
open Plot.Core.Symbols

exception ParserError of message: string
exception FunctionNotFoundError of name: string
exception VariableError of message: string * varName: string

// Grammar:
// <:Root:>         ::= <Assign> | <Expr> | <empty>
// <Assign>         ::= "Identifier" "=" <Expr> | "Identifier" = "f" "(" <:Root:> ")"
// <Expr>           ::= <Term> <ExprOpt>
// <ExprOpt>        ::= "+" <Term> <ExprOpt> | "-" <Term> <ExprOpt> | <empty>
// <Term>           ::= <Factor> <TermOpt>
// <TermOpt>        ::= "*" <Factor> <TermOpt> | "/" <Factor> <TermOpt> | "%" <Factor> <TermOpt> | <empty>
// <Factor>         ::= <Base> <FactorOpt>
// <FactorOpt>      ::= "^" <Factor> | <empty>
// <Base>           ::= "-" <Base> | <Number> | <Identifier> | "(" <Expr> ")" | <FnCall> | <Array> | <ArrayIndex>
// <Number>         ::= "NumI" <value> | "NumF" <value>

// <FnCall>         ::= <Identifier> "(" <Arguments> ")"
// <Arguments>      ::= <Expr> ("," <Expr>)* | <empty>

// <Array>          ::= "[" <ArrayElements> "]"
// <ArrayElements>  ::= <Expr> ("," <Expr>)* | <empty>
// <ArrayIndex>     ::= <Identifier> "[" <Expr> "]"

// in the implementation below, "Arguments" is handled in FnCallExec to allow for code sharing with the f(...) handler,
// which requires arguments not to be processed at the time of the call (to allow for calling the output function with different arguments)

let rec public ParseAndEval (tList: TokenType list, symbolTable: IDictionary<string, SymbolType>, fnContainer: PlotScriptFunctionContainer): SymbolType seq =
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
    and Factor tList = (Base >> FactorOpt) tList
    and FactorOpt (tList, value) =
        match tList with
        | TokenType.Pow :: tail -> let (tLst, tVal) = Base tail
                                   FactorOpt (tLst, powValues(value, tVal))
        | _ -> (tList, value)
    and Base tList =
        match tList with
        | TokenType.NumI value :: tail -> (tail, SymbolType.Int value)
        | TokenType.NumF value :: tail -> (tail, SymbolType.Float value)
        | TokenType.Sub :: tail -> let (tLst, tVal) = Base tail
                                   (tLst, negateValue tVal)

        // feature: if there's a method like pi(), you can reassign it because symbols are checked before functions
        | TokenType.Identifier name :: Eq :: _ -> raise (VariableError("Assignment failed", name))
        | [TokenType.Identifier name] when symbolTable.ContainsKey(name) -> ([], symbolTable[name])
        | TokenType.Identifier name :: tail when symbolTable.ContainsKey(name) ->
            match symbolTable[name] with
            | SymbolType.PlotScriptFunction (func, _) when tail.Head = LPar -> FnCall (fun (f, _) -> func(f)) tail
            | _ -> (tail, symbolTable[name])
        | TokenType.Identifier name :: tail when fnContainer.HasFunction(name) -> FnCall fnContainer.FunctionTable[name] tail
        | TokenType.Identifier name :: _ -> raise (VariableError("Variable not found", name))

        | TokenType.LPar :: tail -> let (tLst, tVal) = Expr tail
                                    match tLst with
                                    | TokenType.RPar :: tail -> (tail, tVal)
                                    | _ -> raise (ParserError "One or more set of parentheses were not closed.")

        | _ -> raise (ParserError "Parser error")
    and FnCall func tList =
        let FnCallExec callTokenList =
            if List.length callTokenList > 0 then
                let processedArgs = TokenUtils.splitTokenArguments callTokenList |> List.map Expr
                if processedArgs |> List.exists (fun (remaining, _) -> remaining.Length > 0) then
                    raise (ParserError "Function call failed")

                func (processedArgs |> List.map snd, symbolTable)
            else
                func ([], symbolTable)

        let (fnCallTokens, remaining) = TokenUtils.extractFnCallTokens tList
        (remaining, FnCallExec fnCallTokens)
    and Assign tList =
        match tList with
        // function assignment
        | TokenType.Identifier name :: TokenType.Eq :: TokenType.Identifier identifier :: tail when identifier = "f" && name <> "f" ->
            let (fnTokens, remaining) = TokenUtils.extractFnCallTokens tail
            let fnCallback = fun inputs ->
                let fnSymbolTable = Dictionary<string, SymbolType>(symbolTable)
                Seq.zip TokenUtils.asciiVariableSequence inputs |> Seq.iter (fun (k, v) -> fnSymbolTable[k] <- v)
                ParseAndEval(fnTokens, fnSymbolTable, fnContainer) |> Seq.exactlyOne

            let callable = SymbolType.PlotScriptFunction (fnCallback, fnTokens)

            symbolTable[name] <- callable
            (remaining, callable)
        // variable assignment
        | TokenType.Identifier name :: TokenType.Eq :: tail ->
            if name = "f" then
                raise (VariableError("Cannot assign to function name", name))

            let (remaining, result) = Expr tail
            if not (isAssignableSymbolType result) then
                raise (VariableError("Result cannot be assigned to a variable", name))

            symbolTable[name] <- result
            (remaining, result)
        | _ -> raise (ParserError "Parser error")
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
                let (remaining, result) = Expr tList

                yield result
                yield! Root remaining
        }

    Root tList
