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
        var decompileSuccess = await AnsiConsole.Status().StartAsync("Decompiling APK...", async _ =>
            await ApkTool.DecompileAsync(settings.InputApk, decompiledApkDir));
        if (!decompileSuccess)
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

        var patchResult = AnsiConsole.Status().Start("Loading assembly...", ctx =>
        {
            patcher.LoadAssembly();
            ctx.Status("Applying patches...");
            
            if (patcher.ApplyPatches())
            {
                AnsiConsole.MarkupLine("[green]All patches applied successfully![/]");
                patcher.SaveAssembly(patchedAssemblyPath);
                return 0;
            }

            AnsiConsole.MarkupLine("[red]Failed to apply patches![/]");
            return 1;
        });

        if (patchResult != 0)
        {
            return patchResult;
        }

        File.Delete(assemblyPath);
        File.Move(patchedAssemblyPath, assemblyPath);
        AnsiConsole.WriteLine("Replaced original assembly with patched assembly.");

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("--- Recompile APK ---");

        var recompileSuccess = await AnsiConsole.Status().StartAsync("Recompiling APK...", async _ =>
            await ApkTool.BuildApkAsync(decompiledApkDir, settings.OutputApk));
        if (!recompileSuccess)
        {
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("--- Sign APK ---");

        var signingSuccess = await AnsiConsole.Status().StartAsync("Signing APK...", async _ =>
            await ApkSigner.SignApkAsync(settings.OutputApk, settings.KeystorePath,
                settings.KeystorePassword, settings.OutputApk));
        if (!signingSuccess)
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
        AnsiConsole.MarkupLine($"[green]Patched APK saved to \"{settings.OutputApk}\"![/]");
        
        return 0;
    }
}