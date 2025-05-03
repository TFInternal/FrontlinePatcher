using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace FrontlinePatcher.Files;

public class FileModificationRule
{
    public string FilePath { get; }
    
    public Regex SearchPattern { get; }
    
    public string Replacement { get; }
    
    public FileModificationRule(string filePath, [StringSyntax(StringSyntaxAttribute.Regex)] string searchPattern, string replacement)
    {
        FilePath = filePath;
        SearchPattern = new Regex(searchPattern, RegexOptions.Compiled | RegexOptions.Multiline);
        Replacement = replacement;
    }
}