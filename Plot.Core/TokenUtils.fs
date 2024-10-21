module Plot.Core.TokenUtils

open System

/// <summary>
/// Splits a list of <see cref="TokenType"/> into statements separated by commas, leaving nested statements intact
/// </summary>
/// <param name="argList">The sequence of tokens to perform splitting on</param>
let public splitTokenArguments argList =
    // i.e. a list [NumI(100), Plus, Identifier(x), Comma, NumI(200), Comma, Identifier(a)]
    // would be split into [[NumI(100), Plus, Identifier(x)], [NumI(200)], [Identifier(a)]]
    let rec splitArgsInternal acc currentList = function
        | [] -> acc @ [currentList]
        | TokenType.Comma :: tail -> splitArgsInternal (acc @ [currentList]) [] tail
        | x :: tail -> splitArgsInternal acc (currentList @ [x]) tail
    
    match argList with
    | TokenType.LPar :: tail when List.last tail = TokenType.RPar ->
        splitArgsInternal [] [] (tail |> List.take (List.length tail - 1)) // perform padding removal
    | _ -> raise (Exception "LPar/RPar padding not present")

/// <summary>
/// Extracts tokens immediately involved in a function call, returing the call tokens and the remaining items.
/// The list must start with an LPar 
/// </summary>
let public extractFnCallTokens tokenList: TokenType list * TokenType list =
    // as tokens are collected in reverse, once we reach the end of the (potentially nested) call the list needs to be reversed
    // calls need to track the current list and the depth (unclosed LPar count) to ensure nested calls aren't cut

    // function currying + function keyword can simplify the whole match... stuff
    // based on https://en.wikibooks.org/wiki/F_Sharp_Programming/Higher_Order_Functions#A_Timer_Function
    let rec extractFnCall acc depth = function
        | [] -> (List.rev acc, [])
        | TokenType.LPar :: tail -> extractFnCall (TokenType.LPar :: acc) (depth + 1) tail
        | TokenType.RPar :: tail when depth = 1 -> (List.rev (TokenType.RPar :: acc), tail)
        | TokenType.RPar :: tail -> extractFnCall (TokenType.RPar :: acc) (depth - 1) tail
        | token :: tail -> extractFnCall (token :: acc) depth tail

    match tokenList with
    | TokenType.LPar :: TokenType.RPar :: tail -> ([], tail) // handle parameterless calls like pi()
    | _ -> extractFnCall [] 0 tokenList                      // normal calls like sin(1)