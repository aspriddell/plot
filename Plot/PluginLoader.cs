using System;
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
        string[] paths =
        [
            GetPlatformSpecificRootDir(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PlotPlugins"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlotPlugins")
        ];

        foreach (var pluginFile in paths.Where(Directory.Exists).SelectMany(x => Directory.EnumerateFiles(x, PluginFileTemplate, SearchOption.AllDirectories)))
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

    private static string GetPlatformSpecificRootDir()
    {
#if !DEBUG
        // macOS uses app bundles, so are three levels up from the executable
        if (OperatingSystem.IsMacOS())
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        }
#endif
        
        return AppContext.BaseDirectory;
    }
}