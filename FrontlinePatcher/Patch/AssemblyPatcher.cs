using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Spectre.Console;

namespace FrontlinePatcher.Patch;

public class AssemblyPatcher
{
    private readonly string _assemblyPath;
    
    private readonly List<Patch> _patches = [];

    private ModuleDefMD? _module;

    public AssemblyPatcher(string assemblyPath)
    {
        _assemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));

        if (!File.Exists(_assemblyPath))
        {
            throw new FileNotFoundException("The specified assembly file does not exist.", _assemblyPath);
        }
    }
    
    public void AddPatch(Patch patch)
    {
        _patches.Add(patch);
    }

    public bool ApplyPatches()
    {
        if (_module is null)
        {
            AnsiConsole.MarkupLine("[red]Assembly has not been loaded![/]");
            return false;
        }
        
        foreach (var patch in _patches)
        {
            AnsiConsole.MarkupLine($"[yellow]Applying patch \"{patch.Name}\"...[/]");

            try
            {
                if (patch.Apply(_module))
                {
                    AnsiConsole.MarkupLine("[green]  Patch applied successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]  Failed to apply patch![/]");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]  Error applying patch: {ex.Message}[/]");
                return false;
            }
        }

        return true;
    }

    public void LoadAssembly()
    {
        AnsiConsole.WriteLine("Loading assembly...");

        var moduleContext = ModuleDef.CreateModuleContext();
        var resolver = (AssemblyResolver) moduleContext.AssemblyResolver;
        resolver.PreSearchPaths.Add(Path.GetDirectoryName(_assemblyPath));
        
        _module = ModuleDefMD.Load(_assemblyPath, moduleContext);
        
        AnsiConsole.WriteLine($"Loaded assembly \"{_module.Name}\".");
    }

    public void SaveAssembly(string outputPath)
    {
        if (_module is null)
        {
            throw new InvalidOperationException("Assembly has not been loaded.");
        }
        
        AnsiConsole.WriteLine($"Saving assembly to \"{outputPath}\"...");

        var writerOptions = new ModuleWriterOptions(_module);
        _module.Write(outputPath, writerOptions);
    }
}