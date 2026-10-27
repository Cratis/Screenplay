// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpConnection.for_SpecGit;

public class when_git_exceeds_its_deadline : Specification
{
    Exception _failure;
    string _config;
    bool _exitedBeforeCleanup;

    void Because() => _failure = Catch.Exception(() => given.SpecGit.Run(
        Path.GetTempPath(), ["controlled-process"], TimeSpan.Zero, Start, WaitForTermination));

    Process Start(ProcessStartInfo start)
    {
        _config = start.Environment["GIT_CONFIG_GLOBAL"]!;
        start.FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";
        start.ArgumentList.Clear();
        start.ArgumentList.Add(OperatingSystem.IsWindows() ? "/c" : "-c");
        start.ArgumentList.Add(OperatingSystem.IsWindows()
            ? "echo ready& ping -t 127.0.0.1 >nul"
            : "printf 'ready\\n'; while :; do sleep 300; done");
        var process = Process.Start(start)!;
        try
        {
            process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult().ShouldEqual("ready");
            return process;
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            process.Dispose();
            throw;
        }
    }

    async Task WaitForTermination(Process process)
    {
        await process.WaitForExitAsync();
        await Task.Yield();
        _exitedBeforeCleanup = process.HasExited && File.Exists(_config);

        // An asynchronous cleanup error must be awaited and retained in the original diagnostic.
        throw new IOException("controlled termination observation failure");
    }

    [Fact] void should_fail_as_a_timeout() => _failure.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_name_the_git_command() => _failure.Message.ShouldContain("git controlled-process (in ");
    [Fact] void should_name_the_override() => _failure.Message.ShouldContain(given.SpecGit.TimeoutVariable);
    [Fact] void should_await_exit_before_deleting_configuration() => _exitedBeforeCleanup.ShouldBeTrue();
    [Fact] void should_delete_configuration_after_exit() => File.Exists(_config).ShouldBeFalse();
    [Fact] void should_preserve_the_timeout_when_cleanup_fails() => _failure.Message.ShouldContain("controlled termination observation failure");
}
