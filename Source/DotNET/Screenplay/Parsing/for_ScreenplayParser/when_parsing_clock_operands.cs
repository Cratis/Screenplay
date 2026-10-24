// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser;

public class when_parsing_clock_operands : Specification
{
    const string Slice = "module M\n  feature F\n    slice StateChange S\n";

    static string Source(string command, string specification = "") =>
        $"time zone \"Europe/Oslo\"\n{Slice}      command Submit\n        value String\n{command}{specification}";

    static ApplicationSyntax Parse(string source)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return parsed.Value!;
    }

    [Fact]
    void should_parse_the_application_time_zone()
    {
        Parse("time zone \"Europe/Oslo\"").TimeZone!.Id.ShouldEqual("Europe/Oslo");
        Parse("time zone configurable default \"Europe/Oslo\"").TimeZone!.Configurable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("time zone \"A\"\ntime zone \"B\"")]
    [InlineData("time zone Europe/Oslo")]
    [InlineData("time zone configurable \"A\"")]
    public void should_refuse_a_repeated_or_malformed_time_zone(string source)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTimeZoneDeclaration);
    }

    [Fact]
    void should_refuse_a_time_zone_in_a_placed_document()
    {
        var parsed = new ScreenplayCompiler().Parse("time zone \"Europe/Oslo\"", "fragment.play", new PlayPlacement(["M"]));
        parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTimeZoneDeclaration);
    }

    [Fact]
    void should_parse_a_command_calendar()
    {
        var literal = Parse(Source("        calendar \"Europe/Oslo\"\n")).Modules.Single().Features.Single().Slices.Single().Commands.Single().Calendar!;
        literal.Id.ShouldEqual("Europe/Oslo");
        var fromContext = Parse(Source("        calendar from $context.zone\n")).Modules.Single().Features.Single().Slices.Single().Commands.Single().Calendar!;
        fromContext.ContextMember.ShouldEqual("zone");
    }

    [Fact]
    void should_refuse_a_repeated_command_calendar()
    {
        var parsed = new ScreenplayCompiler().Parse(Source("        calendar \"A\"\n        calendar \"B\"\n"));
        parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandCalendar);
    }

    [Fact]
    void should_not_mistake_a_property_named_calendar_for_the_declaration()
    {
        var command = Parse("module M\n  feature F\n    slice StateChange S\n      command Submit\n        calendar String").Modules.Single().Features.Single().Slices.Single().Commands.Single();
        command.Calendar.ShouldBeNull();
        command.Properties.Select(property => property.Name).ShouldContain("calendar");
    }

    [Fact]
    void should_parse_a_specification_time_zone()
    {
        var specification = Parse(Source(string.Empty, "      specification Case\n        given time zone \"Europe/Oslo\"\n")).Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        specification.GivenTimeZone!.Id.ShouldEqual("Europe/Oslo");
    }

    [Fact]
    void should_refuse_a_repeated_specification_time_zone()
    {
        var parsed = new ScreenplayCompiler().Parse(Source(string.Empty, "      specification Case\n        given time zone \"A\"\n        given time zone \"B\"\n"));
        parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationTimeZone);
    }

    [Theory]
    [InlineData("today - 7 days", "LessThan", "today - 7 days")]
    [InlineData("now + 2 hours", "GreaterThan", "now + 2 hours")]
    [InlineData("today -7 days", "LessThan", "today - 7 days")]
    [InlineData("today+1 day", "LessThan", "today + 1 day")]
    [InlineData("now - 30 minutes", "LessThan", "now - 30 minutes")]
    public void should_parse_a_clock_offset_on_the_right(string clock, string comparison, string printed)
    {
        var op = comparison == "LessThan" ? "<" : ">";
        var condition = Condition($"        produces when dueDate {op} {clock}\n          Recorded\n");
        condition.Right.ShouldBeOfExactType<ClockOffsetExpressionSyntax>();
        ScreenplaySyntaxText.Expression(condition.Right).ShouldEqual(printed);
    }

    [Fact]
    void should_parse_date_of_on_either_side()
    {
        var condition = Condition("        produces when date of occurred >= date of now - 1 days\n          Recorded\n");
        condition.LeftExpression.ShouldBeOfExactType<DateOfExpressionSyntax>();
        condition.Left.ShouldEqual("date of occurred");
        condition.Right.ShouldBeOfExactType<DateOfExpressionSyntax>();
    }

    [Fact]
    void should_leave_a_bare_today_as_a_name()
    {
        var condition = Condition("        produces when dueDate >= today\n          Recorded\n");
        condition.Right.ShouldBeOfExactType<PathExpressionSyntax>();
        condition.LeftExpression.ShouldBeNull();
    }

    [Theory]
    [InlineData("dueDate < today - days")]
    [InlineData("dueDate < today - 7")]
    [InlineData("dueDate < today - 7 weeks")]
    [InlineData("dueDate < date of")]
    public void should_refuse_a_malformed_clock_operand(string condition)
    {
        var parsed = new ScreenplayCompiler().Parse(Source($"        produces when {condition}\n          Recorded\n"));
        parsed.Success.ShouldBeFalse();
    }

    [Fact]
    void should_round_trip_through_json_and_canonical_source()
    {
        var compiler = new ScreenplayCompiler();
        var source = Source(
            "        calendar from $context.zone\n        produces when date of occurred >= date of today - 7 days\n          Recorded\n",
            "      specification Case\n        given time zone \"Europe/Oslo\"\n");
        var parsed = compiler.Parse(source);
        parsed.Success.ShouldBeTrue();
        var json = SyntaxJson.Serialize(parsed.Value!);
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(json);
        var printed = new ScreenplayPrinter().Print(restored);
        printed.ShouldContain("time zone \"Europe/Oslo\"");
        printed.ShouldContain("calendar from $context.zone");
        printed.ShouldContain("date of occurred >= date of today - 7 days");
        printed.ShouldContain("given time zone \"Europe/Oslo\"");
        var reparsed = compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.Serialize(reparsed.Value!).GetRawText().ShouldEqual(json.GetRawText());
    }

    [Fact]
    void should_omit_the_new_members_from_legacy_json()
    {
        var json = SyntaxJson.Serialize(Parse(Source(string.Empty))).GetRawText();
        json.Contains("calendar", StringComparison.Ordinal).ShouldBeFalse();
        json.Contains("leftExpression", StringComparison.Ordinal).ShouldBeFalse();
        json.Contains("givenTimeZone", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_every_form_in_the_semantic_binder_until_admitted()
    {
        var source = Source(
            "        calendar \"Europe/Oslo\"\n        produces when dueDate < today - 7 days\n          Recorded\n",
            "      specification Case\n        given time zone \"Europe/Oslo\"\n");
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Clocks"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("single"), "single", "single.play", source);
        var result = new SemanticModelBinder().Bind("Clocks", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        var messages = result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).Select(diagnostic => diagnostic.Message).ToArray();
        messages.ShouldContain(message => message.Contains("time zone is not yet admitted", StringComparison.Ordinal));
        messages.ShouldContain(message => message.Contains("declares a calendar", StringComparison.Ordinal));
        messages.ShouldContain(message => message.Contains("given time zone", StringComparison.Ordinal));
        messages.ShouldContain(message => message.Contains("Clock operands", StringComparison.Ordinal));
    }

    static ComparisonConditionSyntax Condition(string validate)
    {
        var command = Parse(Source(validate)).Modules.Single().Features.Single().Slices.Single().Commands.Single();
        return (ComparisonConditionSyntax)command.Produces.Single().When!;
    }
}
