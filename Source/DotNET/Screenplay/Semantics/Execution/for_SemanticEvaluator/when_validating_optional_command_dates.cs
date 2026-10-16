// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_validating_optional_command_dates : given.a_date_command
{
    readonly List<SemanticExecutionResult> _results = [];

    void Because()
    {
        foreach (var (primitive, op) in new[] { "Date", "DateTime" }.SelectMany(primitive => Operators.Select(op => (primitive, op))))
        {
            var literal = primitive == "Date" ? "2026-01-01" : "2026-01-01T00:00:00Z";
            var source = Model(primitive, $"to {op} \"{literal}\"").Replace($"to {primitive}", $"to {primitive} optional", StringComparison.Ordinal);
            _results.Add(Execute(source, primitive == "Date" ? "2026-01-01" : "2026-01-01T00:00:00.0000000Z", null));
        }
    }

    [Fact] void should_keep_absence_semantics_for_every_date_rule() => _results.Count.ShouldEqual(12);
    [Fact] void should_accept_absent_values() => _results.TrueForAll(result => result is SemanticAccepted).ShouldBeTrue();
}
