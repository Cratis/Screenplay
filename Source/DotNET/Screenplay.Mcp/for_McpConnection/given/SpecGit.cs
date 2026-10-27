// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpConnection.given;

// Runs git for spec setup in an isolated configuration with a generous, overridable deadline.
static class SpecGit
{
    internal const string TimeoutVariable = "SCREENPLAY_SPEC_GIT_TIMEOUT_SECONDS";
    static TimeSpan DefaultDeadline => TimeSpan.FromMinutes(2);
    static TimeSpan CleanupDeadline => TimeSpan.FromSeconds(5);

    internal static TimeSpan Deadline => ValidateDeadline(
        int.TryParse(Environment.GetEnvironmentVariable(TimeoutVariable), out var seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : DefaultDeadline);

    internal static void Run(string directory, string[] arguments) => Run(directory, arguments, Deadline);

    internal static void Run(string directory, string[] arguments, TimeSpan deadline) =>
        Run(directory, arguments, deadline, start => Process.Start(start)!, process => process.WaitForExitAsync());

    internal static void Run(string directory, string[] arguments, TimeSpan deadline, Func<ProcessStartInfo, Process> startProcess, Func<Process, Task> waitForTermination) =>
        RunAsync(directory, arguments, deadline, startProcess, waitForTermination).GetAwaiter().GetResult();

    static async Task RunAsync(string directory, string[] arguments, TimeSpan deadline, Func<ProcessStartInfo, Process> startProcess, Func<Process, Task> waitForTermination)
    {
        using var cancellation = new CancellationTokenSource(ValidateDeadline(deadline));
        var settings = Path.Combine(Path.GetTempPath(), $"screenplay-git-{Guid.NewGuid():N}");
        Directory.CreateDirectory(settings);
        var deleteSettings = true;
        try
        {
            using var process = startProcess(StartInfo(directory, arguments, settings));
            process.StandardInput.Close();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            var started = Stopwatch.StartNew();
            try
            {
                await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var cleanup = await Stop(process, waitForTermination).ConfigureAwait(false);
                deleteSettings = process.HasExited;
                if (!deleteSettings)
                {
                    cleanup += $"; isolated configuration retained at {settings} because the process has not exited";
                }

                throw new McpFailure(
                    $"Spec Git setup timed out after {started.Elapsed.TotalSeconds:0.#} s (deadline {deadline.TotalSeconds:0.#} s, override with {TimeoutVariable}): " +
                    $"{Describe(directory, arguments)}{cleanup}{await Drain(output, error).ConfigureAwait(false)}");
            }

            if (process.ExitCode != 0)
            {
                throw new McpFailure(
                    $"Spec Git setup failed with exit code {process.ExitCode}: {Describe(directory, arguments)}{await Drain(output, error).ConfigureAwait(false)}");
            }
        }
        catch (McpFailure failure)
        {
            DeleteSettingsAfter(failure, settings, deleteSettings);
            throw;
        }
        catch
        {
            DeleteSettings(settings, deleteSettings);
            throw;
        }

        DeleteSettings(settings, deleteSettings);
    }

    static TimeSpan ValidateDeadline(TimeSpan deadline)
    {
        if (deadline.TotalMilliseconds > int.MaxValue)
        {
            throw new McpFailure($"{TimeoutVariable} must not exceed {int.MaxValue} milliseconds.");
        }

        return deadline;
    }

    static void DeleteSettings(string settings, bool deleteSettings)
    {
        if (deleteSettings)
        {
            Directory.Delete(settings, true);
        }
    }

    static void DeleteSettingsAfter(McpFailure failure, string settings, bool deleteSettings)
    {
        try
        {
            DeleteSettings(settings, deleteSettings);
        }
        catch (Exception exception)
        {
            throw new McpFailure($"{failure.Message}; configuration cleanup failed: {exception.Message}");
        }
    }

    static async Task<string> Stop(Process process, Func<Process, Task> waitForTermination)
    {
        var diagnostic = string.Empty;
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) when (process.HasExited)
        {
            // Natural exit may race cancellation.
        }
        catch (Exception exception)
        {
            diagnostic = $"; process kill failed: {exception.Message}";
        }

        try
        {
            await waitForTermination(process).WaitAsync(CleanupDeadline).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            diagnostic += $"; process cleanup failed: {exception.Message}";
        }

        return diagnostic;
    }

    static ProcessStartInfo StartInfo(string directory, string[] arguments, string settings)
    {
        var config = Path.Combine(settings, "config");
        var hooks = Path.Combine(settings, "hooks");
        File.WriteAllText(config, string.Empty);
        Directory.CreateDirectory(hooks);
        var start = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var variable in start.Environment.Keys.Where(key => key.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            start.Environment.Remove(variable);
        }
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = config;
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var argument in new[] { "-C", directory, "-c", $"core.hooksPath={hooks}", "-c", "user.name=Screenplay specs", "-c", "user.email=specs@example.invalid", "-c", "commit.gpgsign=false" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        return start;
    }

    static string Describe(string directory, string[] arguments) => $"git {string.Join(' ', arguments)} (in {directory})";

    static string BoundedOutput(string output) => output.Length > 4000 ? $"[truncated] {output[^4000..]}" : output;

    static async Task<string> Drain(Task<string> output, Task<string> error)
    {
        try
        {
            var captured = await Task.WhenAll(output, error).WaitAsync(CleanupDeadline).ConfigureAwait(false);
            return $"; stdout: {BoundedOutput(captured[0].Trim())}; stderr: {BoundedOutput(captured[1].Trim())}";
        }
        catch (Exception exception)
        {
            return $"; output capture failed: {exception.Message}";
        }
    }
}
