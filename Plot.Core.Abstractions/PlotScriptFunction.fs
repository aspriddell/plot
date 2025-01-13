module Plot.Core.Extensibility.Functions

open System

[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type public PlotScriptFunctionAttribute(identifier: string) =
    inherit Attribute()

    let mutable injectSymbolTable = false

    /// <summary>
    /// The identifier of the function.
    /// Used to call the function from a script.
    /// </summary>
    member this.Identifier = identifier

    member this.InjectSymbolTable
        with get() = injectSymbolTable
        and set(value) = injectSymbolTable <- value
