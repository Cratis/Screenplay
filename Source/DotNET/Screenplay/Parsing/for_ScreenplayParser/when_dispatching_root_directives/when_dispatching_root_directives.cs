// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser.when_dispatching_root_directives;

public class when_dispatching_root_directives
{
    public static IEnumerable<object[]> Cases => RootDirectiveCases.All;

    [Theory]
    [MemberData(nameof(Cases))]
    void should_keep_the_diagnostics_recovery_and_state(string name, string[] scope, string source, string expected)
    {
        Assert.NotEmpty(name);
        Snapshot(scope, source).ShouldEqual(expected);
    }

    internal static string Snapshot(string[] scope, string source)
    {
        var result = new ScreenplayCompiler().Parse($"{source}\nconcept Marker : String\n", "fragment.play", new PlayPlacement(scope));
        var application = result.Value!;
        var counts = new (string Key, int Count)[]
        {
            ("policies", application.Policies.Count()),
            ("purposes", application.Purposes.Count()),
            ("stickyNotes", application.StickyNotes.Count()),
            ("examples", application.Examples.Count()),
            ("seeds", application.Seeds.Count()),
            ("uiProfiles", application.UiProfiles.Count()),
            ("sampleDataPacks", application.SampleDataPacks.Count()),
            ("themes", application.Themes.Count()),
            ("triggers", application.Triggers.Count()),
            ("layouts", application.Layouts.Count()),
            ("templates", application.Templates.Count()),
            ("behaviors", application.Behaviors.Count()),
            ("exposures", application.Exposures.Count()),
            ("instanceContributions", application.InstanceContributions.Count()),
            ("declaredTriggers", application.Triggers.Count()),
            ("systems", application.Systems.Count()),
            ("stateMachines", application.StateMachines.Count()),
            ("workflows", application.Workflows.Count()),
            ("eventSources", application.EventSources.Count())
        };
        var presence = new (string Key, bool Present)[]
        {
            ("authentication", application.Authentication is not null),
            ("onboarding", application.Onboarding is not null),
            ("identity", application.Identity is not null),
            ("context", application.Context is not null)
        };
        var state = string.Join(' ', [
            $"commandReadMode={application.CommandReadMode?.ToString() ?? "none"}",
            $"timeZone={(application.TimeZone is { } zone ? $"{zone.Id}/{zone.Configurable.ToString().ToLowerInvariant()}" : "none")}",
            $"reaction={application.ReactionProductionMode?.ToString() ?? "none"}",
            $"domain={application.Domain?.Name ?? "none"}",
            $"imports=[{string.Join(',', application.Imports.Select(i => $"{i.QualifiedName}@{i.Origin ?? string.Empty}"))}]",
            $"fileImports=[{string.Join(',', application.FileImports.Select(i => i.Pattern))}]",
            $"concepts=[{string.Join(',', application.Concepts.Select(c => c.Name))}]",
            $"types=[{string.Join(',', application.Types.Select(t => t.Name))}]",
            $"personas=[{string.Join(',', application.Personas.Select(p => p.Name))}]",
            $"modules=[{string.Join(',', application.Modules.Select(m => m.Name))}]",
            $"counts={{{string.Join(',', counts.Select(c => $"{c.Key}:{c.Count}"))}}}",
            $"present={{{string.Join(',', presence.Select(p => $"{p.Key}:{p.Present.ToString().ToLowerInvariant()}"))}}}"
        ]);
        return string.Join('\n', [.. result.Diagnostics.Select(d => $"{d.Severity.ToString().ToLowerInvariant()} {d.Code} {d.Location.Line}:{d.Location.Column} {d.Message}"), state]);
    }
}
