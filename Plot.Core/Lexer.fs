module public Plot.Core.Lexer

open System
open Plot.Core

/// <summary>
/// Takes a sequence of chars and returns the parsed number from the start of the string.
/// </summary>
/// <returns>The parsed value and the unconsumed string.</returns>
/// <remarks>This is based on the example, with adjustments to handle floating point</remarks>
let rec private scanNumber (inputStr, inputValue, isFloating, divisor) =
    match inputStr with
    | c :: tail when Char.IsDigit c ->
        let digit = Char.GetNumericValue c

        if isFloating then
            // scanning left-to-right results in the divisor growing by a factor of 10 each time
            scanNumber (tail, inputValue + digit / divisor, isFloating, divisor * 10.0)
        else
            // reuse the existing integer logic
            scanNumber (tail, inputValue * 10.0 + digit, isFloating, 1.0)

    // after detecting the decimal point, switch to floating point mode
    | '.' :: tail when not isFloating -> scanNumber (tail, inputValue, true, 10.0)

    // don't allow multiple decimal points
    | '.' :: _ -> failwith "Unexpected decimal point"

    // done
    | _ -> (inputStr, inputValue, isFloating)

// Scan variables, a string of characters starting with a letter
let rec private scanVar (inputStr, currentVar) =
    match inputStr with
    | c :: tail when Char.IsLetterOrDigit c -> scanVar (tail, currentVar + string c)
    | _ -> (inputStr, currentVar)

/// <summary>
/// Performs lexical analysis on the input string, returning a list of tokens.
/// </summary>
let rec private scan input =
    match input with
    | [] -> []
    | '\n' :: tail -> TokenType.NewLine :: scan tail
    | '+' :: tail -> TokenType.Add :: scan tail
    | '-' :: tail -> TokenType.Sub :: scan tail
    | '*' :: tail -> TokenType.Mul :: scan tail
    | '/' :: tail -> TokenType.Div :: scan tail
    | '%' :: tail -> TokenType.Mod :: scan tail
    | '^' :: tail -> TokenType.Pow :: scan tail
    | '(' :: tail -> TokenType.LPar :: scan tail
    | ')' :: tail -> TokenType.RPar :: scan tail
    | '=' :: tail -> TokenType.Eq :: scan tail
    | c :: tail when Char.IsWhiteSpace c -> scan tail

    // if it's a digit, determine if it's an integer or floating point value.
    | c :: tail when Char.IsDigit c ->
        let (outStr, outVal, isFloating) =
            scanNumber (tail, Char.GetNumericValue c, false, 1.0)

        if isFloating then
            NumF outVal :: scan outStr
        else
            NumI(int outVal) :: scan outStr

    // variables start with a letter
    | c :: tail when Char.IsLetter c ->
        let (remaining, varName) = scanVar (tail, string c)
        TokenType.Var varName :: scan remaining

    | _ -> failwith "Lexer error"

let public Parse (input: string): TokenType list = scan (input |> List.ofSeq)
