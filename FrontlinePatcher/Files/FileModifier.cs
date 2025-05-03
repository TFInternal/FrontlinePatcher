using Spectre.Console;

namespace FrontlinePatcher.Files;

public static class FileModifier
{
    public static bool ApplyModifications(string directory, List<FileModificationRule> rules)
    {
        foreach (var rule in rules)
        {
            var filePath = Path.Combine(directory, rule.FilePath);
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[red]File not found \"{filePath}\"![/]");
                return false;
            }
            
            var fileContent = File.ReadAllText(filePath);
            var modifiedContent = rule.SearchPattern.Replace(fileContent, rule.Replacement);
            if (fileContent == modifiedContent)
            {
                AnsiConsole.MarkupLine($"[yellow]No changes made to \"{filePath}\".[/]");
                continue;
            }
            
            File.WriteAllText(filePath, modifiedContent);
            AnsiConsole.MarkupLine($"[green]Modified \"{filePath}\".[/]");
        }

        return true;
    }
}