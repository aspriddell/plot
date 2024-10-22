namespace Plot.Core

open System
open System.Collections.Generic
open System.Reflection
open Plot.Core.Symbols

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

type public PlotScriptFunctionContainer() =
    let functions = Dictionary<string, SymbolType list * IDictionary<string, SymbolType> -> SymbolType>(StringComparer.OrdinalIgnoreCase)
    let processAssembly (assembly: Assembly): unit =
        for method in assembly.GetTypes() |> Seq.collect (_.GetMethods()) |> Seq.filter (_.IsStatic) do
            let attributes = method.GetCustomAttributes(typeof<PlotScriptFunctionAttribute>, false)
            for attribute in attributes do
                let functionAttribute = attribute :?> PlotScriptFunctionAttribute
                let fnCall = if functionAttribute.InjectSymbolTable then
                                fun (args, symTable) -> method.Invoke(null, [|args; symTable|]) :?> SymbolType
                             else
                                fun (args, _) -> method.Invoke(null, [|args|]) :?> SymbolType

                functions.Add(functionAttribute.Identifier, fnCall)

    do
        processAssembly typeof<PlotScriptFunctionContainer>.Assembly

    /// <summary>
    /// The default instance of the function container.
    /// </summary>
    static member Default = PlotScriptFunctionContainer()

    member this.FunctionTable: IReadOnlyDictionary<string, SymbolType list * IDictionary<string, SymbolType> -> SymbolType> = functions

    member this.HasFunction (identifier: string): bool = functions.ContainsKey(identifier)
    member this.RegisterAssembly (assembly: Assembly): unit = processAssembly assembly
