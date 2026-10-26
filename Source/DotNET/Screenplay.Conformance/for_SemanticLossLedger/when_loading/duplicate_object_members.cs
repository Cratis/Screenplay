// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class duplicate_object_members : given.a_ledger
{
    SemanticLossVerdict[] _verdicts;

    void Because()
    {
        // language=json
        const string exclusions = "{\"Skipped\": \"Reason one.\", \"Skipped\": \"Reason two.\"}";
        _verdicts =
        [
            Load(extra: "\"budget\": {\"maximumEntries\": 100, \"maximumByDesign\": 100, \"maximumExclusions\": 100},").Reconcile([Observed(Fixture)], false),
            Load(extra: "\"budget\": {\"maximumEntries\": 0, \"maximumEntries\": 100, \"maximumByDesign\": 100, \"maximumExclusions\": 100},").Reconcile([Observed(Fixture)], false),
            Load(Entry().Replace("\"change\":", $"\"path\": \"{Path}\", \"change\":", StringComparison.Ordinal)).Reconcile([Observed(Fixture, Missing())], false),
            Load(excluded: exclusions).Reconcile([Observed(Fixture)], false),
        ];
    }

    [Fact] void should_report_each_duplicate_member() => Assert.True(_verdicts.All(verdict => verdict.Malformed.Any(finding => finding.Message.Contains("Duplicate", StringComparison.Ordinal))));
    [Fact] void should_never_reconcile_cleanly() => Assert.True(_verdicts.All(verdict => !verdict.IsClean));
}
