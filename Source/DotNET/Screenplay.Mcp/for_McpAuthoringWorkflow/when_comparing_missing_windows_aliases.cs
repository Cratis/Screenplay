// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_comparing_missing_windows_aliases
{
    [Theory]
    [InlineData(".screenplay/IDENTI~1.JSO", ".screenplay/identities.json", true)]
    [InlineData(".screenplay/IDENTI~1.JSO", ".screenplay/identities.json", false)]
    [InlineData("SCREEN~1/IDENTI~1.JSO", ".screenplay/identities.json", true)]
    [InlineData("LONGDI~1/APPREN~1.PLA", "long directory/application-renamed.play", true)]
    [InlineData("long directory/APPREN~1.PLA", "LONGDI~1/application-renamed.play", true)]
    [InlineData("LONGDI~1/ordinary.cs", "long directory/ordinary.cs", true)]
    [InlineData(".screenplay/IDENTI~9.JSO", ".screenplay/identities.json", true)]
    [InlineData("ÉVIDEN~1.CS", "evidence-source.cs", true)]
    [InlineData("ÉVIDEN~1.CS", "evidence-source.cs", false)]
    void should_refuse_only_platform_relevant_potential_short_names(string input, string target, bool windows) =>
        McpRepairWriteConflicts.MissingNamesMayAlias(input, target, windows).ShouldEqual(windows);

    [Theory]
    [InlineData(".screenplay/Handler.cs", ".screenplay/identities.json")]
    [InlineData(".screenplay/IDENTI1.JSO", ".screenplay/identities.json")]
    [InlineData(".screenplay/IDENTI~1.JSO/Handler.cs", ".screenplay/identities.json")]
    [InlineData("Handler.cs", "application.play")]
    [InlineData("LONGDI~1/Handler.cs", "long directory/application-renamed.play")]
    [InlineData("Handler.cs", "long directory/application-renamed.play")]
    void should_accept_provably_unrelated_missing_names(string input, string target) =>
        McpRepairWriteConflicts.MissingNamesMayAlias(input, target, windows: true).ShouldBeFalse();
}

// Discovery reports a real skip, rather than counting a no-op native test as a pass.
internal sealed partial class WindowsShortNamesFactAttribute : FactAttribute
{
    public WindowsShortNamesFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Native Windows DOS 8.3 integration requires Windows; deterministic comparison tests run on every host.";
            return;
        }

        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, ".git")) && !File.Exists(Path.Combine(repository.FullName, ".git"))) repository = repository.Parent;
        var output = Environment.GetEnvironmentVariable("AI_WORK_OUTPUT") ?? Path.Combine(repository!.FullName, ".ai-work", "mcp-specs");
        var probe = Path.Combine(output, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(probe);
        try
        {
            var metadata = Path.Combine(probe, ".screenplay");
            McpFileAccess.CreatePrivateDirectory(metadata);
            var path = Path.Combine(metadata, "identities.json");
            File.WriteAllText(path, "short-name probe");
            if (!Path.GetFileName(ShortPath(path)).Contains('~') || !Path.GetFileName(ShortPath(metadata)).Contains('~'))
            {
                Skip = "Native short-name metadata confirms the test volume did not generate DOS 8.3 aliases for the identity file or metadata directory.";
            }
        }
        finally
        {
            Directory.Delete(probe, true);
        }
    }

    internal static string ShortPath(string path)
    {
        var buffer = new char[32768];
        var length = GetShortPathName(path, buffer, (uint)buffer.Length);

        if (length == 0)
        {
            throw new McpFailure($"Native short-name metadata query failed with Windows error {Marshal.GetLastPInvokeError()}.");
        }

        if (length >= buffer.Length)
        {
            throw new McpFailure("Native short-name metadata exceeds the bounded path buffer.");
        }

        return new string(buffer, 0, (int)length);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "GetShortPathNameW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint GetShortPathName(string longPath, [Out] char[] shortPath, uint bufferLength);
}
