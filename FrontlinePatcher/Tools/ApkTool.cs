using FrontlinePatcher.Processes;
using Spectre.Console;

namespace FrontlinePatcher.Tools;

public class ApkTool
{
    private const string Name = "./apktool";

    public static async Task<bool> DecompileAsync(string apkPath, string outputDir)
    {
        if (!File.Exists(apkPath))
        {
            AnsiConsole.MarkupLine($"[red]APK file not found \"{apkPath}\"![/]");
            return false;
        }

        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
        
        AnsiConsole.WriteLine($"Decompiling APK \"{apkPath}\" to \"{outputDir}\"...");
        
        var result = await ProcessExecutor.RunAsync(Name, $"d \"{apkPath}\" -o \"{outputDir}\" -f");
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Failed to decompile APK \"{apkPath}\"![/]");
            return false;
        }
        
        AnsiConsole.MarkupLine($"[green]Decompiled APK to \"{outputDir}\"![/]");
        return true;
    }

    public static async Task<bool> BuildApkAsync(string sourceDirectory, string outputApkPath)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            AnsiConsole.MarkupLine($"[red]Source directory not found \"{sourceDirectory}\"![/]");
            return false;
        }
        
        AnsiConsole.WriteLine($"Building APK from \"{sourceDirectory}\" to \"{outputApkPath}\"...");
        
        var result = await ProcessExecutor.RunAsync(Name, $"b \"{sourceDirectory}\" -o \"{outputApkPath}\"");
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Failed to build APK \"{outputApkPath}\"![/]");
            return false;
        }
        
        AnsiConsole.MarkupLine($"[green]Built APK to \"{outputApkPath}\"![/]");
        return true;
    }
}