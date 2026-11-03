// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_fixed_offset_clock_operands : given.a_clock_command
{
    [Theory]
    [InlineData("occurred == now + 2 hours", "2025-03-02T08:00:00Z")]
    [InlineData("occurred == now - 30 minutes", "2025-03-02T10:30:00Z")]
    [InlineData("date of occurred == date of now + 2 hours", "2025-03-01T23:00:00Z")]
    public void should_evaluate_the_bound_offset_and_date_of_operands(string condition, string instant)
    {
        Compile(Source($"        validate\n          require {condition}\n        produces Done\n          for id\n"));
        Run("2025-03-02", DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture)).ShouldBeOfExactType<SemanticAccepted>();
    }

    [Theory]
    [InlineData("Europe/Oslo", "2024-02-28T23:30:00Z", "2024-02-29")]
    [InlineData("Asia/Kolkata", "2025-03-01T18:45:00Z", "2025-03-02")]
    public void should_use_the_fixed_instant_for_today(string zone, string instant, string date)
    {
        Compile(Source($"        calendar \"{zone}\"\n        validate\n          require dueDate == today\n        produces Done\n          for id\n"));
        Run(date, DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture)).ShouldBeOfExactType<SemanticAccepted>();
    }
}
