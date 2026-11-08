// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool;

// The lineage trace as an indented tree: '->' where the value goes, '<-' where it comes from, breaks in brackets.
static class LineageText
{
    internal static void Write(McpLineageReport report, TextWriter output)
    {
        output.WriteLine($"Lineage of {report.Subject.Address} ({report.Subject.Kind}), {McpLineageDirections.Name(report.Direction)}");
        foreach (var hop in report.Hops)
        {
            var arrow = hop.Direction switch { "downstream" => "-> ", "upstream" => "<- ", _ => string.Empty };
            var status = hop.Step.Status switch
            {
                "resolved" => string.Empty,
                "repeated" => $" [repeated, see #{hop.RepeatOf}]",
                var other => $" [{other}] {hop.Step.Reason}"
            };
            var address = hop.Step.Address is null ? string.Empty : $" {hop.Step.Address}";
            var text = hop.Step.Text is null ? string.Empty : $" '{hop.Step.Text}'";
            output.WriteLine($"{new string(' ', hop.Depth * 2)}{arrow}{hop.Step.Role}{address}{text}{Location(hop.Step.Location)}{status}");
        }

        var breaks = report.Breaks.Length;
        output.WriteLine(breaks == 0
            ? $"{report.Hops.Count} hop(s), complete."
            : $"{report.Hops.Count} hop(s), {breaks} break(s){(report.Truncated ? ", truncated" : string.Empty)}.");
    }

    static string Location(SourceLocation location) => location.Path is null ? $" ({location.Line}:{location.Column})" : $" ({location.Path}:{location.Line}:{location.Column})";
}
