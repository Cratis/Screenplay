// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_bare_date_of_clock_operands : given.a_clock_binder
{
    [Theory]
    [InlineData("now", true)]
    [InlineData("today", true)]
    [InlineData("now", false)]
    [InlineData("today", false)]
    public void should_bind_the_calendar_date_of_a_bare_clock(string clock, bool requirement)
    {
        var body = requirement
            ? $"        validate\n          require dueDate == date of {clock}\n"
            : $"        produces when dueDate == date of {clock}\n          Done\n            for id\n";
        var result = BindClock(Source(body));
        result.Success.ShouldBeTrue();
        var command = Command(result.Value!);
        var comparison = (SemanticComparison)(requirement ? command.Requirements.Single().Condition : command.Produces.Single().When!);
        comparison.Right.Clock.ShouldEqual(new SemanticClockOperand(clock == "today" ? SemanticClockKind.Today : SemanticClockKind.Now, 0, SemanticClockUnit.Day) { DateOf = true });
    }

    [Fact]
    void should_keep_a_property_named_now_in_date_of_a_path()
    {
        var source = Source("        validate\n          require dueDate == date of now\n").Replace("        occurred DateTime\n", "        now DateTime\n", StringComparison.Ordinal);
        var result = BindClock(source);
        result.Success.ShouldBeTrue();
        var command = Command(result.Value!);
        var comparison = (SemanticComparison)command.Requirements.Single().Condition;
        comparison.Right.Property.ShouldEqual(command.Properties.Single(property => property.Name == "now").Id);
        comparison.Right.DateOf.ShouldBeTrue();
        comparison.Right.Clock.ShouldBeNull();
    }
}
