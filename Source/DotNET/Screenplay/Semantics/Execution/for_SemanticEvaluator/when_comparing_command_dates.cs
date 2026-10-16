// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_comparing_command_dates : given.a_date_command
{
    readonly List<SemanticExecutionResult> _accepted = [];
    readonly List<SemanticExecutionResult> _rejected = [];
    readonly List<(SemanticExecutionResult Result, bool Produces)> _productions = [];

    void Because()
    {
        foreach (var (primitive, first, equal, later) in new[]
        {
            ("Date", "2026-01-01", "2026-01-01", "2026-01-02"),
            ("DateTime", "2026-01-01T01:00:00.0000000+02:00", "2025-12-31T23:00:00.0000000Z", "2025-12-31T23:00:00.0000001Z")
        })
        {
            foreach (var (op, to, passes) in new[]
            {
                ("==", equal, true), ("!=", equal, false), (">", later, true), (">=", equal, true), ("<", first, false), ("<=", equal, true)
            })
            {
                var result = Execute(Model(primitive, $"require to {op} from"), first, to);
                (passes ? _accepted : _rejected).Add(result);
                var rule = Execute(Model(primitive, $"to {op} \"{first}\""), first, to);
                (passes ? _accepted : _rejected).Add(rule);
                var guarded = Model(primitive, "id not empty").Replace("produces WindowKept\n          for id", $"produces when to {op} from\n          WindowKept\n            for id", StringComparison.Ordinal);
                _productions.Add((Execute(guarded, first, to), passes));
            }
        }
    }

    [Fact] void should_accept_calendar_and_instant_ordering_including_ticks_and_equal_offsets() => _accepted.TrueForAll(result => result is SemanticAccepted).ShouldBeTrue();
    [Fact] void should_reject_unsatisfied_chronological_conditions() => _rejected.TrueForAll(result => result is SemanticRejected { Category: SemanticRejectionCategory.Validation }).ShouldBeTrue();
    [Fact] void should_evaluate_guarded_productions_chronologically() => _productions.TrueForAll(pair => pair.Result is SemanticAccepted accepted && accepted.Facts.Length == (pair.Produces ? 1 : 0)).ShouldBeTrue();
    [Fact] void should_exercise_all_operators_and_both_primitives() => (_accepted.Count + _rejected.Count).ShouldEqual(24);
}
