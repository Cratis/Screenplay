// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

// St John's falls back from 1987-10-25 00:01 to 1987-10-24 23:01 at 02:31Z.
public class with_a_calendar_during_a_dst_overlap : given.a_clock_command
{
    [Theory]
    [InlineData("today", "1987-10-25T02:30:30Z", "1987-10-25", "1987-10-24")]
    [InlineData("today", "1987-10-25T02:31:30Z", "1987-10-24", "1987-10-25")]
    [InlineData("date of now + 1 day", "1987-10-25T02:30:30Z", "1987-10-25", "1987-10-26")]
    public void should_compare_the_due_date_with_the_calendar_date_of_each_instant(string operand, string instant, string expectedDate, string otherDate)
    {
        Compile(Source($"        calendar \"America/St_Johns\"\n        validate\n          require dueDate == {operand}\n        produces Done\n          for id\n"));
        var at = DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture);
        Run(expectedDate, at).ShouldBeOfExactType<SemanticAccepted>();
        Run(otherDate, at).ShouldBeOfExactType<SemanticRejected>();
    }
}
