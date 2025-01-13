using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Plot.Core;

namespace Plot;

public static class PluginLoader
{
    private const string PluginFileTemplate = "*.psplugin.dll";

    /// <summary>
    /// Loads plugins into the target <see cref="PlotScriptFunctionContainer"/>, using the provided <see cref="AssemblyLoadContext"/> for isolation.
    /// </summary>
    public static void LoadPlugins(AssemblyLoadContext context, PlotScriptFunctionContainer target)
    {
#if !DEBUG
        // app bundles, sandboxing and perms don't work well with plugin loading
        if (OperatingSystem.IsMacOS())
        {
            return;
        }
#endif

        string[] paths =
        [
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PlotPlugins"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlotPlugins")
        ];

        foreach (var pluginFile in paths.Where(Directory.Exists).SelectMany(x =>
                     Directory.EnumerateFiles(x, PluginFileTemplate, SearchOption.AllDirectories)))
        {
            try
            {
                // try to load plugin (allows dependencies to be loaded in the same context without causing conflicts with the main program)
                target.RegisterAssembly(context.LoadFromAssemblyPath(pluginFile));
            }
            catch
            {
                // not a lot that can be done...
            }
        }
    }
}