// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_response_boundaries : given.a_compiler
{
    const string Prefix = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("Date", "2024-02-29", true)]
    [InlineData("Date", "1900-02-29", false)]
    [InlineData("Date", "2000-02-29", true)]
    [InlineData("Date", "2023/02/29", false)]
    [InlineData("Date", "2024-01-02T00:00", false)]
    [InlineData("Date", "Jan 2 2024", false)]
    [InlineData("DateTime", "2024-02-29T12:00:00Z", true)]
    [InlineData("DateTime", "2024-02-29T12:00:00.1234567-14:00", true)]
    [InlineData("DateTime", "2024-02-29T12:00:00.12345678Z", false)]
    [InlineData("DateTime", "2023-02-29t12:00:00Z", false)]
    [InlineData("DateTime", "2023-02-29T12:00:00Z", false)]
    [InlineData("DateTime", "2023-01-01T12:00:00+1500", false)]
    [InlineData("DateTime", "2024-01-01T12:00:00+14:01", false)]
    [InlineData("DateTime", "2024-01-01T12:00:00+01:60", false)]
    [InlineData("DateTime", "2024-01-01T24:00:00Z", false)]
    [InlineData("DateTime", "2024-01-01T12:00:00", false)]
    [InlineData("DateTime", "10:00", false)]
    void should_check_response_calendar_and_offset_spelling_without_normalization(string type, string value, bool valid)
    {
        var parsed = _compiler.Compile(Prefix + $"      command C\n        value {type}\n        returns value\n      specification Accepts\n        when C\n        then returns \"{value}\"");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0491").ShouldEqual(!valid);
    }

    [Fact]
    void should_not_tighten_general_literal_parsing()
    {
        var parsed = _compiler.Parse(Prefix + "      command C\n        value Date\n      specification Accepts\n        when C\n          value = \"Jan 2 2024\"");
        parsed.Success.ShouldBeTrue();
    }

    [Fact]
    void should_round_trip_the_smallest_and_largest_finite_double_returns()
    {
        should_round_trip_finite_double_returns_without_exponent_spelling("0." + new string('0', 323) + "5");
        should_round_trip_finite_double_returns_without_exponent_spelling("17976931348623157" + new string('0', 292));
    }

    [Theory]
    [InlineData("")]
    [InlineData("import External.Enum\n")]
    void should_keep_unresolved_enum_shapes_unknown(string import)
    {
        var parsed = _compiler.Compile(import + Prefix + "      command C\n        value Enum\n        returns value\n      specification Accepts\n        when C\n        then returns \"text\"");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0491").ShouldBeFalse();
    }

    [Theory]
    [InlineData("0.0000001")]
    [InlineData("-0.0000001")]
    [InlineData("1000000000000000000000000000000")]
    [InlineData("123456789012345678901234567890.5")]
    void should_round_trip_finite_double_returns_without_exponent_spelling(string number)
    {
        foreach (var record in new[] { false, true })
        {
            var contract = record ? "returns\n          result = value" : "returns value";
            var expectation = record ? "then returns\n          result = " + number : "then returns " + number;
            var parsed = _compiler.Parse(Prefix + $"      command C\n        value Decimal\n        {contract}\n      specification Accepts\n        when C\n        {expectation}");
            parsed.Success.ShouldBeTrue();
            var printed = new ScreenplayPrinter().Print(parsed.Value!);
            printed.ShouldNotContain("E-");
            printed.ShouldNotContain("E+");
            var reparsed = _compiler.Parse(printed);
            reparsed.Success.ShouldBeTrue();
            SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
        }
    }

    [Fact]
    void should_round_trip_nested_numeric_return_values()
    {
        var parsed = _compiler.Parse("type Payload\n  amount Decimal\n  amounts Decimal[]\n" + Prefix + "      command C\n        value Payload\n        returns value\n      specification Accepts\n        when C\n        then returns {\"amount\":0.0000001,\"amounts\":[1000000000000000000000000000000]}");
        parsed.Success.ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        var reparsed = _compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
    }

    [Fact]
    void should_round_trip_the_smallest_and_largest_finite_double_generated_fixtures()
    {
        should_round_trip_numeric_generated_fixtures_for_imported_unknown_shapes("0." + new string('0', 323) + "5");
        should_round_trip_numeric_generated_fixtures_for_imported_unknown_shapes("17976931348623157" + new string('0', 292));
    }

    [Theory]
    [InlineData("0.0000001")]
    [InlineData("-0.0000001")]
    [InlineData("1000000000000000000000000000000")]
    [InlineData("123456789012345678901234567890.5")]
    void should_round_trip_numeric_generated_fixtures_for_imported_unknown_shapes(string number)
    {
        foreach (var value in new[] { number, "[" + number + "]", "{\"amount\":" + number + ",\"amounts\":[" + number + "]}" })
        {
            var parsed = _compiler.Parse("import External.Id\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        receipt Id generated\n      specification Accepts\n        when C\n          generated receipt = " + value);
            parsed.Success.ShouldBeTrue();
            var printed = new ScreenplayPrinter().Print(parsed.Value!);
            printed.ShouldNotContain("E-");
            printed.ShouldNotContain("E+");
            var reparsed = _compiler.Parse(printed);
            reparsed.Success.ShouldBeTrue();
            SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
            _compiler.Compile(printed).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0490").ShouldBeFalse();
        }
    }

    [Fact]
    void should_keep_imported_enum_fixture_shapes_unknown()
    {
        var parsed = _compiler.Compile("import External.Enum\n" + Prefix + "      command C\n        receipt Enum generated\n      specification Accepts\n        when C\n          generated receipt = \"text\"");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0490").ShouldBeFalse();
    }

    [Theory]
    [InlineData("refresh Q")]
    [InlineData("navigate to Entry")]
    [InlineData("open dialog Dialog")]
    [InlineData("close dialog")]
    void should_not_attribute_continuation_arguments_to_the_enclosing_command(string action)
    {
        var parsed = _compiler.Compile(Prefix + $"      command C\n        receipt Id generated\n      screen Entry\n        on submit\n          execute C\n            on success\n              {action}\n                with receipt from $form.receipt");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0485").ShouldBeFalse();
    }

    [Theory]
    [InlineData("parameter C\n  on submit\n    execute C\n      with receipt from $form.receipt")]
    [InlineData("on submit\n    execute C\n      with receipt from $form.receipt\n  parameter C")]
    void should_respect_behavior_parameter_shadowing_in_any_declaration_order(string body)
    {
        var parsed = _compiler.Compile("behavior B\n  " + body + "\n" + Prefix + "      command C\n        receipt Id generated\n      command D\n        receipt Id\n      screen Entry\n        uses B\n          C D");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0485").ShouldBeFalse();
    }

    [Fact]
    void should_still_check_actual_command_arguments_in_dialog_continuations()
    {
        var parsed = _compiler.Compile(Prefix + "      command C\n        receipt Id generated\n      screen Entry\n        on submit\n          open dialog Dialog\n            on result\n              execute C\n                with receipt from $result.receipt");
        parsed.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0485").ShouldEqual(1);
    }

    [Fact]
    void should_check_escaped_invocation_mapping_targets()
    {
        var parsed = _compiler.Compile(Prefix + "      command C\n        receipt Id generated\n      reaction R\n        when Recorded\n          invokes C\n            @receipt = \"ignored\"");
        parsed.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0485").ShouldEqual(1);
    }

    [Fact]
    void should_check_table_behavior_inputs()
    {
        var parsed = _compiler.Compile(Prefix + "      command C\n        receipt Id generated\n      screen Entry\n        table items\n          column name\n          on select\n            execute C\n              with receipt from $row.receipt");
        parsed.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0485").ShouldEqual(1);
    }

    [Theory]
    [InlineData("returns Foo identifier optional", "PLAY0480")]
    [InlineData("returns Id generated optional", "PLAY0484")]
    void should_keep_property_modifier_diagnostics_for_property_shaped_returns(string declaration, string code)
    {
        var parsed = _compiler.Parse(Prefix + "      command C\n        " + declaration);
        parsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0486").ShouldBeFalse();
    }

    [Fact]
    void should_keep_tab_separated_returns_as_the_preexisting_property_declaration()
    {
        const string Source = Prefix + "      command C\n        id Id identifier\n        returns\tId";
        var parsed = _compiler.Compile(Source);
        parsed.Success.ShouldBeTrue();
        var command = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        command.Response.ShouldBeNull();
        command.Properties.Single(property => property.Name == "returns").Type.Name.ShouldEqual("Id");
    }
}
