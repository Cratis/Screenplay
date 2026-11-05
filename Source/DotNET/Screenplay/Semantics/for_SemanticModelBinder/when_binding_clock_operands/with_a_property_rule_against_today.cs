// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_a_property_rule_against_today : given.a_clock_binder
{
    CompilationResult<ClockOperandsBinding> _result = null!;

    void Because() => _result = BindClock(Source("        validate\n          dueDate >= today\n"));

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_today_as_the_rule_clock_operand() => Command(_result.Value!).Validations.Single().ClockOperand.ShouldEqual(new SemanticClockOperand(SemanticClockKind.Today, 0, SemanticClockUnit.Day));
}
