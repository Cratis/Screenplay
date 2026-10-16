// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_validating_compared_datetime_inputs : given.a_date_command
{
    readonly List<SemanticExecutionResult> _results = [];

    void Because()
    {
        const string first = "2026-01-01T00:00:00.0000000Z";
        const string offsetless = "2026-01-02T00:00:00.0000000";
        _results.Add(Execute(Model("DateTime", "require to >= from"), first, offsetless));
        _results.Add(Execute(Model("DateTime", "require to >= from"), offsetless, first));
        _results.Add(Execute(Model("DateTime", "to >= \"2026-01-01T00:00:00Z\""), first, offsetless));
        _results.Add(Execute(Model("DateTime", "require id == \"window\" or to >= from"), first, offsetless));
        _results.Add(Execute(Model("DateTime", "id == \"failure\" message \"Rule ran\"").Replace("produces WindowKept\n          for id", "produces when id == \"other\" and to >= from\n          WindowKept\n            for id", StringComparison.Ordinal), first, offsetless));
        _results.Add(Execute(Model("DateTime", "require to >= from").Replace("        produces WindowKept", "        validate\n          ```csharp\n          yield return \"Rule ran\";\n          ```\n        produces WindowKept", StringComparison.Ordinal), first, offsetless));
    }

    [Fact] void should_validate_even_skipped_comparisons() => _results.Count.ShouldEqual(6);
    [Fact] void should_reject_as_validation_rather_than_contract() => _results.TrueForAll(result => result is SemanticRejected { Category: SemanticRejectionCategory.Validation }).ShouldBeTrue();
    [Fact] void should_reject_before_any_rule_runs() => _results.Cast<SemanticRejected>().All(result => result.ValidationFailures.Length == 1 && result.Details.Contains("explicit offset or Z", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_commit_nothing() => _results.TrueForAll(result => result.World == SemanticWorld.Empty).ShouldBeTrue();
}
