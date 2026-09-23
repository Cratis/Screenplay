// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.for_EventContextCatalog.given;

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog;

public class when_holding_the_editor_resolver_to_it : Specification
{
    const string Variable = "SCREENPLAY_REGENERATE_EVENT_CONTEXT_VECTORS";
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    static readonly string FilePath = Path.Combine(EventContextSurfaces.Root(), "Source", "Screenplay", "Monaco", "screenplay-language", "event-context-resolution-vectors.json");
    string _checkedIn = string.Empty;

    void Establish()
    {
        if (Environment.GetEnvironmentVariable(Variable) == "1")
        {
            File.WriteAllText(FilePath, Render());
            throw new EventContextSurfacesRegenerated($"Rewrote '{FilePath}'. Review the diff and rerun without {Variable}.");
        }
    }

    void Because() => _checkedIn = File.ReadAllText(FilePath).ReplaceLineEndings("\n");

    [Fact] void should_match_the_catalog_resolutions() => _checkedIn.ShouldEqual(Render());

    static string Render()
    {
        var paths = EventContextCatalog.Paths.Select(path => path.Path)
            .Concat(EventContextCatalog.Paths.Select(path => char.ToUpperInvariant(path.Path[0]) + path.Path[1..]))
            .Concat(EventContextCatalog.Paths.Select(path => path.Path + ".unknown"))
            .Concat(["", "eventType.", ".eventType", "unknown", "unknown.id", "eventType.name", "eventType.id.value.unknown", "tags.value", "causation.occurred", "causedBy.onBehalfOf.onBehalfOf.userName", "occurred.Week()", "occurred.week", "occurred.Week.value", "EventType.Id"])
            .Distinct(StringComparer.Ordinal);
        var vectors = paths.Select(path => new { path, status = char.ToLowerInvariant(EventContextCatalog.Resolve(path).Status.ToString()[0]) + EventContextCatalog.Resolve(path).Status.ToString()[1..] });
        return JsonSerializer.Serialize(vectors, _jsonOptions) + "\n";
    }
}
