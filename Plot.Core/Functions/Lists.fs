module Plot.Core.Functions.Lists

open Plot.Core
open Plot.Core.Symbols

[<PlotScriptFunction("append")>]
[<PlotScriptFunction("push")>]
let rec public listAppend (x: SymbolType list) : SymbolType =
    match x with
    | SymbolType.List list :: tail -> SymbolType.List (list @ tail)
    | _ -> invalidArg "*" "expected a list followed by a series of elements"

[<PlotScriptFunction("prepend")>]
let rec public listPrepend (x: SymbolType list) : SymbolType =
    match x with
    | SymbolType.List list :: tail -> SymbolType.List (tail @ list)
    | _ -> invalidArg "*" "expected a list followed by a series of elements"

[<PlotScriptFunction("concat")>]
let rec public listConcat (x: SymbolType list) : SymbolType =
    match x with
    | SymbolType.List l1 :: SymbolType.List l2 :: tail -> listConcat (SymbolType.List (l1 @ l2) :: tail)
    | [SymbolType.List l1] -> SymbolType.List l1
    | _ -> invalidArg "*" "expected two list arguments"

[<PlotScriptFunction("head")>]
[<PlotScriptFunction("first")>]
let public listHead (x: SymbolType list) : SymbolType =
    match x with
    | [SymbolType.List list] ->
        match List.tryHead list with
        | Some e -> e
        | None -> invalidArg "*" "expected expects a non-empty list argument"
    | _ -> invalidArg "*" "expected a list argument"

[<PlotScriptFunction("tail")>]
[<PlotScriptFunction("last")>]
let public listTail (x: SymbolType list) : SymbolType =
    match x with
    | [SymbolType.List list] ->
        match List.tryLast list with
        | Some e -> e
        | None -> invalidArg "*" "expected expects a non-empty list argument"
    | _ -> invalidArg "*" "expected a list argument"

[<PlotScriptFunction("len")>]
[<PlotScriptFunction("length")>]
let public listLength (x: SymbolType list) : SymbolType =
    match x with
    | [SymbolType.List list] -> Int (List.length list)
    | _ -> invalidArg "*" "expected a list argument"

[<PlotScriptFunction("insert")>]
let public listInsert (x: SymbolType list) : SymbolType =
    match x with
    | [SymbolType.List list; Int index; element] ->
        let index = int index
        if index < 0 || index > List.length list then
            invalidArg "*" "index out of range"
        SymbolType.List (List.insertAt index element list)
    | _ -> invalidArg "*" "expected a list, an index, and an element to insert"
    
[<PlotScriptFunction("map")>]
[<PlotScriptFunction("project")>]
let public listProject (x: SymbolType list) : SymbolType =
    match x with
    | [SymbolType.List list; SymbolType.PlotScriptFunction projection] ->
        list |> List.map (fun i -> (fst projection) [i]) |> SymbolType.List
    | _ -> invalidArg "*" "expected a list and an index"
