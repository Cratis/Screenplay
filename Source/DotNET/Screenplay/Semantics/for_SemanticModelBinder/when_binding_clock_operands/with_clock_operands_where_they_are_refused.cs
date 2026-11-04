// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_clock_operands_where_they_are_refused : given.a_clock_binder
{
    [Theory]
    [InlineData("        validate\n          require occurred >= today\n        produces Done\n          for id\n")]
    [InlineData("        validate\n          require dueDate >= now\n")]
    [InlineData("        validate\n          require today >= today - 1 days\n")]
    public void should_refuse_a_mismatched_or_clock_to_clock_comparison(string body)
    {
        var result = BindClock(Source(body));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    }

    [Theory]
    [InlineData("today - 7 days")]
    [InlineData("date of now")]
    public void should_refuse_offsets_and_date_of_in_validate_property_rules(string operand)
    {
        var result = BindClock(Source($"        validate\n          dueDate >= {operand}\n"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "PLAY0268");
    }

    [Theory]
    [InlineData("require dueDate >= today + 2 hours")]
    [InlineData("require dueDate >= today - 30 minutes")]
    [InlineData("require dueDate >= date of today + 2 hours")]
    [InlineData("require dueDate >= date of today - 30 minutes")]
    [InlineData("produces when dueDate >= today + 2 hours\n          Done\n            for id")]
    [InlineData("produces when dueDate >= today - 30 minutes\n          Done\n            for id")]
    public void should_refuse_sub_day_offsets_on_today(string condition)
    {
        var body = condition.StartsWith("require", StringComparison.Ordinal)
            ? $"        validate\n          {condition}\n"
            : $"        {condition}\n";
        var result = BindClock(Source(body));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message == "'today' is offset in whole days; use 'now' for hours or minutes.");
    }

    [Fact]
    void should_keep_a_property_named_today_a_path()
    {
        var source = Source("        validate\n          require today >= dueDate\n").Replace("        dueDate Date\n", "        dueDate Date\n        today Date\n", StringComparison.Ordinal);
        var result = BindClock(source);
        result.Success.ShouldBeTrue();
        SemanticClockUsage.UsesCommand(Command(result.Value!)).ShouldBeFalse();
        result.Value!.LanguageVersion.Major.ShouldBeLessThan(36u);
    }
}
