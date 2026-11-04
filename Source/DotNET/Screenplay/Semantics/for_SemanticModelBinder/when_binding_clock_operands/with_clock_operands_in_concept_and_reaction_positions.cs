// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_clock_operands_in_concept_and_reaction_positions : given.a_clock_binder
{
    const string ReactionSource =
        """
        module M
          feature F
            slice StateChange S
              event Done
                dueDate Date
              command C
                id Uuid identifier
                dueDate Date
                produces Done
                  for id
                  dueDate = dueDate
            slice Translate T
              reaction R
                when Done
                  dueDate
                  invokes C
                where dueDate >= today - 1 days
        """;

    const string ConceptSource =
        """
        concept DueDate : Date
          validate
            >= today - 1 days
        module M
          feature F
            slice StateChange S
              event Done
              command C
                id Uuid identifier
                due DueDate
                produces Done
                  for id
        """;

    const string ClockRefusal = "Clock operands (today, now and date of) are not yet admitted";

    [Fact]
    public void should_refuse_the_clock_operand_in_a_reaction_condition_when_writing_the_candidate()
    {
        var result = BindClock(ReactionSource);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax &&
            diagnostic.Message.Contains(ClockRefusal, StringComparison.Ordinal));
    }

    // Gap: a concept rule never reaches a clock-specific refusal; the existing date refusal fires first.
    [Fact]
    public void should_refuse_concept_date_rules_before_clock_detection()
    {
        var result = BindClock(ConceptSource);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax &&
            diagnostic.Message.Contains("'DueDate'", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("'>='", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("is not admitted: ESM v1 has no runtime date value", StringComparison.Ordinal));
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Message.Contains(ClockRefusal, StringComparison.Ordinal));
    }
}
