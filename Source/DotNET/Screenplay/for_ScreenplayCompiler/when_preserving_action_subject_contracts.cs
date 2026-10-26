// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_preserving_action_subject_contracts : Specification
{
    const string Source = """
        module Work
          feature Items
            slice StateView Details
              screen Details
                action Retry for item label "Retry for screen"
                  with id from item.foreignKey
                  with note from $context.note
                  navigate to Details
                action "Choose" for screen
                  when item.ready == true execute Retry
                    with id from item.foreignKey
                  otherwise execute Release
                    with id from item.foreignKey
                toolbar
                  item retry action Retry for screen label "Try"
                    parameter id from item.foreignKey
                    icon "retry"
                    confirm "Sure?"
        """;
    ApplicationSyntax _application;

    void Establish() => _application = new ScreenplayCompiler().Parse(Source).Value!;

    [Fact]
    void should_print_and_reparse_only_authored_metadata()
    {
        var printed = new ScreenplayPrinter().Print(_application);
        var reparsed = new ScreenplayCompiler().Parse(printed).Value!;
        SyntaxJson.StructurallyEqual(_application, reparsed).ShouldBeTrue();
        printed.ShouldContain("for item");
        printed.ShouldContain("for screen");
    }

    [Fact]
    void should_roundtrip_new_json_members() => SyntaxJson.StructurallyEqual(_application, SyntaxJson.Deserialize(SyntaxJson.Serialize(_application))).ShouldBeTrue();

    [Fact]
    void should_keep_legacy_constructor_and_deconstruction_arity()
    {
        var action = new ScreenActionSyntax("Retry", null, null, SourceLocation.Start);
        var (command, label, navigate, location) = action;
        command.ShouldEqual("Retry");
        label.ShouldBeNull();
        navigate.ShouldBeNull();
        location.ShouldEqual(SourceLocation.Start);
        SyntaxJson.Serialize(action).TryGetProperty("subject", out _).ShouldBeFalse();
        SyntaxJson.Serialize(action).TryGetProperty("inputs", out _).ShouldBeFalse();
    }

    [Fact]
    void should_visit_each_plain_input_and_keep_its_source_location()
    {
        var walker = new Syntax.for_ScreenplaySyntaxWalker.given.a_counting_walker();
        walker.VisitApplication(_application);
        var plain = walker.Nodes.OfType<ScreenActionSyntax>().Single();
        var inputs = plain.Inputs.ToArray();
        inputs.Select(input => input.Name).ShouldEqual(["id", "note"]);
        inputs.Select(input => input.Location.Line).ShouldEqual([6, 7]);
        walker.Nodes.OfType<InteractionArgumentSyntax>().Count().ShouldEqual(4);
    }

    [Fact]
    void should_refuse_unknown_subject_values()
    {
        var json = SyntaxJson.Serialize(_application).GetRawText().Replace("\"Item\"", "\"Row\"", StringComparison.Ordinal);
        Assert.Throws<InvalidSyntaxJson>(() => SyntaxJson.Deserialize(JsonDocument.Parse(json).RootElement));
    }

    [Fact]
    void should_refuse_subject_metadata_on_navigation_toolbar_items()
    {
        var json = SyntaxJson.Serialize(_application).GetRawText().Replace("\"syntaxKind\":\"Action\"", "\"syntaxKind\":\"Navigate\"", StringComparison.Ordinal);
        Assert.Throws<InvalidSyntaxJson>(() => SyntaxJson.Deserialize(JsonDocument.Parse(json).RootElement));
    }
}
