// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.given;

public class a_worktree_connection : a_connection
{
    internal string RepositoryPath = null!;
    internal string WorktreePath = null!;
    internal string ModelPath = null!;
    internal string WorktreeModelPath = null!;

    void Establish()
    {
        RepositoryPath = Path.Combine(RootPath, "repository");
        WorktreePath = Path.Combine(RootPath, "worktree");
        ModelPath = Path.Combine(RepositoryPath, ".cratis", "screenplay");
        WorktreeModelPath = Path.Combine(WorktreePath, ".cratis", "screenplay");
        CreateModel(RepositoryPath);
        Git(RepositoryPath, "init", "-b", "main");
        Git(RepositoryPath, "add", ".cratis/screenplay/application.play");
        Git(RepositoryPath, "commit", "-m", "Create model");
        Git(RepositoryPath, "worktree", "add", "--detach", WorktreePath, "HEAD");
        Root = new(ModelPath);
        Connection = new(new McpTools(Root));
        Initialize();
    }

    internal JsonElement Open(string? path = null)
    {
        var response = path is null
            ? Call("open-workspace", new { applicationName = "Projects" })
            : Call("open-workspace", new { path, applicationName = "Projects" });
        response.TryGetProperty("error", out _).ShouldBeFalse();
        var result = response.GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeFalse();

        return result.GetProperty("structuredContent");
    }

    internal JsonElement ProposeMove(JsonElement opened, string path = "renamed.play")
    {
        var workspace = Workspace();
        return Call("propose", new
        {
            operation = "move-document",
            documentId = workspace.Documents[0].Id.ToString(),
            path,
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result");
    }

    internal JsonElement ApplyMove(JsonElement opened, string proposalId) => Call("apply", new
    {
        proposalId,
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString()
    }).GetProperty("result");

    internal static void CreateModel(string checkout)
    {
        var path = Path.Combine(checkout, ".cratis", "screenplay");
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "application.play"), Source);
    }

    internal static void Git(string directory, params string[] arguments)
    {
        var settings = Path.Combine(Path.GetTempPath(), $"screenplay-git-{Guid.NewGuid():N}");
        Directory.CreateDirectory(settings);
        try
        {
            var config = Path.Combine(settings, "config");
            var hooks = Path.Combine(settings, "hooks");
            File.WriteAllText(config, string.Empty);
            Directory.CreateDirectory(hooks);
            var start = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var variable in start.Environment.Keys.Where(key => key.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                start.Environment.Remove(variable);
            }
            start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
            start.Environment["GIT_CONFIG_GLOBAL"] = config;
            foreach (var argument in new[] { "-C", directory, "-c", $"core.hooksPath={hooks}", "-c", "user.name=Screenplay specs", "-c", "user.email=specs@example.invalid", "-c", "commit.gpgsign=false" }.Concat(arguments))
            {
                start.ArgumentList.Add(argument);
            }
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(10000))
            {
                process.Kill(entireProcessTree: true);
                throw new McpFailure("Spec Git setup timed out.");
            }

            if (process.ExitCode != 0)
            {
                throw new McpFailure($"Spec Git setup failed: {output.GetAwaiter().GetResult()} {error.GetAwaiter().GetResult()}");
            }
        }
        finally
        {
            Directory.Delete(settings, true);
        }
    }
}
