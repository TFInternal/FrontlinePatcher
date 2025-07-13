using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;

namespace FrontlinePatcher.Processes;

public static class ProcessExecutor
{
    public static async Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory = null)
    {
        AnsiConsole.MarkupLine($"[yellow]Executing: {fileName} {arguments}[/]");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        var error = new StringBuilder();
        var outputCloseEvent = new TaskCompletionSource<bool>();
        var errorCloseEvent = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                outputCloseEvent.TrySetResult(true);
            }
            else
            {
                output.AppendLine(e.Data);
                AnsiConsole.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                errorCloseEvent.TrySetResult(true);
            }
            else
            {
                error.AppendLine(e.Data);
                AnsiConsole.MarkupLine($"[red]{e.Data}[/]");
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // On Windows for some reason apktool has a prompt for user input
            // so we need to send an empty line to continue
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StandardInput.WriteLine();
                process.StandardInput.Close();
            }

            await Task.WhenAll(process.WaitForExitAsync(), outputCloseEvent.Task, errorCloseEvent.Task);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
            return new ProcessResult(-1, string.Empty, e.Message);
        }
        
        AnsiConsole.WriteLine($"Process exited with code {process.ExitCode}.");
        return new ProcessResult(process.ExitCode, output.ToString().Trim(), error.ToString().Trim());
    }
}