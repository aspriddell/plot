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
// <Term>           ::= <Power> <TermOpt>
// <TermOpt>        ::= "*" <Power> <TermOpt> | "/" <Power> <TermOpt> | "%" <Power> <TermOpt> | <empty>
// <Power>         ::= <Base> <PowerOpt>
// <PowerOpt>      ::= "^" <Power> | <empty>
// <Base>           ::= "-" <Base> | <Number> | <Identifier> | "(" <Expr> ")" | <FnCall> | <Array> | <ArrayIndex>
// <Number>         ::= "NumI" <value> | "NumF" <value>

// <FnCall>         ::= <Identifier> "(" <Arguments> ")"
// <Arguments>      ::= <Expr> ("," <Expr>)* | <Array> ("," <Expr>)* | <empty>

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
    and Term tList = (Power >> TermOpt) tList
    and TermOpt (tList, value) =
        match tList with
        | TokenType.Mul :: tail -> let (tLst, tVal) = Power tail
                                   TermOpt (tLst, mulValues(value, tVal))
        | TokenType.Div :: tail -> let (tLst, tVal) = Power tail
                                   TermOpt (tLst, divValues(value, tVal))
        | TokenType.Mod :: tail -> let (tLst, tVal) = Power tail
                                   TermOpt (tLst, modValues(value, tVal))
        | _ -> (tList, value)
    and Power tList = (Base >> PowerOpt) tList
    and PowerOpt (tList, value) =
        match tList with
        | TokenType.Pow :: tail -> let (tLst, tVal) = Base tail
                                   PowerOpt (tLst, powValues(value, tVal))
        | _ -> (tList, value)
    and Base tList =
        match tList with
        | TokenType.NumI value :: tail -> (tail, SymbolType.Int value)
        | TokenType.NumF value :: tail -> (tail, SymbolType.Float value)

        // unary number handling
        | TokenType.Sub :: tail -> let (tLst, tVal) = Base tail
                                   (tLst, negateValue tVal)

        | TokenType.Identifier name :: Eq :: _ -> raise (VariableError("Assignment failed", name))  // invalid assignment
        | [TokenType.Identifier name] when symbolTable.ContainsKey(name) -> ([], symbolTable[name]) // single value return
        | TokenType.Identifier name :: TokenType.LInd :: tail when symbolTable.ContainsKey(name) -> // array index access
            ArrayIndex tail symbolTable[name]

        | TokenType.Identifier name :: tail when symbolTable.ContainsKey(name) -> // symbol lookup
            match symbolTable[name] with
            // check the function is being invoked before invoking it (check for next token being LPar)
            | SymbolType.PlotScriptFunction fn when List.tryHead tail = Some(TokenType.LPar) -> FnCall (fun (f, _) -> fn.Function(f)) tail
            | SymbolType.PlotScriptPolynomialFunction fn when List.tryHead tail = Some(TokenType.LPar) -> FnCall (fun (f, _) -> fn.Function(f)) tail
            | _ -> (tail, symbolTable[name])
        | TokenType.Identifier name :: tail when fnContainer.HasFunction(name) -> // function lookup (builtin)
            // same check as above, make sure it's being invoked otherwise return it as a plotscript function (monkey patchable)
            if List.tryHead tail = Some(TokenType.LPar) then
                FnCall fnContainer.FunctionTable[name] tail
            else
                (tail, SymbolType.PlotScriptFunction { Function = (fun fnArg -> fnContainer.FunctionTable[name](fnArg, null)); Tokens = None })
        | TokenType.Identifier name :: _ -> raise (VariableError("Variable not found", name))

        | TokenType.LInd :: tail -> Array tail []
        | TokenType.LPar :: tail -> let (tLst, tVal) = Expr tail
                                    match tLst with
                                    | TokenType.RPar :: tail -> (tail, tVal)
                                    | _ -> raise (ParserError "One or more set of parentheses were not closed.")

        | _ -> raise (ParserError "Parser error")
    and Array tList acc =
        match tList with
        | TokenType.RInd :: tail -> (tail, SymbolType.List (List.rev acc))
        | _ -> let (remaining, result) = Expr tList
               match remaining with
               | TokenType.Comma :: tail -> Array tail (result :: acc)
               | TokenType.RInd :: tail -> (tail, SymbolType.List (List.rev (result :: acc)))
               | _ -> raise (ParserError "Expected comma or closing bracket")
    and ArrayIndex tList current =
        let (remaining, index) = Expr tList
        match remaining, index with
        | TokenType.RInd :: tail, SymbolType.Int idx // single index access
        | TokenType.RInd :: tail, SymbolType.List [SymbolType.Int idx] -> // secondary index access (handles [2] in a[1][2]) as Expr produces a[1] and an array [2])
            match current with
            | SymbolType.List arr when idx >= 0 && idx < arr.Length ->
                if List.tryHead tail = Some(TokenType.LInd) then // if next token starts another index, go again...
                    ArrayIndex tail arr[idx]
                else // return the value
                    (tail, arr[idx])
            | SymbolType.List _ -> raise (ParserError "Index out of bounds")
            | _ -> raise (ParserError "Cannot use indexer on non-list type")
        | TokenType.LInd :: tail, SymbolType.List [SymbolType.Int idx] -> // final index handling in multiple sequential accesses (handles [3] in a[1][2][3])
            match current with
            | SymbolType.List arr when idx >= 0 && idx < arr.Length -> ArrayIndex tail arr[idx]  // recursive check
            | SymbolType.List _ -> raise (ParserError "Index out of bounds")
            | _ -> raise (ParserError "Cannot use indexer on non-list type")
        | _, SymbolType.List [SymbolType.Int idx] -> // handle base case (newline/identifier starting another statement, etc.)
            match current with
            | SymbolType.List arr when idx >= 0 && idx < arr.Length -> (remaining, arr[idx]) // final index
            | SymbolType.List _ -> raise (ParserError "Index out of bounds")
            | _ -> raise (ParserError "Cannot use indexer on non-list type")
        | _ -> raise (ParserError "Index parsing error")
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

            let callable = SymbolType.PlotScriptFunction { Function = fnCallback; Tokens = Some(fnTokens) }

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
