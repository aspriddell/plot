namespace Plot.Core

open System
open System.Collections.Generic
open System.Reflection
open Plot.Core.Symbols

[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type public PlotScriptFunctionAttribute(identifier: string) =
    inherit Attribute()

    /// <summary>
    /// The identifier of the function.
    /// Used to call the function from a script.
    /// </summary>
    member this.Identifier = identifier

type public PlotScriptFunctionContainer() =
    let functions = Dictionary<string, SymbolType list -> SymbolType>(StringComparer.OrdinalIgnoreCase)
    let processAssembly (assembly: Assembly): unit =
        for method in assembly.GetTypes() |> Seq.collect (_.GetMethods()) do
            let attributes = method.GetCustomAttributes(typeof<PlotScriptFunctionAttribute>, false)
            for attribute in attributes do
                let functionAttribute = attribute :?> PlotScriptFunctionAttribute
                functions.Add(functionAttribute.Identifier, fun args -> method.Invoke(null, [|args|]) :?> SymbolType)

    do
        processAssembly typeof<PlotScriptFunctionContainer>.Assembly

    /// <summary>
    /// The default instance of the function container.
    /// </summary>
    static member Default = PlotScriptFunctionContainer()

    member this.FunctionTable: IReadOnlyDictionary<string, SymbolType list -> SymbolType> = functions
    member this.RegisterAssembly (assembly: Assembly): unit = processAssembly assembly
