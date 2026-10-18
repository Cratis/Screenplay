// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Observations.for_ObservationReportJson;

public class when_writing_a_report : for_ObservationComparison.given.a_compiled_model
{
    string _json = null!;
    string _golden = null!;

    static IEnumerable<string> PropertyNames(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().SelectMany(property => new[] { property.Name }.Concat(PropertyNames(property.Value))),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(PropertyNames),
        _ => []
    };

    void Establish()
    {
        Reaction(mapped: true);
        _snapshot.Consumers = [_snapshot.Consumers[0] with { LastHandledSequenceNumber = 0, NextSequenceNumber = 1 }];
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Screenplay.slnx"))) root = root.Parent!;
        var path = Path.Combine(root.FullName, "Source/DotNET/Screenplay/Observations/for_ObservationReportJson/report.golden.json");
        _golden = File.Exists(path) ? File.ReadAllText(path).TrimEnd() : string.Empty;
        _result = Compare();
    }

    void Because() => _json = ObservationReportJson.Write(_result);

    [Fact] void should_match_the_reviewed_golden() => _json.ShouldEqual(_golden);
    [Fact] void should_not_include_payloads_or_event_source_identifiers() => PropertyNames(JsonDocument.Parse(_json).RootElement).Any(value => value.Contains("payload", StringComparison.OrdinalIgnoreCase) || value.Contains("eventSource", StringComparison.OrdinalIgnoreCase) || value.Contains("causation", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_keep_human_statements_identical() => JsonDocument.Parse(_json).RootElement.GetProperty("findings").EnumerateArray().Select(value => value.GetProperty("statement").GetString()).ShouldEqual(_result.Findings.Select(value => value.Statement));
    [Fact] void should_omit_unknown_verdict_instead_of_reporting_false() => JsonDocument.Parse(_json).RootElement.TryGetProperty("hasDrift", out _).ShouldBeFalse();
}
