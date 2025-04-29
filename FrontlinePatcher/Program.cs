using FrontlinePatcher.Patch;
using FrontlinePatcher.Patch.Patches;
using FrontlinePatcher.Tools;

Console.WriteLine("TITANFALL: FRONTLINE PATCHER");

var inputApk = "com.nexonm.tffl.apk";
var outputApk = "com.nexonm.tffl_patched.apk";
var keystorePath = "my-release-key.keystore";
var keystorePassword = "123456";

var tempDir = "temp";
if (Directory.Exists(tempDir))
{
    Directory.Delete(tempDir, true);
}

Directory.CreateDirectory(tempDir);

Console.WriteLine("--- Decompile APK ---");

var decompiledApkDir = Path.Combine(tempDir, "decompiled_apk");
if (!await ApkTool.DecompileAsync(inputApk, decompiledApkDir))
{
    return;
}

Console.WriteLine("--- Patch Assemblies ---");

var managedPath = Path.Combine(decompiledApkDir, "assets", "bin", "Data", "Managed");
var assemblyPath = Path.Combine(managedPath, "Assembly-CSharp.dll");
var patchedAssemblyPath = Path.Combine(tempDir, "Assembly-CSharp.dll");
if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly not found at: {assemblyPath}");
    return;
}

var patcher = new AssemblyPatcher(assemblyPath);
patcher.AddPatch(new GameDebugLogPatch());
patcher.AddPatch(new StorePurchasePatch());

patcher.LoadAssembly();

if (patcher.ApplyPatches())
{
    Console.WriteLine("All patches applied successfully!");
    patcher.SaveAssembly(patchedAssemblyPath);
}
else
{
    Console.Error.WriteLine("Failed to patch!");
    return;
}

Console.WriteLine("--- Replace Assembly ---");

File.Delete(assemblyPath);
File.Move(patchedAssemblyPath, assemblyPath);

Console.WriteLine("--- Recompile APK ---");

if (!await ApkTool.BuildApkAsync(decompiledApkDir, outputApk))
{
    return;
}

Console.WriteLine("--- Sign APK ---");

if (!await ApkSigner.SignApkAsync(outputApk, keystorePath, keystorePassword, outputApk))
{
    return;
}

Console.WriteLine("--- Clean Up ---");

Directory.Delete(tempDir, true);

Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine($"Patched APK saved to \"{outputApk}\"!");