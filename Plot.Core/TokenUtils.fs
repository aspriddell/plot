module Plot.Core.TokenUtils

/// <summary>
/// Splits a list of <see cref="TokenType"/> into statements separated by commas, leaving nested statements intact
/// </summary>
/// <param name="argList">The sequence of tokens to perform splitting on</param>
let public splitTokenArguments argList =
    // i.e. a list [NumI(100), Plus, Identifier(x), Comma, NumI(200), Comma, Identifier(a)]
    // would be split into [[NumI(100), Plus, Identifier(x)], [NumI(200)], [Identifier(a)]]
    let rec splitArgsInternal acc currentList = function
        | [] -> acc @ [currentList]
        | TokenType.Comma :: rest -> splitArgsInternal (acc @ [currentList]) [] rest
        | x :: rest -> splitArgsInternal acc (currentList @ [x]) rest

    splitArgsInternal [] [] argList
