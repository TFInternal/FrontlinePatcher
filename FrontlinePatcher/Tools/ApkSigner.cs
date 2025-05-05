using FrontlinePatcher.Processes;
using Spectre.Console;

namespace FrontlinePatcher.Tools;

public static class ApkSigner
{
    private const string KeyToolName = "keytool";
    
    private const string ApkSignerName = "/home/nuutti/Android/Sdk/build-tools/34.0.0/apksigner";

    public static async Task<bool> GenerateKeystore(string keystorePath, string keystorePassword)
    {
        const string distinguishedName = "CN=Unknown, OU=Unknown, O=Unknown, L=Unknown, ST=Unknown, C=FI";
        
        var result = await ProcessExecutor.RunAsync(KeyToolName, $"-genkey -keystore \"{keystorePath}\" -storepass \"{keystorePassword}\" -keyalg RSA -dname \"{distinguishedName}\" -validity 10000");
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Failed to generate keystore \"{keystorePath}\"![/]");
            return false;
        }
        
        AnsiConsole.MarkupLine($"[green]Generated keystore \"{keystorePath}\"![/]");
        return true;
    }

    public static async Task<bool> SignApkAsync(string apkPath, string keystorePath, string keystorePassword, string outputApkPath)
    {
        if (!File.Exists(apkPath))
        {
            AnsiConsole.MarkupLine($"[red]APK file not found \"{apkPath}\"![/]");
            return false;
        }

        if (!File.Exists(keystorePath))
        {
            AnsiConsole.MarkupLine($"[red]Keystore file not found \"{keystorePath}\"![/]");
            return false;
        }

        if (apkPath == outputApkPath)
        {
            AnsiConsole.WriteLine($"Signing APK \"{apkPath}\" with keystore \"{keystorePath}\"...");
        }
        else
        {
            AnsiConsole.WriteLine($"Signing APK \"{apkPath}\" with keystore \"{keystorePath}\" to \"{outputApkPath}\"...");
        }

        var result = await ProcessExecutor.RunAsync(ApkSignerName, $"sign --ks \"{keystorePath}\" --ks-pass pass:{keystorePassword} --out \"{outputApkPath}\" \"{apkPath}\"");
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Failed to sign APK \"{apkPath}\"![/]");
            return false;
        }

        AnsiConsole.MarkupLine($"[green]Signed APK to \"{outputApkPath}\"![/]");
        return true;
    }
}