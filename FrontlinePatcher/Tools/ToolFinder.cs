using System.Runtime.InteropServices;
using Spectre.Console;

namespace FrontlinePatcher.Tools;

public class ToolFinder
{
    private static readonly Dictionary<string, List<string>> SearchPatterns = new();

    private readonly Dictionary<string, string> _foundTools = new();

    static ToolFinder()
    {
        var keyToolExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "keytool.exe" : "keytool";
        var apkSignerExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "apksigner.bat" : "apksigner";
        var apkToolExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "apktool.bat" : "apktool";

        SearchPatterns["keytool"] = [
            "./" + keyToolExe,
            "%JAVA_HOME%/bin/" + keyToolExe
        ];

        SearchPatterns["apksigner"] = [
            "./" + apkSignerExe,
            "%ANDROID_HOME%/build-tools/*/" + apkSignerExe
        ];

        SearchPatterns["apktool"] = [
            "./" + apkToolExe
        ];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SearchPatterns["keytool"].Add("/usr/bin/" + keyToolExe);
            SearchPatterns["apksigner"].Add("%HOME%/Android/Sdk/build-tools/*/" + apkSignerExe);
            SearchPatterns["apktool"].Add("/usr/local/bin/" + apkToolExe);
            SearchPatterns["apktool"].Add("%HOME%/.local/bin/" + apkToolExe);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SearchPatterns["keytool"].Add("%ProgramFiles%/Java/*/bin/" + keyToolExe);
            SearchPatterns["apksigner"].Add("%LOCALAPPDATA%/Android/Sdk/build-tools/*/" + apkSignerExe);
        }
    }

    public void FindTools()
    {
        foreach (var toolName in SearchPatterns.Keys)
        {
            AnsiConsole.MarkupLine($"Searching for \"{toolName}\"...");
            var path = FindTool(toolName);
            if (!string.IsNullOrEmpty(path))
            {
                _foundTools[toolName] = path;
                AnsiConsole.MarkupLine($"[green]  > Found at: \"{path}\"[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]  > \"{toolName}\" not found.[/]");
            }
        }
    }
    
    public string? GetToolPath(string toolName)
    {
        return _foundTools.GetValueOrDefault(toolName);
    }

    private static string? FindTool(string toolName)
    {
        if (!SearchPatterns.TryGetValue(toolName, out var patterns))
        {
            return null;
        }

        foreach (var pattern in patterns)
        {
            var resolvedPath = ResolvePath(pattern);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                return resolvedPath;
            }
        }

        return null;
    }

    private static string? ResolvePath(string pattern)
    {
        var expandedPattern = Environment.ExpandEnvironmentVariables(pattern);
        if (!expandedPattern.Contains('*'))
        {
            return File.Exists(expandedPattern) ? expandedPattern : null;
        }

        try
        {
            var directory = Path.GetDirectoryName(expandedPattern)!;
            var searchPattern = Path.GetFileName(expandedPattern);

            var wildcardIndex = directory.IndexOf('*');
            if (wildcardIndex == -1)
            {
                return null;
            }

            var searchRoot = directory[..wildcardIndex];
            var remainingDirPart = directory[wildcardIndex..].Replace("*", "");

            if (!Directory.Exists(searchRoot))
            {
                return null;
            }

            var foundFiles = Directory
                .EnumerateFiles(searchRoot, searchPattern, SearchOption.AllDirectories)
                .Where(f => f.Contains(remainingDirPart));

            return foundFiles
                .OrderByDescending(f => f)
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}