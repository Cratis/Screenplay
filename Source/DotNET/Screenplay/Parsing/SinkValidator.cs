// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Holds the named sinks of an application to the read models and graphs that choose them.
/// </summary>
/// <remarks>
/// Sink names are unique application-wide ignoring case, so two names a case-insensitive store would merge never
/// coexist. A directive must name its sink exactly as declared.
/// </remarks>
internal static class SinkValidator
{
    internal static void Validate(ApplicationSyntax application, IEnumerable<SliceSyntax> slices, ParserContext context)
    {
        var sinks = new Dictionary<string, SinkSyntax>(StringComparer.Ordinal);
        var seen = new Dictionary<string, SinkSyntax>(StringComparer.OrdinalIgnoreCase);
        foreach (var sink in application.Sinks)
        {
            if (seen.TryGetValue(sink.Name, out var first))
            {
                context.Error(DiagnosticCodes.DuplicateSink, $"Sink '{sink.Name}' is declared more than once; sink names are unique ignoring case", sink.Location);
                continue;
            }

            seen.Add(sink.Name, sink);
            sinks.Add(sink.Name, sink);
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var slice in slices)
        {
            foreach (var readModel in slice.ReadModels ?? [])
            {
                if (readModel.Sink is { } name)
                {
                    Check(name, SinkKind.ReadModels, $"Read model '{readModel.Name}'", readModel.DirectiveLocations.GetValueOrDefault(SinkParser.Keyword, readModel.Location), sinks, used, context);
                }
            }

            foreach (var graph in slice.Graphs)
            {
                if (graph.Sink is { } name)
                {
                    Check(name, SinkKind.Graphs, $"Graph '{graph.Name}'", graph.DirectiveLocations.GetValueOrDefault(SinkParser.Keyword, graph.Location), sinks, used, context);
                }
            }
        }

        foreach (var sink in sinks.Values.Where(sink => !used.Contains(sink.Name)))
        {
            context.Warning(DiagnosticCodes.UnusedSink, $"Sink '{sink.Name}' is not named by any {Describe(sink.Stores)}", sink.Location);
        }
    }

    static void Check(string name, SinkKind kind, string owner, SourceLocation location, Dictionary<string, SinkSyntax> sinks, HashSet<string> used, ParserContext context)
    {
        if (!sinks.TryGetValue(name, out var sink))
        {
            context.Error(DiagnosticCodes.UnknownSink, $"{owner} names unknown sink '{name}' - declare it with 'sink {name} for {Keyword(kind)}'", location);
            return;
        }

        used.Add(sink.Name);
        if (sink.Stores != kind)
        {
            context.Error(DiagnosticCodes.SinkKindMismatch, $"{owner} names sink '{name}', which is declared 'for {Keyword(sink.Stores)}'", location);
        }
    }

    static string Keyword(SinkKind kind) => kind == SinkKind.Graphs ? "graphs" : "readmodels";

    static string Describe(SinkKind kind) => kind == SinkKind.Graphs ? "graph" : "read model";
}
