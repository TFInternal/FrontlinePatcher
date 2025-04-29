using FrontlinePatcher.Processes;

namespace FrontlinePatcher.Tools;

public class ApkSigner
{
    private const string Name = "/home/nuutti/Android/Sdk/build-tools/34.0.0/apksigner";

    public static async Task<bool> SignApkAsync(string apkPath, string keystorePath, string keystorePassword, string outputApkPath)
    {
        if (!File.Exists(apkPath))
        {
            await Console.Error.WriteLineAsync($"APK file not found: {apkPath}");
            return false;
        }

        if (!File.Exists(keystorePath))
        {
            await Console.Error.WriteLineAsync($"Keystore file not found: {keystorePath}");
            return false;
        }

        if (apkPath == outputApkPath)
        {
            Console.WriteLine($"Signing APK \"{apkPath}\" with keystore \"{keystorePath}\"");
        }
        else
        {
            Console.WriteLine($"Signing APK \"{apkPath}\" with keystore \"{keystorePath}\" to \"{outputApkPath}\"");
        }

        var result = await ProcessExecutor.RunAsync(Name, $"sign --ks \"{keystorePath}\" --ks-pass pass:{keystorePassword} --out \"{outputApkPath}\" \"{apkPath}\"");
        if (!result.IsSuccess)
        {
            await Console.Error.WriteLineAsync($"Failed to sign APK: {apkPath}");
            return false;
        }

        Console.WriteLine($"Signed APK to: {outputApkPath}");
        return true;
    }
}