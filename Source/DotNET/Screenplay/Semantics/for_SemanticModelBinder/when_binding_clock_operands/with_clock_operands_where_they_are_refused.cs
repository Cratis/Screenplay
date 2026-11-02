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

    [Fact]
    void should_keep_a_property_named_today_a_path()
    {
        var source = Source("        validate\n          require today >= dueDate\n").Replace("        dueDate Date\n", "        dueDate Date\n        today Date\n", StringComparison.Ordinal);
        var result = BindClock(source);
        result.Success.ShouldBeTrue();
        SemanticClockUsage.UsesCommand(Command(result.Value!)).ShouldBeFalse();
        result.Value!.LanguageVersion.Major.ShouldBeLessThan(36u);
    }

    [Fact]
    void should_refuse_the_clock_operand_in_the_public_binding()
    {
        var result = Bind(Source("        validate\n          require dueDate >= today\n"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    }
}
