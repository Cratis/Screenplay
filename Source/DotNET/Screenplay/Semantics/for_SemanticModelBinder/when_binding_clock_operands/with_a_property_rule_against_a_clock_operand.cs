// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_a_property_rule_against_a_clock_operand : given.a_clock_binder
{
    [Theory]
    [InlineData("now", SemanticClockKind.Now)]
    [InlineData("today", SemanticClockKind.Today)]
    public void should_bind_the_date_of_the_clock_as_the_rule_operand(string clock, SemanticClockKind kind) =>
        Command(BindRule(clock)).Validations.Single().ClockOperand.ShouldEqual(new SemanticClockOperand(kind, 0, SemanticClockUnit.Day) { DateOf = true });

    [Fact]
    void should_bind_an_offset_as_the_rule_operand() =>
        Command(BindRule("today - 1 days", dateOf: false)).Validations.Single().ClockOperand.ShouldEqual(new SemanticClockOperand(SemanticClockKind.Today, -1, SemanticClockUnit.Day));

    [Theory]
    [InlineData("now")]
    [InlineData("today")]
    public void should_select_v37(string clock) => Model(clock).SemanticVersion.ShouldEqual(SemanticVersion.V37);

    [Theory]
    [InlineData("now")]
    [InlineData("today")]
    public void should_write_the_date_of_the_clock(string clock) =>
        Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(Model(clock))).ShouldContain($"\"clock\":{{\"kind\":\"{clock}\",\"dateOf\":true}}");

    [Theory]
    [InlineData("now")]
    [InlineData("today")]
    public void should_round_trip_strictly(string clock)
    {
        var bytes = SemanticModelSerializer.Serialize(Model(clock));
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).ShouldEqual(bytes);
    }

    ClockOperandsBinding BindRule(string clock, bool dateOf = true)
    {
        var result = BindClock(Source($"        validate\n          dueDate >= {(dateOf ? "date of " : string.Empty)}{clock}\n"));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!;
    }

    ExecutableSemanticModel Model(string clock)
    {
        var binding = BindRule(clock);
        return ExecutableSemanticModel.Create(binding.LanguageVersion, binding.SemanticVersion, binding.Application);
    }
}
