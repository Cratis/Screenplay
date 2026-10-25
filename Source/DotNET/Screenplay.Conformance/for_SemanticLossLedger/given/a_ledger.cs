// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.given;

public class a_ledger : Specification
{
    protected const string Fixture = "RegisterProjectCorpus.V7";
    protected const string Path = "/application/modules[Projects]/features[Registration]/slices[Register]/commands[Register]";
    protected const string Issue = "Cratis/Stage#515";

    internal SemanticLossLedger _ledger = null!;
    internal SemanticLossVerdict _verdict = null!;

    internal static SemanticLossLedger Load(string losses = "", string excluded = "{}", int minimum = 1, int maximumEntries = 10, int maximumByDesign = 10, int maximumExclusions = 10, string extra = "")
    {
        var json = $$"""
            {
              "schema": "{{SemanticLossLedger.Schema}}",
              "target": "cratis-arc",
              "corpusVersion": "4.241.0",
              "minimumFixtureCount": {{minimum}},
              "budget": { "maximumEntries": {{maximumEntries}}, "maximumByDesign": {{maximumByDesign}}, "maximumExclusions": {{maximumExclusions}} },
              "excludedFixtures": {{excluded}},
              {{extra}}
              "losses": [{{losses}}]
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return SemanticLossLedger.Load(stream);
    }

    internal static string Entry(string change = "missing", string path = Path, string direction = "render", string diagnostics = "", string justification = $"\"issue\": \"{Issue}\"", string fixture = Fixture) =>
        $$"""{ "fixture": "{{fixture}}", "change": "{{change}}", "path": "{{path}}", "direction": "{{direction}}", "diagnostics": [{{diagnostics}}], {{justification}} }""";

    internal static RoundTripObservation Observed(string fixture = Fixture, params SemanticDifference[] differences) =>
        new(fixture, new([.. differences]), [], []);

    internal static RoundTripObservation Observed(RoundTripDiagnostic[] diagnostics, params SemanticDifference[] differences) =>
        new(Fixture, new([.. differences]), [.. diagnostics], []);

    internal static SemanticDifference Missing(string path = Path) => new(path, SemanticDifferenceKind.Missing, "{}", null);

    internal static SemanticDifference Changed(string path = Path) => new(path, SemanticDifferenceKind.Changed, "1", "2");
}
