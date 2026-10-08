// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

static class ModelTest
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        string? target = null;
        string? filter = null;
        var format = "text";
        var hasFormat = false;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == "--filter" || argument == "--format")
            {
                if (index + 1 == args.Length || args[index + 1].StartsWith('-') || string.IsNullOrWhiteSpace(args[index + 1]) ||
                    (argument == "--filter" && filter is not null) || (argument == "--format" && hasFormat))
                {
                    error.WriteLine($"{argument} requires one value and cannot be repeated.");
                    return 2;
                }

                var value = args[++index];
                if (argument == "--filter")
                {
                    filter = value;
                }
                else
                {
                    format = value;
                    hasFormat = true;
                }
            }
            else if (argument.StartsWith('-') || target is not null)
            {
                error.WriteLine($"Unexpected argument '{argument}'.");
                return 2;
            }
            else
            {
                target = argument;
            }
        }

        if (format is not "text" and not "json")
        {
            error.WriteLine("--format requires text or json.");
            return 2;
        }

        target ??= Directory.GetCurrentDirectory();
        var isFile = File.Exists(target);
        if ((!isFile && !Directory.Exists(target)) || (isFile && !target.EndsWith(".play", StringComparison.OrdinalIgnoreCase)))
        {
            error.WriteLine($"'{target}' must be an existing .play file or directory.");
            return 2;
        }

        try
        {
            var snapshot = McpSnapshot.Compile(target, isFile);

            // The compiler owns file/import discovery. Only its selected source set enters execution.
            var directory = isFile ? Path.GetDirectoryName(Path.GetFullPath(target))! : Path.GetFullPath(target);
            var documents = McpTestDocuments.From(snapshot, directory, out var rootDirectory);
            var root = new McpRoot(rootDirectory);
            var persisted = new McpManagedFiles(root).Read(McpState.FileName);
            var name = root.ApplicationName;
            var identity = ApplicationIdentity.Create(name);
            ScreenplayWorkspace workspace;
            if (persisted is not null)
            {
                workspace = McpState.Deserialize(persisted).Open(root);
            }
            else
            {
                workspace = documents.Length == 0
                    ? ScreenplayWorkspace.CreateEmpty(identity, name)
                    : ScreenplayWorkspace.Create(identity, name, documents, SemanticIdentityCatalog.Empty(identity));
            }
            var selectedPaths = documents.Select(document => document.Path.Value).ToHashSet(StringComparer.Ordinal);
            var selectedDocuments = persisted is not null
                ? workspace.Documents.Where(document => selectedPaths.Contains(document.Path.Value)).Select(document => document.Id).ToHashSet()
                : null;
            var report = McpSpecificationExecution.Run(workspace, filter, documents: selectedDocuments);
            if (format == "json")
            {
                output.WriteLine(JsonSerializer.Serialize(report, McpJson.Options));
            }
            else
            {
                WriteText(report, output);
            }

            return report.Outcome switch { "passed" => 0, "failed" => 1, _ => 3 };
        }
        catch (McpFailure failure)
        {
            error.WriteLine(failure.Message);
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidWorkspaceDocument or InvalidScreenplayWorkspace or InvalidSemanticContract or InvalidPortablePlayPath)
        {
            error.WriteLine($"Could not test '{target}': {exception.Message}");
            return 2;
        }
    }

    static void WriteText(McpSpecificationReport report, TextWriter output)
    {
        foreach (var diagnostic in report.Diagnostics)
        {
            output.WriteLine($"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message}");
        }

        foreach (var issue in report.PlanIssues)
        {
            output.WriteLine($"Unsupported {issue.Kind}: {issue.Details}");
        }
        foreach (var result in report.Results)
        {
            output.WriteLine($"{result.Outcome}: {result.Address}");
            foreach (var failure in result.Failures)
            {
                output.WriteLine($"  {failure}");
            }
            if (result.Reason is not null)
            {
                output.WriteLine($"  {result.Capability}: {result.Reason}");
            }
        }

        output.WriteLine($"{report.Outcome}: {report.Discovered} discovered, {report.Selected} selected, {report.Executed} executed; {report.Passed} passed, {report.Failed} failed, {report.Unsupported} unsupported");
    }
}
