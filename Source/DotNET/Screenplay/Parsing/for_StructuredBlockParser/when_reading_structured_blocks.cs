// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing.for_StructuredBlockParser;

public class when_reading_structured_blocks : Specification
{
    const string Head = "module M\n  feature F\n    slice StateChange S\n      specification T\n        when C\n";
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_read_quoted_and_escaped_member_names()
    {
        var value = (ObjectExpressionSyntax)ValueOf("detail", "  \"first name\" = \"Ada\"", "  @item = 1");
        value.Members.Select(member => member.Name).ShouldEqual("first name", "item");
    }

    [Fact]
    void should_read_nested_lists_empty_lists_and_objects_without_members()
    {
        var value = (ListExpressionSyntax)ValueOf("grid", "  item", "    item 1", "  item none", "  item empty");
        var items = value.Items.ToArray();
        items[0].ShouldBeOfExactType<ListExpressionSyntax>();
        ((ListExpressionSyntax)items[1]).Items.ShouldBeEmpty();
        ((ObjectExpressionSyntax)items[2]).Members.ShouldBeEmpty();
    }

    [Fact] void should_read_an_empty_list() => ((ListExpressionSyntax)ValueOf("tags none")).Items.ShouldBeEmpty();
    [Fact] void should_read_an_object_without_members() => ((ObjectExpressionSyntax)ValueOf("detail empty")).Members.ShouldBeEmpty();
    [Fact] void should_refuse_a_line_under_an_empty_list() => Codes("tags none", "  item 1").ShouldEqual("PLAY0291@7");
    [Fact] void should_refuse_a_line_under_an_object_without_members() => Codes("detail empty", "  name = 1").ShouldEqual("PLAY0291@7");
    [Fact] void should_refuse_a_nested_member_with_nothing_under_it() => Codes("detail", "  inner").ShouldEqual("PLAY0291@7");
    [Fact] void should_refuse_a_bare_item_with_nothing_under_it() => Codes("tags", "  item").ShouldEqual("PLAY0291@7");
    [Fact] void should_refuse_a_member_line_in_a_list() => Codes("tags", "  item 1", "  name = 2").ShouldEqual("PLAY0291@8");
    [Fact] void should_refuse_an_item_line_in_an_object() => Codes("detail", "  name = 2", "  item 1").ShouldEqual("PLAY0291@8");
    [Fact] void should_refuse_a_malformed_member() => Codes("detail", "  name : 2").ShouldEqual("PLAY0291@7");
    [Fact] void should_refuse_lines_under_a_member_with_a_value() => Codes("detail", "  name = 2", "    more = 3").ShouldEqual("PLAY0291@8");
    [Fact] void should_refuse_lines_under_an_item_with_a_value() => Codes("tags", "  item 1", "    more = 3").ShouldEqual("PLAY0291@8");
    [Fact] void should_refuse_a_value_that_is_not_a_scalar() => Codes("detail", "  name = other").ShouldEqual("PLAY0291@7");
    [Fact] void should_refuse_a_case_reference_inside_a_block() => Codes("detail", "  name = case.amount").ShouldEqual("PLAY0584@7");
    [Fact] void should_refuse_inline_json_inside_a_block() => Codes("detail", "  name = [1]").ShouldEqual("PLAY0833@7");
    [Fact] void should_refuse_a_repeated_member() => Codes("detail", "  name = 1", "  name = 2").ShouldEqual("PLAY0294@8");

    [Fact]
    void should_read_a_structured_return_opened_with_value()
    {
        var result = _compiler.Parse(Head + "        then returns value\n          id = 1\n");
        ((ScalarSpecificationReturnSyntax)Specification(result).ThenReturns!).Value.ShouldBeOfExactType<ObjectExpressionSyntax>();
    }

    [Fact] void should_refuse_a_structured_return_with_nothing_under_it() => _compiler.Parse(Head + "        then returns value\n").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidStructuredValue);
    [Fact] void should_read_an_empty_structured_return() => _compiler.Parse(Head + "        then returns value none\n").Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_a_structured_generated_fixture() => _compiler.Parse(Head + "          generated receipt\n            item 1\n").Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_a_pinned_generated_block() => _compiler.Parse(Head.Replace("        when C\n", "        given generated C.receipt none\n        when C\n", StringComparison.Ordinal)).Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_read_a_composition_set_block_and_a_structured_item_value()
    {
        var result = _compiler.Parse("instance Browse\n  set \"shell:header\".actions\n    item \"export\"\n  set shellHeader.filters none\n  items shellHeader.actions\n    item \"export:csv\"\n      tags\n        item \"a\"\n");
        result.Diagnostics.ShouldBeEmpty();
        var contributions = result.Value!.InstanceContributions.Single().Contributions.ToArray();
        ((ListExpressionSyntax)contributions[0].Value!).Items.Count().ShouldEqual(1);
        ((ListExpressionSyntax)contributions[1].Value!).Items.ShouldBeEmpty();
        contributions[2].Items.Single().Values.Single().Value.ShouldBeOfExactType<ListExpressionSyntax>();
    }

    [Fact]
    void should_read_structured_inline_operation_inputs()
    {
        var result = _compiler.Parse("type Contact\n  recipient String\nsystem Mailer\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        produces operation Send\n          uses Mailer\n          contact Contact\n            recipient = \"Ada\"\n          tags String[] none\n");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var produces = result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
        produces.Mappings.Select(mapping => mapping.Source.GetType()).ShouldEqual(typeof(ObjectExpressionSyntax), typeof(ListExpressionSyntax));
    }

    [Fact]
    void should_read_structured_inline_event_values()
    {
        var result = _compiler.Parse("type Line\n  sku String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        produces event Placed\n          lines Line[]\n            item\n              sku = \"A-1\"\n          tags String[] empty\n");
        var produces = result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
        produces.Mappings.Select(mapping => mapping.Source.GetType()).ShouldEqual(typeof(ListExpressionSyntax), typeof(ObjectExpressionSyntax));
        produces.InlineEvent!.Properties.Select(property => property.Name).ShouldEqual("lines", "tags");
    }

    [Fact]
    void should_read_a_literal_binding_block_with_modifiers()
    {
        var result = _compiler.Parse("module M\n  feature F\n    slice StateView S\n      screen S\n        component Cratis.Blueprint.Default.list items\n          property filter from literal mode oneWay\n            status = \"open\"\n          property empty from literal empty\n");
        result.Diagnostics.ShouldBeEmpty();
        var properties = result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenComponentSyntax>().Single().Properties.ToArray();
        properties[0].Binding!.Literal.ShouldBeOfExactType<ObjectExpressionSyntax>();
        properties[0].Binding!.Mode.ShouldEqual(UiBindingMode.OneWay);
        ((ObjectExpressionSyntax)properties[1].Binding!.Literal!).Members.ShouldBeEmpty();
    }

    ExpressionSyntax ValueOf(params string[] lines) => Specification(Read(lines)).When!.Values.First().Source;

    string[] Codes(params string[] lines) => [.. Read(lines).Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}")];

    CompilationResult<ApplicationSyntax> Read(string[] lines) => _compiler.Parse(Head + string.Join('\n', lines.Select(line => $"          {line}")) + "\n");

    static SpecificationSyntax Specification(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
}
