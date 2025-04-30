using System.ComponentModel;
using FrontlinePatcher.Patch;
using FrontlinePatcher.Patch.Patches;
using FrontlinePatcher.Tools;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FrontlinePatcher;

public class PatchCommand : AsyncCommand<PatchCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Input APK file to patch.")]
        [CommandArgument(0, "<APK>")]
        public required string InputApk { get; init; }
        
        [Description("Output APK file after patching.")]
        [CommandArgument(1, "<Output APK>")]
        public required string OutputApk { get; init; }
        
        [Description("Path to the keystore for signing the APK.")]
        [CommandArgument(2, "<Keystore Path>")]
        public required string KeystorePath { get; init; }
        
        [Description("Password for the keystore.")]
        [CommandArgument(3, "<Keystore Password>")]
        public required string KeystorePassword { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.WriteLine("TITANFALL: FRONTLINE PATCHER");
        AnsiConsole.WriteLine();

        const string tempDir = "temp";
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }

        Directory.CreateDirectory(tempDir);

        AnsiConsole.WriteLine("--- Decompile APK ---");

        var decompiledApkDir = Path.Combine(tempDir, "decompiled_apk");
        if (!await ApkTool.DecompileAsync(settings.InputApk, decompiledApkDir))
        {
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("--- Patch Assemblies ---");

        var managedPath = Path.Combine(decompiledApkDir, "assets", "bin", "Data", "Managed");
        var assemblyPath = Path.Combine(managedPath, "Assembly-CSharp.dll");
        var patchedAssemblyPath = Path.Combine(tempDir, "Assembly-CSharp.dll");
        if (!File.Exists(assemblyPath))
        {
            AnsiConsole.MarkupLine($"[red]Assembly not found at \"{assemblyPath}\"[/]");
            return 1;
        }

        var patcher = new AssemblyPatcher(assemblyPath);
        patcher.AddPatch(new GameDebugLogPatch());
        patcher.AddPatch(new StorePurchasePatch());

        patcher.LoadAssembly();

        if (patcher.ApplyPatches())
        {
            AnsiConsole.WriteLine("All patches applied successfully!");
            patcher.SaveAssembly(patchedAssemblyPath);
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Failed to apply patches![/]");
            return 1;
        }

        File.Delete(assemblyPath);
        File.Move(patchedAssemblyPath, assemblyPath);
        AnsiConsole.WriteLine("Replaced original assembly with patched assembly.");

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("--- Recompile APK ---");

        if (!await ApkTool.BuildApkAsync(decompiledApkDir, settings.OutputApk))
        {
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("--- Sign APK ---");

        if (!await ApkSigner.SignApkAsync(settings.OutputApk, settings.KeystorePath, settings.KeystorePassword, settings.OutputApk))
        {
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("--- Clean Up ---");

        Directory.Delete(tempDir, true);
        AnsiConsole.WriteLine("Temporary files deleted.");

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine($"Patched APK saved to \"{settings.OutputApk}\"!");
        
        return 0;
    }
}