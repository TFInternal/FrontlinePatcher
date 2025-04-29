using FrontlinePatcher.Processes;

namespace FrontlinePatcher.Tools;

public class ApkTool
{
    private const string Name = "./apktool";

    public static async Task<bool> DecompileAsync(string apkPath, string outputDir)
    {
        if (!File.Exists(apkPath))
        {
            await Console.Error.WriteLineAsync($"APK file not found: {apkPath}");
            return false;
        }

        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
        
        Console.WriteLine($"Decompiling APK \"{apkPath}\" to \"{outputDir}\"");
        
        var result = await ProcessExecutor.RunAsync(Name, $"d \"{apkPath}\" -o \"{outputDir}\" -f");
        if (!result.IsSuccess)
        {
            await Console.Error.WriteLineAsync($"Failed to decompile APK: {apkPath}");
            return false;
        }
        
        Console.WriteLine($"Decompiled APK to: {outputDir}");
        return true;
    }

    public static async Task<bool> BuildApkAsync(string sourceDirectory, string outputApkPath)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            await Console.Error.WriteLineAsync($"Source directory not found: {sourceDirectory}");
            return false;
        }
        
        Console.WriteLine($"Building APK from \"{sourceDirectory}\" to \"{outputApkPath}\"");
        
        var result = await ProcessExecutor.RunAsync(Name, $"b \"{sourceDirectory}\" -o \"{outputApkPath}\"");
        if (!result.IsSuccess)
        {
            await Console.Error.WriteLineAsync($"Failed to build APK: {outputApkPath}");
            return false;
        }
        
        Console.WriteLine($"Built APK to: {outputApkPath}");
        return true;
    }
}