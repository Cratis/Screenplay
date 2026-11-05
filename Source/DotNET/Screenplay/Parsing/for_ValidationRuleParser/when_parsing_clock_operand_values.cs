// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ValidationRuleParser;

public class when_parsing_clock_operand_values : Specification
{
    static CompilationResult<ApplicationSyntax> Parse(string rule) =>
        new ScreenplayCompiler().Parse($"module M\n  feature F\n    slice StateChange S\n      command C\n        dueDate Date\n        validate\n          {rule}\n");

    static ValidationRuleSyntax Rule(string rule)
    {
        var parsed = Parse(rule);
        parsed.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var validate = (DeclarativeValidateSyntax)parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
        return validate.Rules.Single();
    }

    [Theory]
    [InlineData("dueDate >= date of now", "date of now")]
    [InlineData("dueDate >= date of today", "date of today")]
    [InlineData("dueDate < date of now + 2 hours", "date of now + 2 hours")]
    public void should_parse_the_date_of_a_clock(string rule, string printed)
    {
        var value = Rule(rule).Value!;
        value.ShouldBeOfExactType<DateOfExpressionSyntax>();
        ScreenplaySyntaxText.Expression(value).ShouldEqual(printed);
    }

    [Fact]
    void should_parse_a_clock_offset() => Rule("dueDate >= today - 1 days").Value.ShouldBeOfExactType<ClockOffsetExpressionSyntax>();

    [Fact]
    void should_keep_a_bare_clock_a_path() => Rule("dueDate >= now").Value.ShouldBeOfExactType<PathExpressionSyntax>();

    [Fact]
    void should_refuse_trailing_tokens_after_a_clock_operand() =>
        Parse("dueDate >= date of now later").Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidValidationRule);
}
