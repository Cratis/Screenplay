// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool;

// Prints the trace-lineage report for one property or screen field: the same hops MCP pages, all at once.
static class LineageCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var arguments = CommandArguments.Parse(args, CliCommandCatalog.Lineage);
        if (arguments is null || arguments.Positional.Count is < 1 or > 2)
        {
            error.WriteLine($"Usage: {CliCommandCatalog.Lineage.Usage}");
            return 2;
        }

        var format = arguments.Option(CliCommandCatalog.Format) ?? "text";
        if (format is not ("text" or "json"))
        {
            error.WriteLine($"{CliCommandCatalog.Format.Name} must be text or json.");
            return 2;
        }

        var target = arguments.Positional.Count == 2 ? arguments.Positional[1] : Directory.GetCurrentDirectory();
        if ((!File.Exists(target) && !Directory.Exists(target)) || (File.Exists(target) && !target.EndsWith(".play", StringComparison.OrdinalIgnoreCase)))
        {
            error.WriteLine($"'{target}' must be an existing .play file or model directory.");
            return 2;
        }

        try
        {
            var direction = McpLineageDirections.Parse(arguments.Option(CliCommandCatalog.LineageDirection));
            var snapshot = McpSnapshot.Compile(target, File.Exists(target));
            if (snapshot.Sources.Count == 0)
            {
                error.WriteLine($"No .play files found beneath '{target}'.");
                return 2;
            }

            var report = McpLineage.Trace(snapshot, arguments.Positional[0], direction);
            if (format == "json") output.WriteLine(JsonSerializer.Serialize(Json(snapshot, report), McpJson.Options));
            else LineageText.Write(report, output);

            return report.Breaks.Length == 0 ? 0 : 1;
        }
        catch (Exception exception) when (ModelSource.IsReadFailure(exception))
        {
            error.WriteLine($"Could not trace '{arguments.Positional[0]}': {exception.Message}");
            return 2;
        }
    }

    static object Json(McpSnapshot snapshot, McpLineageReport report) => new
    {
        snapshot.Compilation.Success,
        snapshot.SourceRevision,
        coverage = McpLineage.Coverage,
        subject = report.SubjectJson(),
        direction = McpLineageDirections.Name(report.Direction),
        hopCount = report.Hops.Count,
        complete = report.Breaks.Length == 0,
        truncated = report.Truncated,
        breakCount = report.Breaks.Length,
        breaks = report.Breaks.Select(hop => hop.Summary()).ToArray(),
        hops = report.Hops.Select(hop => hop.Json()).ToArray()
    };
}
