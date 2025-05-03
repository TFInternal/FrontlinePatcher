using dnlib.DotNet;
using Spectre.Console;

namespace FrontlinePatcher.Patch;

/// <summary>
/// Represents a patch that can be applied to a module.
/// </summary>
public abstract class Patch
{
    /// <summary>
    /// Gets the name of the patch.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Applies the patch to the specified module.
    /// </summary>
    /// <param name="module">The module to apply the patch to.</param>
    /// <returns>True if the patch was applied successfully; otherwise, false.</returns>
    public abstract bool Apply(ModuleDefMD module);
    
    /// <summary>
    /// Finds a type in the UnityEngine assembly.
    /// </summary>
    /// <param name="module">The module to search in.</param>
    /// <param name="typeName">The name of the type to find.</param>
    /// <returns>The found type definition, or null if not found.</returns>
    protected TypeDef? FindUnityEngineType(ModuleDefMD module, string typeName)
    {
        var unityRef = module.GetAssemblyRefs().FirstOrDefault(a => a.Name == "UnityEngine");
        if (unityRef is null)
        {
            AnsiConsole.MarkupLine("[red]  UnityEngine assembly reference not found![/]");
            return null;
        }

        var unityAssembly = module.Context.AssemblyResolver.Resolve(unityRef, module);
        if (unityAssembly is null)
        {
            AnsiConsole.MarkupLine("[red]  UnityEngine assembly not found![/]");
            return null;
        }

        var type = unityAssembly.Find(typeName, true);
        if (type is not null)
        {
            return type;
        }
        
        AnsiConsole.MarkupLine($"[red]  {typeName} type not found in UnityEngine assembly![/]");
        return null;
    }
}