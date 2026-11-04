// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_SpecificationParser.when_dispatching_given_forms;

public class when_dispatching_given_forms
{
    public static IEnumerable<object[]> Cases => GivenCases.All;

    [Theory]
    [MemberData(nameof(Cases))]
    void should_keep_the_diagnostics_recovery_and_state(string name, bool exact, string body, string expected)
    {
        Assert.NotEmpty(name);
        Snapshot(exact, body).ShouldEqual(expected);
    }

    static string Snapshot(bool exact, string body)
    {
        var source = $"{(exact ? "numbers exact\n" : string.Empty)}specification S\n  {body.Replace("\n", "\n  ")}\n  given Marker\n";
        var result = new ScreenplayCompiler().CompileSpecification(source);
        var specification = result.Value!;
        var caller = specification.GivenCaller is { } given ? $"{given.Authenticated.ToString().ToLowerInvariant()}/{string.Join("+", given.Roles)}/{given.Claims.Count()}" : "none";
        var state = $"events=[{string.Join(",", specification.Given.Select(e => e.EventType))}] readModels=[{string.Join(",", specification.GivenReadModels.Select(m => m.Name))}] captures=[{string.Join(",", specification.GivenCaptures.Select(c => c.Capture))}] generated=[{string.Join(",", specification.GivenGenerated.Select(g => $"{g.Command}.{g.Property}"))}] clock={specification.GivenClock?.Instant ?? "none"} timeZone={specification.GivenTimeZone?.Id ?? "none"} caller={caller} persona={specification.GivenCallerPersona?.Name ?? "none"} failures=[{string.Join(",", specification.GivenOperationFailures.Select(f => f.Operation))}]";
        return string.Join('\n', [.. result.Diagnostics.Select(d => $"{d.Severity.ToString().ToLowerInvariant()} {d.Code} {d.Location.Line}:{d.Location.Column} {d.Message}"), state]);
    }
}
