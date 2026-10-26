// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class invalid_present_field_types : given.a_ledger
{
    Dictionary<string, SemanticLossVerdict> _verdicts;

    void Establish() => _verdicts = [];

    void Because()
    {
        foreach (var diagnostics in new[] { "\"SP0001\"", "[123]", "[\"SP0001\",123]", "null", "{}" })
        {
            Observe($"diagnostics:{diagnostics}", Entry().Replace("\"diagnostics\": []", $"\"diagnostics\": {diagnostics}", StringComparison.Ordinal));
        }

        Observe("path", Entry(change: "diagnostic").Replace($"\"path\": \"{Path}\"", "\"path\": 123", StringComparison.Ordinal));
        Observe("issue", Entry(justification: "\"byDesign\": \"Intentional.\", \"issue\": 123"));
        Observe("byDesign", Entry(justification: $"\"issue\": \"{Issue}\", \"byDesign\": false"));
        Observe("null path", Entry(change: "diagnostic").Replace($"\"path\": \"{Path}\"", "\"path\": null", StringComparison.Ordinal));
        Observe("null issue", Entry(justification: "\"byDesign\": \"Intentional.\", \"issue\": null"));
        Observe("null byDesign", Entry(justification: $"\"issue\": \"{Issue}\", \"byDesign\": null"));
    }

    void Observe(string name, string entry) => _verdicts[name] = Load(entry).Reconcile([Observed(Fixture, Missing())], requireCompleteRun: false);

    [Fact]
    void should_report_each_invalid_present_type_as_malformed() => Assert.True(
        _verdicts.All(pair => pair.Value.Malformed.Length > 0), string.Join(", ", _verdicts.Where(pair => pair.Value.Malformed.Length == 0).Select(pair => pair.Key)));
    [Fact] void should_never_reconcile_cleanly() => Assert.True(_verdicts.Values.All(verdict => !verdict.IsClean));
    [Fact] void should_exercise_every_case() => _verdicts.Count.ShouldEqual(11);
}
