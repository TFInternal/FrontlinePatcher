using FrontlinePatcher.Patch;

const string baseDir = "/home/nuutti/Downloads/Managed/";
var assemblyCSharpPath = Path.Combine(baseDir, "Assembly-CSharp.dll");

var outputDir = Path.Combine(baseDir, "Patched");
var outputPath = Path.Combine(outputDir, "Assembly-CSharp.dll");

Directory.CreateDirectory(outputDir);

var patcher = new AssemblyPatcher(assemblyCSharpPath);

patcher.LoadAssembly();

if (patcher.ApplyPatches())
{
    Console.WriteLine("All patches applied successfully!");
    patcher.SaveAssembly(outputPath);
    Console.WriteLine($"Patched assembly saved to: {outputPath}");
}
else
{
    Console.Error.WriteLine("Failed to patch!");
}