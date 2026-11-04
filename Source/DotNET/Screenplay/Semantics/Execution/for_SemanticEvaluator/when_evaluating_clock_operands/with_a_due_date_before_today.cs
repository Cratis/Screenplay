// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_due_date_before_today : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _inOslo = null!;

    void Establish() => Compile(Source("        calendar \"Europe/Oslo\"\n        validate\n          require dueDate >= today\n        produces Done\n          for id\n"));

    void Because() => _inOslo = Run("2025-03-01", Instant);

    [Fact] void should_refuse_the_command() => _inOslo.ShouldBeOfExactType<SemanticRejected>();
}
