using dnlib.DotNet;
using dnlib.DotNet.Writer;

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
            Console.Error.WriteLine("Assembly has not been loaded!");
            return false;
        }
        
        foreach (var patch in _patches)
        {
            Console.WriteLine($"Applying patch: {patch.Name}");

            try
            {
                if (patch.Apply(_module))
                {
                    Console.WriteLine("  Patch applied successfully.");
                }
                else
                {
                    Console.Error.WriteLine("  Failed to apply patch!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("  Error applying patch!");
                Console.Error.WriteLine(ex);
                return false;
            }
        }

        return true;
    }

    public void LoadAssembly()
    {
        Console.WriteLine("Loading assembly...");

        var moduleContext = ModuleDef.CreateModuleContext();
        var resolver = (AssemblyResolver) moduleContext.AssemblyResolver;
        resolver.PreSearchPaths.Add(Path.GetDirectoryName(_assemblyPath));
        
        _module = ModuleDefMD.Load(_assemblyPath, moduleContext);
        
        Console.WriteLine($"Loaded assembly: {_module.Name}");
    }

    public void SaveAssembly(string outputPath)
    {
        if (_module is null)
        {
            throw new InvalidOperationException("Assembly has not been loaded.");
        }
        
        Console.WriteLine($"Saving assembly to: {outputPath}");

        var writerOptions = new ModuleWriterOptions(_module);
        _module.Write(outputPath, writerOptions);
    }
}