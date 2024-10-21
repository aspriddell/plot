using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.FSharp.Collections;
using Plot.Core;

namespace Plot.Models;

public class PlotScriptDocument
{
    /// <summary>
    /// Default file extension for plotscript documents
    /// </summary>
    internal const string DefaultFileExtension = ".plotscript";

    /// <summary>
    /// The encoding used to read/write plotscript document types.
    /// Currently set to UTF-8 without BOM.
    /// </summary>
    private static readonly Encoding DocumentEncoding = new UTF8Encoding(false);

    private IStorageFile _file;

    private string _sourceText;
    private FSharpList<TokenType> _cachedLexerOutput;

    private PlotScriptDocument(IStorageFile file)
    {
        _file = file;
    }

    /// <summary>
    /// The filename (without path).
    /// If no file is loaded, will return <c>null</c>
    /// </summary>
    public string FileName => _file?.Name;

    /// <summary>
    /// Gets whether the current <see cref="PlotScriptDocument"/> is backed by persistent storage
    /// </summary>
    public bool IsBackedByFile => _file != null;

    /// <summary>
    /// Gets the source text (script)
    /// </summary>
    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (_sourceText?.Equals(value) == true)
            {
                return;
            }
            
            _sourceText = value;
            _cachedLexerOutput = null;
        }
    }

    /// <summary>
    /// Executes the currently stored script, returning the output symbol sequence.
    /// </summary>
    public IEnumerable<Symbols.SymbolType> ExecuteScript(IDictionary<string, Symbols.SymbolType> symbolTable = null)
    {
        _cachedLexerOutput ??= Lexer.Parse(SourceText);
        symbolTable ??= new Dictionary<string, Symbols.SymbolType>();

        return Parser.ParseAndEval(_cachedLexerOutput, symbolTable, PlotScriptFunctionContainer.Default);
    }

    public async Task SaveDocument(IStorageFile saveAs = null)
    {
        _file = saveAs;
        
        if (_file == null)
        {
            throw new InvalidOperationException("Cannot save document without a file");
        }
        
        await using var stream = await _file.OpenWriteAsync();
        
        // write contents then dispose writer (leave stream open for truncation)
        await using (var writer = new StreamWriter(stream, DocumentEncoding, leaveOpen: true))
        {
            await writer.WriteAsync(SourceText);
        }

        stream.SetLength(stream.Position);
    }

    /// <summary>
    /// Creates a <see cref="PlotScriptDocument"/> from a file.
    /// </summary>
    public static async Task<PlotScriptDocument> FromFile(IStorageFile file)
    {
        using var reader = new StreamReader(await file.OpenReadAsync(), DocumentEncoding);
        var document = new PlotScriptDocument(file)
        {
            _sourceText = await reader.ReadToEndAsync()
        };

        return document;
    }
}